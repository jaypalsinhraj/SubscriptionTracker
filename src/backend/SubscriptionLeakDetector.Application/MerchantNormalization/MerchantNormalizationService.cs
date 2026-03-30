using System.Text.RegularExpressions;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.MerchantNormalization;

public sealed class MerchantNormalizationService : IMerchantNormalizationService
{
    private static readonly Regex NoiseRef = new(@"\bP\d+[A-Z0-9]*\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

    private static readonly MerchantAliasRules.Rule[] RulesSorted = MerchantAliasRules.All
        .OrderByDescending(r => r.Pattern.Length)
        .ToArray();

    public MerchantNormalizationResult Normalize(string? rawDescription, string? vendorName, string? rawCategory)
    {
        var combined = string.Join(" ",
            new[] { rawDescription, vendorName, rawCategory }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(combined))
            return new MerchantNormalizationResult("unknown", "Unknown", 0, "No description text", null, null);

        var cleaned = CleanForMatch(combined);
        var blob = cleaned.ToLowerInvariant();

        foreach (var rule in RulesSorted)
        {
            if (!blob.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase)) continue;
            var key = CollapseKey(rule.DisplayName);
            return new MerchantNormalizationResult(key, rule.DisplayName, 92,
                $"Alias match: {rule.DisplayName}", rule.RuleName, rule.Hint);
        }

        // Payment processor tail: STRIPE*MERCHANT, PAYPAL *MERCHANT
        var processor = TryProcessorTail(blob);
        if (processor != null)
        {
            var key = CollapseKey(processor.Value.Display);
            return new MerchantNormalizationResult(key, processor.Value.Display, 78,
                "Payment processor prefix stripped", "processor_tail", processor.Value.Hint);
        }

        var fallbackKey = HeuristicKey(blob);
        var display = ToTitle(fallbackKey);
        return new MerchantNormalizationResult(fallbackKey, display, 45,
            "Heuristic collapse — no alias matched", null, null);
    }

    private static string CleanForMatch(string s)
    {
        s = s.Replace('*', ' ');
        s = NoiseRef.Replace(s, " ");
        s = MultiSpace.Replace(s, " ").Trim();
        return s;
    }

    private static (string Display, RecurringType? Hint)? TryProcessorTail(string blobLower)
    {
        foreach (var prefix in new[] { "stripe*", "stripe ", "paypal *", "paypal ", "sq *", "sumup *" })
        {
            var idx = blobLower.IndexOf(prefix, StringComparison.Ordinal);
            if (idx < 0) continue;
            var tail = blobLower[(idx + prefix.Length)..].Trim();
            tail = tail.TrimStart('*', ' ');
            if (tail.Length < 2) continue;
            var word = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrEmpty(word)) continue;
            return (ToTitle(word), GuessHintFromTail(word));
        }

        return null;
    }

    private static RecurringType? GuessHintFromTail(string word)
    {
        if (word.Contains("notion", StringComparison.OrdinalIgnoreCase)) return RecurringType.SoftwareSubscription;
        if (word.Contains("canva", StringComparison.OrdinalIgnoreCase)) return RecurringType.SoftwareSubscription;
        return null;
    }

    private static string HeuristicKey(string blobLower)
    {
        var stripped = blobLower;
        foreach (var noise in new[] { ".com", ".co.uk", " ltd", " limited", " inc" })
            stripped = stripped.Replace(noise, "", StringComparison.OrdinalIgnoreCase);

        stripped = MultiSpace.Replace(stripped, " ").Trim();
        var parts = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "unknown";
        return string.Join(' ', parts.Take(Math.Min(4, parts.Length)));
    }

    private static string CollapseKey(string displayName) =>
        string.Join(' ', displayName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string ToTitle(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "Unknown";
        return string.Join(' ',
            key.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(static p =>
                p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
