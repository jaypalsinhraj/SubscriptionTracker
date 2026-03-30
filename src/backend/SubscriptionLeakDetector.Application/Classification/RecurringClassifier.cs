using SubscriptionLeakDetector.Application.MerchantNormalization;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Classification;

/// <summary>
/// Keyword/scoring pass after recurring pattern detection. Keeps logic testable and separate from cadence math.
/// </summary>
public static class RecurringClassifier
{
    private static readonly string[] SalaryKeywords =
    [
        "salary", "payroll", "wage", "gross pay", "net pay", "employer", "p60", "p45",
        "hmrc", "inland revenue", "acme payroll"
    ];

    private static readonly string[] TransferKeywords =
    [
        "transfer to", "transfer from", "faster payment", "chaps", "internal transfer",
        "savings", "isa transfer", "pot ", "between accounts", "tfr ", "xfer"
    ];

    private static readonly string[] RentMortgageKeywords =
    [
        "rent", "letting", "landlord", "estate agent", "mortgage", "halifax mort", "nationwide mort",
        "hsbc mort", "barclays mort", "council tax"
    ];

    private static readonly string[] UtilityKeywords =
    [
        "british gas", "scottish power", "edf ", "eon ", "ovo energy", "bulb energy", "octopus energy",
        "water bill", "thames water", "severn trent", "utilities", "electricity", "gas bill",
        "sse ", "npower", "utility warehouse"
    ];

    private static readonly string[] TelecomKeywords =
    [
        "virgin media", "bt group", "bt sport", "sky ltd", "sky tv", "broadband", "fibre",
        "ee limited", "ee mobile", "vodafone", "o2 ", "three uk", "talktalk", "plusnet", "openreach"
    ];

    private static readonly string[] InsuranceKeywords =
    [
        "insurance", "aviva", "direct line", "admiral", "hastings", "churchill", "prudential",
        "life cover", "home insurance", "car insurance", "pet insurance"
    ];

    private static readonly string[] LoanKeywords =
    [
        "loan repayment", "loan payment", "personal loan", "zopa", "ratesetter", "klarna",
        "clearpay", "affirm", "credit card minimum", "amex payment", "visa payment"
    ];

    private static readonly string[] SoftwareSaasKeywords =
    [
        "adobe", "microsoft 365", "office 365", "azure", "aws", "amazon web services", "google workspace",
        "github", "gitlab", "atlassian", "jira", "slack", "zoom", "dropbox", "notion", "figma", "canva",
        "openai", "chatgpt", "cursor", "jetbrains", "heroku", "vercel", "cloudflare", "salesforce",
        "hubspot", "zendesk", "okta", "auth0", "datadog", "new relic", "splunk", "snowflake",
        "databricks", "mongodb", "elastic", "stripe", "notionlabs"
    ];

    private static readonly string[] MediaKeywords =
    [
        "spotify", "netflix", "apple music", "youtube premium", "amazon prime", "prime video",
        "disney+", "hbo", "hulu", "paramount", "audible", "kindle unlimited", "deezer", "tidal"
    ];

    private static readonly string[] RecurringIncomeKeywords =
    [
        "tax refund", "dividend", "interest received", "cashback", "rebate"
    ];

    private static readonly string[] TaxKeywords =
    [
        "hmrc payment", "self assessment", "tax payment", "vat payment"
    ];

    public static RecurringClassificationResult Classify(string vendorName, string? description, int patternConfidence,
        MerchantNormalizationResult? normalization = null)
    {
        var blob = $"{vendorName} {description ?? ""}".ToLowerInvariant();
        var normalizedKey = NormalizeMerchant(vendorName);

        if (normalization?.HintedRecurringType is { } hint && normalization.Value.Confidence >= 70)
        {
            var fromAlias = ClassifyFromAliasHint(hint, patternConfidence, normalization.Value);
            if (fromAlias != null) return fromAlias.Value;
        }

        if (ContainsAny(blob, TaxKeywords))
            return new RecurringClassificationResult(RecurringType.OtherRecurringExpense, 12,
                "Matched tax authority / tax payment keywords");

        if (ContainsAny(blob, RecurringIncomeKeywords))
            return new RecurringClassificationResult(RecurringType.RecurringIncome, 18,
                "Matched recurring income / refund-style keywords");

        if (ContainsAny(blob, SalaryKeywords))
            return new RecurringClassificationResult(RecurringType.Salary, 8,
                "Matched salary / payroll keywords");

        if (ContainsAny(blob, TransferKeywords))
            return new RecurringClassificationResult(RecurringType.Transfer, 10,
                "Matched transfer / movement keywords");

        if (ContainsAny(blob, RentMortgageKeywords))
            return new RecurringClassificationResult(RecurringType.Rent, 15,
                "Matched rent / mortgage / council tax keywords");

        if (ContainsAny(blob, TelecomKeywords))
            return new RecurringClassificationResult(RecurringType.Telecom, 22,
                "Matched telecom / broadband keywords");

        if (ContainsAny(blob, UtilityKeywords))
            return new RecurringClassificationResult(RecurringType.UtilityBill, 12,
                "Matched energy / water / utility keywords");

        if (ContainsAny(blob, InsuranceKeywords))
            return new RecurringClassificationResult(RecurringType.Insurance, 18,
                "Matched insurance keywords");

        if (ContainsAny(blob, LoanKeywords))
            return new RecurringClassificationResult(RecurringType.LoanPayment, 20,
                "Matched loan / credit repayment keywords");

        if (ContainsAny(blob, SoftwareSaasKeywords))
        {
            var score = CombineScores(88, patternConfidence);
            return new RecurringClassificationResult(RecurringType.SoftwareSubscription, score,
                "Matched software / SaaS merchant keywords");
        }

        if (ContainsAny(blob, MediaKeywords))
        {
            var score = CombineScores(85, patternConfidence);
            return new RecurringClassificationResult(RecurringType.MediaSubscription, score,
                "Matched media / streaming keywords");
        }

        var blended = Math.Clamp((int)(patternConfidence * 0.55 + 20), 0, 100);
        var reason = normalization?.Confidence >= 60
            ? $"Weak text match; alias confidence {normalization.Value.Confidence} blended with pattern"
            : "No strong merchant category; score blends recurring pattern strength only";
        return new RecurringClassificationResult(RecurringType.UnknownRecurring, blended, reason);
    }

