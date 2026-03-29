using System.Globalization;

namespace SubscriptionLeakDetector.Application.Transactions;

public static class CsvTransactionParser
{
    /// <param name="defaultCurrency">ISO 4217 used when the CSV has no currency column or blank values.</param>
    public static IReadOnlyList<CsvTransactionRow> Parse(string csvContent, string defaultCurrency = "USD")
    {
        var lines = csvContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
            throw new InvalidOperationException("CSV must include a header row and at least one data row.");

        var header = SplitLine(lines[0]);
        var dateIdx = FindColumn(header, "date", "transactiondate", "posted");
        var amountIdx = FindColumn(header, "amount", "value", "debit");
        var vendorIdx = FindColumn(header, "vendor", "merchant", "description", "payee", "name");
        var currencyIdx = FindOptionalColumn(header, "currency", "curr");

        if (dateIdx < 0 || amountIdx < 0 || vendorIdx < 0)
            throw new InvalidOperationException(
                "CSV must include columns for Date, Amount, and Vendor (or Description/Merchant).");

        var rows = new List<CsvTransactionRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitLine(lines[i]);
            if (cols.Length == 0) continue;

            var dateStr = Get(cols, dateIdx);
            var amountStr = Get(cols, amountIdx);
            var vendor = Get(cols, vendorIdx);
            var currency = currencyIdx >= 0 ? Get(cols, currencyIdx) : defaultCurrency;
            if (string.IsNullOrWhiteSpace(vendor)) continue;

            if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                if (!DateOnly.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                    continue;

            if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rawAmount))
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.CurrentCulture, out rawAmount))
                    continue;

            // UK-style exports: positive = money in (credit), negative = spend (debit). Subscription detection uses debits only.
            var isCredit = rawAmount > 0;
            var amount = Math.Abs(rawAmount);
            if (string.IsNullOrWhiteSpace(currency)) currency = defaultCurrency;

            var iso = currency.Trim().ToUpperInvariant();
            if (iso.Length != 3) iso = defaultCurrency.Trim().ToUpperInvariant();
            rows.Add(new CsvTransactionRow(date, amount, isCredit, vendor.Trim(), iso));
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("No valid transaction rows were found in the CSV.");

        return rows;
    }

    private static string[] SplitLine(string line) =>
        line.Split(',', StringSplitOptions.TrimEntries);

    private static int FindColumn(string[] header, params string[] names)
    {
        for (var i = 0; i < header.Length; i++)
        {
            var h = Normalize(header[i]);
            foreach (var n in names)
                if (h == Normalize(n)) return i;
        }

        return -1;
    }

    private static int FindOptionalColumn(string[] header, params string[] names) => FindColumn(header, names);

    private static string Normalize(string s) =>
        new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string Get(IReadOnlyList<string> cols, int idx) =>
        idx < cols.Count ? cols[idx] : string.Empty;
}
