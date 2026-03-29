using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Subscriptions;
using SubscriptionLeakDetector.Infrastructure.Persistence;

namespace SubscriptionLeakDetector.Worker.Jobs;

/// <summary>
/// Periodically runs detection and alert generation for all accounts (single-account MVP; one account per user).
/// </summary>
public sealed class SubscriptionDetectionJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SubscriptionDetectionJob> _logger;
    private readonly IConfiguration _config;

    public SubscriptionDetectionJob(IServiceProvider services, ILogger<SubscriptionDetectionJob> logger,
        IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_config.GetValue("Worker:IntervalMinutes", 60));
        _logger.LogInformation("Subscription detection job started. Interval: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var detection = scope.ServiceProvider.GetRequiredService<IRecurringDetectionService>();
                var alerts = scope.ServiceProvider.GetRequiredService<IAlertGenerationService>();

                var accountIds = await db.Accounts.AsNoTracking().Select(a => a.Id).ToListAsync(stoppingToken);
                foreach (var accountId in accountIds)
                {
                    await detection.RunForAccountAsync(accountId, stoppingToken);
                    await alerts.GenerateForAccountAsync(accountId, stoppingToken);
                    _logger.LogInformation("Processed account {AccountId}", accountId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription detection job failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
