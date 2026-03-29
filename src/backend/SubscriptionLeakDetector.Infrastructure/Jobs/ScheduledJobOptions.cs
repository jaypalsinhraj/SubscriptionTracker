namespace SubscriptionLeakDetector.Infrastructure.Jobs;

/// <summary>
/// Configuration binding for timer-based jobs (worker). Azure Container Apps Jobs use separate trigger config in Bicep.
/// </summary>
public sealed class ScheduledJobOptions
{
    public const string SectionName = "Worker";
    public int IntervalMinutes { get; set; } = 60;
}
