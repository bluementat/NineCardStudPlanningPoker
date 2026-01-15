namespace PlanningPoker.Api.Models;

public class Participant
{
    public int ParticipantId { get; set; }
    public int SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    
    // Navigation properties
    public Session Session { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
