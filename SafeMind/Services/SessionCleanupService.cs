using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using Data.Enums;

namespace SafeMind.Services;

/// <summary>
/// Background service that automatically cancels unpaid sessions
/// when they are less than 24 hours away from their start time.
/// Runs every 15 minutes.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public SessionCleanupService(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelUnpaidSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling unpaid sessions.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CancelUnpaidSessionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeMindDbContext>();

        var cutoff = DateTimeOffset.UtcNow.AddHours(24);

        var unpaid = await db.Sessions
            .Where(s => s.SessionStatus != SessionStatus.Cancelled
                     && s.PaymentStatus == PaymentStatus.Pending
                     && s.StartTime <= cutoff)
            .ToListAsync();

        if (unpaid.Count == 0) return;

        foreach (var session in unpaid)
        {
            session.SessionStatus = SessionStatus.Cancelled;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Auto-cancelled {Count} unpaid session(s).", unpaid.Count);
    }
}
