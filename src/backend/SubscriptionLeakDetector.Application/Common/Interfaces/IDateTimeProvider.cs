namespace SubscriptionLeakDetector.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly TodayUtc { get; }
}
