namespace PlanningPoker.Api.DTOs;

public class VoteResultDto
{
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CardValue { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; }
}
