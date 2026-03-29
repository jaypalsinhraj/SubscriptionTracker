using FluentValidation;

namespace SubscriptionLeakDetector.Application.Transactions;

public class ImportTransactionRequestValidator : AbstractValidator<ImportTransactionRequest>
{
    public ImportTransactionRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.CsvContent).NotEmpty().MaximumLength(5_000_000);
    }
}
