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
                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == mapping.Pin);
                if (session != null)
                {
                    var participant = await _context.Participants
                        .FirstOrDefaultAsync(p => p.ParticipantId == mapping.ParticipantId && p.SessionId == session.SessionId);

                    if (participant != null)
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

        await base.OnDisconnectedAsync(exception);
    }

    public async Task LeaveSession(string pin)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, pin);
    }

    public async Task VoteSubmitted(string pin, int participantId, string cardValue)
    {
        await Clients.Group(pin).SendAsync("VoteSubmitted", new { Pin = pin, ParticipantId = participantId, CardValue = cardValue });
    }

    public async Task VotesRevealed(string pin)
    {
        await Clients.Group(pin).SendAsync("VotesRevealed", new { Pin = pin });
    }
}
