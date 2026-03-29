using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Infrastructure.Identity;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
