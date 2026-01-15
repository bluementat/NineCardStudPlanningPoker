namespace PlanningPoker.Api.Models;

public class Vote
{
    public int VoteId { get; set; }
    public int SessionId { get; set; }
    public int ParticipantId { get; set; }
    public string CardValue { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; }
    
    // Navigation properties
    public Session Session { get; set; } = null!;
    public Participant Participant { get; set; } = null!;
}
