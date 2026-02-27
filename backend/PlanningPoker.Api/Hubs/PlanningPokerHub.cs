using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.DTOs;

namespace PlanningPoker.Api.Hubs;

public class PlanningPokerHub : Hub
{
    private static readonly ConcurrentDictionary<string, (string Pin, int ParticipantId)> ConnectionToParticipant = new();
    private readonly PlanningPokerDbContext _context;
    private readonly ILogger<PlanningPokerHub> _logger;

    public PlanningPokerHub(PlanningPokerDbContext context, ILogger<PlanningPokerHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task JoinSession(string pin, int participantId, string participantName)
    {
        _logger.LogInformation("SignalR client joining group. PIN: {PIN}, ParticipantId: {ParticipantId}, Name: {Name}, ConnectionId: {ConnectionId}", 
            pin, participantId, participantName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, pin);
        ConnectionToParticipant[Context.ConnectionId] = (pin, participantId);
        await Clients.Group(pin).SendAsync("ParticipantJoined", new { Pin = pin, ParticipantId = participantId, ParticipantName = participantName });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "SignalR client disconnected with error. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("SignalR client disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        if (ConnectionToParticipant.TryRemove(Context.ConnectionId, out var mapping))
        {
            _logger.LogInformation("Removing participant mapping. PIN: {PIN}, ParticipantId: {ParticipantId}, ConnectionId: {ConnectionId}", 
                mapping.Pin, mapping.ParticipantId, Context.ConnectionId);
            var hasOtherConnections = ConnectionToParticipant.Values.Any(v => v.Pin == mapping.Pin && v.ParticipantId == mapping.ParticipantId);
            if (!hasOtherConnections)
            {
                _logger.LogInformation("No other connections for participant. Cleaning up. PIN: {PIN}, ParticipantId: {ParticipantId}", 
                    mapping.Pin, mapping.ParticipantId);
                var session = await _context.Sessions
                    .Include(s => s.Participants)
                    .Include(s => s.Votes)
                    .FirstOrDefaultAsync(s => s.PIN == mapping.Pin);
                if (session != null)
                {
                    var participant = await _context.Participants
                        .FirstOrDefaultAsync(p => p.ParticipantId == mapping.ParticipantId && p.SessionId == session.SessionId);

                    if (participant != null)
                    {
                        var host = session.Participants.OrderBy(p => p.JoinedAt).FirstOrDefault();
                        if (host != null && participant.ParticipantId == host.ParticipantId)
                        {
                            _logger.LogInformation("Host disconnected. Ending session. PIN: {PIN}, HostId: {ParticipantId}", 
                                mapping.Pin, participant.ParticipantId);
                            await Clients.Group(mapping.Pin).SendAsync("SessionEnded", new { Pin = mapping.Pin });
                            _context.Sessions.Remove(session);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            _logger.LogInformation("Participant left. PIN: {PIN}, ParticipantId: {ParticipantId}, Name: {Name}", 
                                mapping.Pin, participant.ParticipantId, participant.Name);
                            _context.Participants.Remove(participant);
                            await _context.SaveChangesAsync();
                            await Clients.Group(mapping.Pin).SendAsync("ParticipantLeft", new ParticipantDto
                            {
                                ParticipantId = participant.ParticipantId,
                                Name = participant.Name,
                                JoinedAt = participant.JoinedAt
                            });
                        }
                    }
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task LeaveSession(string pin)
    {
        _logger.LogInformation("SignalR client explicitly leaving session. PIN: {PIN}, ConnectionId: {ConnectionId}", pin, Context.ConnectionId);
        if (ConnectionToParticipant.TryGetValue(Context.ConnectionId, out var mapping) && mapping.Pin == pin)
        {
            ConnectionToParticipant.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, pin);

            var session = await _context.Sessions
                .Include(s => s.Participants)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.PIN == pin);
            if (session != null)
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.ParticipantId == mapping.ParticipantId && p.SessionId == session.SessionId);
                if (participant != null)
                {
                    var host = session.Participants.OrderBy(p => p.JoinedAt).FirstOrDefault();
                    if (host != null && participant.ParticipantId == host.ParticipantId)
                    {
                        _logger.LogInformation("Host leaving. Ending session. PIN: {PIN}, HostId: {ParticipantId}", 
                            pin, participant.ParticipantId);
                        await Clients.Group(pin).SendAsync("SessionEnded", new { Pin = pin });
                        _context.Sessions.Remove(session);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogInformation("Participant leaving. PIN: {PIN}, ParticipantId: {ParticipantId}, Name: {Name}", 
                            pin, participant.ParticipantId, participant.Name);
                        _context.Participants.Remove(participant);
                        await _context.SaveChangesAsync();
                        await Clients.Group(pin).SendAsync("ParticipantLeft", new ParticipantDto
                        {
                            ParticipantId = participant.ParticipantId,
                            Name = participant.Name,
                            JoinedAt = participant.JoinedAt
                        });
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("LeaveSession called but no mapping found or PIN mismatch. PIN: {PIN}, ConnectionId: {ConnectionId}", 
                pin, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, pin);
        }
    }

    public async Task VoteSubmitted(string pin, int participantId, string cardValue)
    {
        await Clients.Group(pin).SendAsync("VoteSubmitted", new { Pin = pin, ParticipantId = participantId, CardValue = cardValue });
    }

    public async Task VotesRevealed(string pin)
    {
        await Clients.Group(pin).SendAsync("VotesRevealed", new { Pin = pin });
    }

    public async Task HostModeChanged(string pin, int participantId, bool isHostOnly)
    {
        await Clients.Group(pin).SendAsync("HostModeChanged", new { Pin = pin, ParticipantId = participantId, IsHostOnly = isHostOnly });
    }
}
