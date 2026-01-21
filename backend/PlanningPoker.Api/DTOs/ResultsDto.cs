namespace PlanningPoker.Api.DTOs;

public class ResultsDto
{
    public List<VoteResultDto> Votes { get; set; } = new();
    public StatisticsDto? Statistics { get; set; }
}
