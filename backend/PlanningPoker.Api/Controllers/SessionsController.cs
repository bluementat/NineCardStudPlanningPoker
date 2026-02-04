using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.DTOs;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Models;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly PlanningPokerDbContext _context;
    private readonly IPinGenerator _pinGenerator;
    private readonly IHubContext<PlanningPokerHub> _hubContext;

    public SessionsController(
        PlanningPokerDbContext context,
        IPinGenerator pinGenerator,
        IHubContext<PlanningPokerHub> hubContext)
    {
        _context = context;
        _pinGenerator = pinGenerator;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession([FromBody] CreateSessionRequest request)
    {
        var pin = await _pinGenerator.GenerateUniquePinAsync();
        
        var session = new Session
        {
            PIN = pin,
            SessionName = request.SessionName,
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new SessionDto
        {
            SessionId = session.SessionId,
            PIN = session.PIN,
            SessionName = session.SessionName,
            CreatedAt = session.CreatedAt,
            Status = session.Status.ToString()
        });
    }

    [HttpGet("{pin}")]
    public async Task<ActionResult<SessionDto>> GetSession(string pin)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.PIN == pin);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        return Ok(new SessionDto
        {
            SessionId = session.SessionId,
            PIN = session.PIN,
            SessionName = session.SessionName,
            CreatedAt = session.CreatedAt,
            Status = session.Status.ToString(),
            Participants = session.Participants.Select(p => new ParticipantDto
            {
                ParticipantId = p.ParticipantId,
                Name = p.Name,
                JoinedAt = p.JoinedAt
            }).ToList()
        });
    }

    [HttpPost("{pin}/participants")]
    public async Task<ActionResult<ParticipantDto>> JoinSession(string pin, [FromBody] JoinSessionRequest request)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == pin);
        if (session == null)
        {
            return NotFound("Session not found");
        }

        if (session.Status != SessionStatus.Active)
        {
            return BadRequest("Session is not active");
        }

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = request.Name,
            JoinedAt = DateTime.UtcNow
        };

        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(pin).SendAsync("ParticipantJoined", new ParticipantDto
        {
            ParticipantId = participant.ParticipantId,
            Name = participant.Name,
            JoinedAt = participant.JoinedAt
        });

        return Ok(new ParticipantDto
        {
            ParticipantId = participant.ParticipantId,
            Name = participant.Name,
            JoinedAt = participant.JoinedAt
        });
    }

    [HttpPost("{pin}/votes")]
    public async Task<ActionResult> SubmitVote(string pin, [FromBody] SubmitVoteRequest request)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == pin);
        if (session == null)
        {
            return NotFound("Session not found");
        }

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.ParticipantId == request.ParticipantId && p.SessionId == session.SessionId);
        
        if (participant == null)
        {
            return NotFound("Participant not found");
        }

        // Remove existing vote for this participant in this session (if any)
        var existingVote = await _context.Votes
            .Where(v => v.SessionId == session.SessionId && v.ParticipantId == request.ParticipantId)
            .OrderByDescending(v => v.VotedAt)
            .FirstOrDefaultAsync();

        if (existingVote != null)
        {
            _context.Votes.Remove(existingVote);
        }

        var vote = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = request.ParticipantId,
            CardValue = request.CardValue,
            VotedAt = DateTime.UtcNow
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(pin).SendAsync("VoteSubmitted", new
        {
            ParticipantId = participant.ParticipantId,
            ParticipantName = participant.Name,
            CardValue = vote.CardValue
        });

        return Ok(new { Message = "Vote submitted successfully" });
    }

    [HttpPost("{pin}/reveal")]
    public async Task<ActionResult> RevealVotes(string pin)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == pin);
        if (session == null)
        {
            return NotFound("Session not found");
        }

        await _hubContext.Clients.Group(pin).SendAsync("VotesRevealed", new { Pin = pin });

        return Ok(new { Message = "Votes revealed" });
    }

    [HttpPost("{pin}/reset")]
    public async Task<ActionResult> ResetSession(string pin)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions
            .Include(s => s.Votes)
            .FirstOrDefaultAsync(s => s.PIN == pin);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        _context.Votes.RemoveRange(session.Votes);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group(pin).SendAsync("NewRoundStarted", new { Pin = pin });

        return Ok(new { Message = "Session reset for new round" });
    }

    [HttpDelete("{pin}")]
    public async Task<ActionResult> EndSession(string pin)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions
            .Include(s => s.Participants)
            .Include(s => s.Votes)
            .FirstOrDefaultAsync(s => s.PIN == pin);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        // Notify all participants that the session has ended
        await _hubContext.Clients.Group(pin).SendAsync("SessionEnded", new { Pin = pin });

        // Remove the session (cascade delete will handle participants and votes)
        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Session ended and deleted successfully" });
    }

    [HttpGet("{pin}/results")]
    public async Task<ActionResult<ResultsDto>> GetResults(string pin)
    {
        if (!_pinGenerator.IsValidPin(pin))
        {
            return BadRequest("Invalid PIN format");
        }

        var session = await _context.Sessions
            .Include(s => s.Votes)
            .ThenInclude(v => v.Participant)
            .FirstOrDefaultAsync(s => s.PIN == pin);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        var votes = session.Votes
            .OrderByDescending(v => v.VotedAt)
            .GroupBy(v => v.ParticipantId)
            .Select(g => g.First())
            .ToList();

        var cardValues = votes
            .Where(v => v.CardValue != "?" && int.TryParse(v.CardValue, out _))
            .Select(v => int.Parse(v.CardValue))
            .ToList();

        var results = new ResultsDto
        {
            Votes = votes.Select(v => new VoteResultDto
            {
                ParticipantId = v.ParticipantId,
                ParticipantName = v.Participant.Name,
                CardValue = v.CardValue,
                VotedAt = v.VotedAt
            }).ToList(),
            Statistics = cardValues.Any() ? new StatisticsDto
            {
                Average = cardValues.Average(),
                Min = cardValues.Min(),
                Max = cardValues.Max()
            } : null
        };

        return Ok(results);
    }
}
