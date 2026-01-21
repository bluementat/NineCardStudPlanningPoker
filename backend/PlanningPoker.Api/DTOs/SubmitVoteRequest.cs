namespace PlanningPoker.Api.DTOs;

public class SubmitVoteRequest
{
    public int ParticipantId { get; set; }
    public string CardValue { get; set; } = string.Empty;
}
