using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Hubs;

public class PlanningPokerHub : Hub
{
    public async Task JoinSession(string pin, string participantName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, pin);
        await Clients.Group(pin).SendAsync("ParticipantJoined", new { Pin = pin, ParticipantName = participantName });
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
