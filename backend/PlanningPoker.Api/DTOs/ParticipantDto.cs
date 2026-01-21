namespace PlanningPoker.Api.DTOs;

public class ParticipantDto
{
    public int ParticipantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
