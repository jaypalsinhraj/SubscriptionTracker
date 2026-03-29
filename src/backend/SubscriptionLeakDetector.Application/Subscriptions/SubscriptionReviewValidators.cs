using FluentValidation;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public sealed class AssignOwnerRequestValidator : AbstractValidator<AssignOwnerRequest>
{
    public AssignOwnerRequestValidator()
    {
        RuleFor(x => x.OwnerName).MaximumLength(200).When(x => x.OwnerName != null);
        RuleFor(x => x.OwnerEmail).MaximumLength(320).EmailAddress().When(x => !string.IsNullOrEmpty(x.OwnerEmail));
    }
}

public sealed class RespondToAlertRequestValidator : AbstractValidator<RespondToAlertRequest>
{
    public RespondToAlertRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
