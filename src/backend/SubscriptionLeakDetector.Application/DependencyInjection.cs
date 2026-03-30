using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionLeakDetector.Application.Accounts;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Dashboard;
using SubscriptionLeakDetector.Application.MerchantNormalization;
using SubscriptionLeakDetector.Application.Recurring;
using SubscriptionLeakDetector.Application.Subscriptions;
using SubscriptionLeakDetector.Application.Transactions;

namespace SubscriptionLeakDetector.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountDataResetService, AccountDataResetService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITransactionImportService, TransactionImportService>();
        services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
        services.AddScoped<ISubscriptionReviewService, SubscriptionReviewService>();
        services.AddScoped<IAlertQueryService, AlertQueryService>();
        services.AddScoped<IRecurringDetectionService, RecurringDetectionService>();
        services.AddScoped<IAlertGenerationService, AlertGenerationService>();
        services.AddScoped<IMerchantNormalizationService, MerchantNormalizationService>();
        services.AddScoped<IRecurringReviewQueryService, RecurringReviewQueryService>();
        services.AddScoped<IRecurringCandidateActionService, RecurringCandidateActionService>();

        services.AddValidatorsFromAssemblyContaining<Transactions.ImportTransactionRequestValidator>();
        return services;
    }
}
