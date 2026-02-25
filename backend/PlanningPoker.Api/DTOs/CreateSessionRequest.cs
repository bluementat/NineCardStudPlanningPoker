namespace PlanningPoker.Api.DTOs;

public class CreateSessionRequest
{
    public string SessionName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
}
