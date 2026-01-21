namespace PlanningPoker.Api.DTOs;

public class SessionDto
{
    public int SessionId { get; set; }
    public string PIN { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ParticipantDto>? Participants { get; set; }
}
