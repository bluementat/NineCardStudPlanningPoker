namespace PlanningPoker.Api.Models;

public class Session
{
    public int SessionId { get; set; }
    public string PIN { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public SessionStatus Status { get; set; }
    public int? HostParticipantId { get; set; }
    
    // Navigation properties
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

public enum SessionStatus
{
    Active,
    Completed
}
