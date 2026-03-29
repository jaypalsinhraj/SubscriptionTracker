using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Infrastructure.Persistence;
using SubscriptionLeakDetector.Infrastructure.Storage;

namespace SubscriptionLeakDetector.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDateTimeProvider, Identity.SystemDateTimeProvider>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        var storageSection = configuration.GetSection(AzureBlobStorageOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(storageSection["ConnectionString"]))
        {
            services.Configure<AzureBlobStorageOptions>(storageSection);
            services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddScoped<IBlobStorageService, NoOpBlobStorageService>();
        }

        return services;
    }
}
