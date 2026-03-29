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
        "hmrc", "inland revenue", "tax refund", "dividend"
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
        "databricks", "mongodb", "elastic"
    ];

    private static readonly string[] MediaKeywords =
    [
        "spotify", "netflix", "apple music", "youtube premium", "amazon prime", "prime video",
        "disney+", "hbo", "hulu", "paramount", "audible", "kindle unlimited", "deezer", "tidal"
    ];

    private static readonly string[] TaxKeywords =
    [
        "hmrc payment", "self assessment", "tax payment", "vat payment"
    ];

    public static RecurringClassificationResult Classify(string vendorName, string? description, int patternConfidence)
    {
        var blob = $"{vendorName} {description ?? ""}".ToLowerInvariant();
        var normalized = NormalizeMerchant(vendorName);

        if (ContainsAny(blob, TaxKeywords))
            return new RecurringClassificationResult(RecurringType.OtherRecurringExpense, 12,
                "Matched tax authority / tax payment keywords");

        if (ContainsAny(blob, SalaryKeywords))
            return new RecurringClassificationResult(RecurringType.Salary, 8,
                "Matched salary / payroll keywords");

        if (ContainsAny(blob, TransferKeywords))
            return new RecurringClassificationResult(RecurringType.Transfer, 10,
                "Matched transfer / movement keywords");

        if (ContainsAny(blob, RentMortgageKeywords))
            return new RecurringClassificationResult(RecurringType.Rent, 15,
                "Matched rent / mortgage / council tax keywords");

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
        return new RecurringClassificationResult(RecurringType.UnknownRecurring, blended,
            "No strong merchant category; score blends recurring pattern strength only");
    }

    public static string NormalizeMerchant(string vendorName) =>
        string.Join(' ', vendorName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Likely subscription row for default UI (software/media, high confidence).
    /// </summary>
    public static bool IsLikelySubscriptionRow(RecurringType type, int classificationScore) =>
        classificationScore >= 70 &&
        (type == RecurringType.SoftwareSubscription || type == RecurringType.MediaSubscription);

    /// <summary>
    /// Review bucket: worth human review but not auto-listed as subscription.
    /// </summary>
    public static bool IsReviewBucketRow(RecurringType type, int classificationScore) =>
        classificationScore is >= 40 and < 70 &&
        (type is RecurringType.SoftwareSubscription or RecurringType.MediaSubscription
            or RecurringType.UnknownRecurring);

    /// <summary>Duplicate-tool alerts: ignore payroll, transfers, and very low-confidence noise.</summary>
    public static bool IncludedInDuplicateDetection(RecurringType type, int classificationScore) =>
        classificationScore >= 45 &&
        type is not (RecurringType.Salary or RecurringType.Transfer or RecurringType.Rent);

    /// <summary>Renewal / suspected-unused / owner workflows — only for subscription-like rows.</summary>
    public static bool IsEligibleForSubscriptionAlerts(RecurringType type, int classificationScore) =>
        (type is RecurringType.SoftwareSubscription or RecurringType.MediaSubscription && classificationScore >= 45)
        || (type == RecurringType.UnknownRecurring && classificationScore >= 60);

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
