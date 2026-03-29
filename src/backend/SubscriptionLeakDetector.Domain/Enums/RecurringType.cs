namespace SubscriptionLeakDetector.Domain.Enums;

/// <summary>
/// Classification of a recurring payment pattern (separate from cadence detection).
/// </summary>
public enum RecurringType
{
    UnknownRecurring = 0,
    SoftwareSubscription = 1,
    MediaSubscription = 2,
    UtilityBill = 3,
    Salary = 4,
    Transfer = 5,
    Insurance = 6,
    LoanPayment = 7,
    Rent = 8,
    OtherRecurringExpense = 9
}
