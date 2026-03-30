using FluentValidation;

namespace SubscriptionLeakDetector.Application.Accounts;

public sealed class ResetAccountDataRequestValidator : AbstractValidator<ResetAccountDataRequest>
{
    public ResetAccountDataRequestValidator()
    {
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Confirm must be true to delete all account data.");
    }
}
