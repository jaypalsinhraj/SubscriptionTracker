using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence;

public sealed class DevDataSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(IServiceProvider services, ILogger<DevDataSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment()) return;

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Accounts.AnyAsync(cancellationToken)) return;

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var uiCulture = config["Globalization:DefaultCulture"] ?? "en-US";
        var defaultCurrency = config["Globalization:DefaultCurrency"] ?? "USD";

        db.Accounts.Add(new Account
        {
            Id = accountId,
            Name = "Dev Account",
            UiCulture = uiCulture,
            DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        db.Users.Add(new User
        {
            Id = userId,
            AccountId = accountId,
            ExternalId = "dev-user",
            Email = "dev@local.test",
            DisplayName = "Dev User",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Development seed data created for account {AccountId}", accountId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
