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

    public PlanningPokerHub(PlanningPokerDbContext context)
    {
        _context = context;
    }

    public async Task JoinSession(string pin, int participantId, string participantName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, pin);
        ConnectionToParticipant[Context.ConnectionId] = (pin, participantId);
        await Clients.Group(pin).SendAsync("ParticipantJoined", new { Pin = pin, ParticipantId = participantId, ParticipantName = participantName });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToParticipant.TryRemove(Context.ConnectionId, out var mapping))
        {
            var hasOtherConnections = ConnectionToParticipant.Values.Any(v => v.Pin == mapping.Pin && v.ParticipantId == mapping.ParticipantId);
            if (!hasOtherConnections)
            {
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
                            await Clients.Group(mapping.Pin).SendAsync("SessionEnded", new { Pin = mapping.Pin });
                            _context.Sessions.Remove(session);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
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
                        await Clients.Group(pin).SendAsync("SessionEnded", new { Pin = pin });
                        _context.Sessions.Remove(session);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
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