    private static RecurringClassificationResult? ClassifyFromAliasHint(RecurringType hint, int patternConfidence,
        MerchantNormalizationResult normalization)
    {
        var reason = $"Merchant alias ({normalization.MatchedRuleName ?? "rule"}): {normalization.DisplayName}";
        return hint switch
        {
            RecurringType.SoftwareSubscription => new RecurringClassificationResult(hint,
                CombineScores(90, patternConfidence), reason),
            RecurringType.MediaSubscription => new RecurringClassificationResult(hint,
                CombineScores(88, patternConfidence), reason),
            RecurringType.UtilityBill => new RecurringClassificationResult(hint, 14,
                reason + " — utility alias"),
            RecurringType.Telecom => new RecurringClassificationResult(hint, 20,
                reason + " — telecom alias"),
            RecurringType.Transfer => new RecurringClassificationResult(hint, 12,
                reason + " — transfer alias"),
            RecurringType.Salary => new RecurringClassificationResult(hint, 10,
                reason + " — payroll alias"),
            RecurringType.RecurringIncome => new RecurringClassificationResult(hint, 15,
                reason + " — income alias"),
            _ => null
        };
    }

    public static string NormalizeMerchant(string vendorName) =>
        string.Join(' ', vendorName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Likely subscription row for default UI: keyword-matched SaaS/media, or strong recurring pattern with
    /// no category keyword (unknown) at the same confidence threshold.
    /// </summary>
    public static bool IsLikelySubscriptionRow(RecurringType type, int subscriptionConfidenceScore) =>
        subscriptionConfidenceScore >= 70 &&
        (type == RecurringType.SoftwareSubscription || type == RecurringType.MediaSubscription
         || type == RecurringType.UnknownRecurring);

    /// <summary>
    /// Review bucket: worth human review but not auto-listed as subscription.
    /// </summary>
    public static bool IsReviewBucketRow(RecurringType type, int subscriptionConfidenceScore) =>
        subscriptionConfidenceScore is >= 40 and < 70 &&
        (type is RecurringType.SoftwareSubscription or RecurringType.MediaSubscription
            or RecurringType.UnknownRecurring);

    /// <summary>Duplicate-tool alerts: ignore payroll, transfers, and very low-confidence noise.</summary>
    public static bool IncludedInDuplicateDetection(RecurringType type, int subscriptionConfidenceScore) =>
        subscriptionConfidenceScore >= 45 &&
        type is not (RecurringType.Salary or RecurringType.Transfer or RecurringType.Rent
            or RecurringType.RecurringIncome);

    /// <summary>Renewal / suspected-unused / owner workflows — only for subscription-like rows.</summary>
    public static bool IsEligibleForSubscriptionAlerts(RecurringType type, int subscriptionConfidenceScore) =>
        (type is RecurringType.SoftwareSubscription or RecurringType.MediaSubscription && subscriptionConfidenceScore >= 45)
        || (type == RecurringType.UnknownRecurring && subscriptionConfidenceScore >= 60);

    /// <summary>Patterns that are clearly not SaaS subscriptions but still recurring.</summary>
    public static bool IsNonSubscriptionRecurringType(RecurringType type) =>
        type is RecurringType.UtilityBill or RecurringType.Salary or RecurringType.Transfer
            or RecurringType.Rent or RecurringType.Insurance or RecurringType.LoanPayment
            or RecurringType.Telecom or RecurringType.RecurringIncome
            or RecurringType.OtherRecurringExpense;

    private static int CombineScores(int keywordFloor, int patternConfidence) =>
        Math.Clamp(Math.Max(keywordFloor, (keywordFloor + patternConfidence) / 2), 0, 100);

    private static bool ContainsAny(string text, IEnumerable<string> needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
