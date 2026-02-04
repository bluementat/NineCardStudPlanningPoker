using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Hubs;

namespace PlanningPoker.Api.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
    private readonly int _sessionExpirationMinutes = 60;

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service is starting.");

        using PeriodicTimer timer = new(_cleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session Cleanup Service is stopping.");
        }
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Checking for expired sessions...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlanningPokerDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PlanningPokerHub>>();

        var expirationTime = DateTime.UtcNow.AddMinutes(-_sessionExpirationMinutes);

        var expiredSessions = await dbContext.Sessions
            .Where(s => s.CreatedAt < expirationTime)
            .ToListAsync(stoppingToken);

        if (expiredSessions.Any())
        {
            _logger.LogInformation("Found {Count} expired sessions to clean up.", expiredSessions.Count);

            foreach (var session in expiredSessions)
            {
                try
                {
                    // Notify connected users in the session group
                    await hubContext.Clients.Group(session.PIN).SendAsync("SessionEnded", new { Pin = session.PIN }, stoppingToken);
                    
                    _logger.LogInformation("Notified users in session {PIN} that it has ended.", session.PIN);

                    // Remove the session (cascading deletes will handle participants and votes)
                    dbContext.Sessions.Remove(session);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up session {PIN}.", session.PIN);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Cleanup complete.");
        }
        else
        {
            _logger.LogInformation("No expired sessions found.");
        }
    }
}
