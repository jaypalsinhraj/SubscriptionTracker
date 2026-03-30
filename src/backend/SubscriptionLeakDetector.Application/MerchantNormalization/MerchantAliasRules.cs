using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.MerchantNormalization;

/// <summary>
/// Seed alias rules (pattern substring match on cleaned blob, longest pattern wins). Extend via future config/DB.
/// </summary>
internal static class MerchantAliasRules
{
    internal readonly record struct Rule(string Pattern, string DisplayName, string RuleName, RecurringType? Hint);

    /// <summary>Ordered by pattern length descending at runtime.</summary>
    internal static readonly Rule[] All =
    [
        new("amazon web services", "Amazon Web Services", "alias:aws_full", RecurringType.SoftwareSubscription),
        new("google *youtube", "YouTube", "alias:youtube", RecurringType.MediaSubscription),
        new("google youtube", "YouTube", "alias:youtube_2", RecurringType.MediaSubscription),
        new("microsoft azure", "Microsoft Azure", "alias:azure", RecurringType.SoftwareSubscription),
        new("office 365", "Microsoft 365", "alias:m365", RecurringType.SoftwareSubscription),
        new("microsoft 365", "Microsoft 365", "alias:m365_2", RecurringType.SoftwareSubscription),
        new("google workspace", "Google Workspace", "alias:google_workspace", RecurringType.SoftwareSubscription),
        new("virgin media", "Virgin Media", "alias:virgin_media", RecurringType.Telecom),
        new("octopus energy", "Octopus Energy", "alias:octopus", RecurringType.UtilityBill),
        new("british gas", "British Gas", "alias:british_gas", RecurringType.UtilityBill),
        new("internal transfer", "Internal Transfer", "alias:internal_transfer", RecurringType.Transfer),
        new("transfer to savings", "Internal Transfer", "alias:to_savings", RecurringType.Transfer),
        new("stripe notion", "Notion", "alias:stripe_notion", RecurringType.SoftwareSubscription),
        new("paypal *canva", "Canva", "alias:paypal_canva", RecurringType.SoftwareSubscription),
        new("amzn aws", "Amazon Web Services", "alias:amzn_aws", RecurringType.SoftwareSubscription),
        new("aws emea", "Amazon Web Services", "alias:aws_emea", RecurringType.SoftwareSubscription),
        new("spotify", "Spotify", "alias:spotify", RecurringType.MediaSubscription),
        new("netflix", "Netflix", "alias:netflix", RecurringType.MediaSubscription),
        new("youtube", "YouTube", "alias:youtube_short", RecurringType.MediaSubscription),
        new("notion", "Notion", "alias:notion", RecurringType.SoftwareSubscription),
        new("adobe", "Adobe", "alias:adobe", RecurringType.SoftwareSubscription),
        new("slack", "Slack", "alias:slack", RecurringType.SoftwareSubscription),
        new("github", "GitHub", "alias:github", RecurringType.SoftwareSubscription),
        new("atlassian", "Atlassian", "alias:atlassian", RecurringType.SoftwareSubscription),
        new("openai", "OpenAI", "alias:openai", RecurringType.SoftwareSubscription),
        new("azure", "Microsoft Azure", "alias:azure_short", RecurringType.SoftwareSubscription),
        new("payroll", "Payroll", "alias:payroll", RecurringType.Salary),
        new("salary", "Salary", "alias:salary", RecurringType.Salary),
        new("dividend", "Dividend", "alias:dividend", RecurringType.RecurringIncome),
        new("interest", "Interest", "alias:interest", RecurringType.RecurringIncome),
    ];
}
