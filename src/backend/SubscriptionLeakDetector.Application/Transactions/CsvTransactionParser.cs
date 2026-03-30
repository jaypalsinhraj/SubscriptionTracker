using System.Globalization;
using Microsoft.Extensions.Logging;

namespace SubscriptionLeakDetector.Application.Transactions;

public static class CsvTransactionParser
{
    /// <param name="defaultCurrency">ISO 4217 used when the CSV has no currency column or blank values.</param>
    /// <param name="logger">Optional; when set, skipped rows and parse summary are logged (useful in Development).</param>
    public static IReadOnlyList<CsvTransactionRow> Parse(string csvContent, string defaultCurrency = "USD",
        ILogger? logger = null)
    {
        var lines = csvContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        logger?.LogDebug("CSV: {LineCount} non-empty lines after split (including header).", lines.Length);

        if (lines.Length < 2)
        {
            logger?.LogWarning("CSV parse failed: need header + at least one data row; got {LineCount} line(s).", lines.Length);
            throw new InvalidOperationException("CSV must include a header row and at least one data row.");
        }

        var header = SplitLine(lines[0]);
        var dateIdx = FindColumn(header, "date", "transactiondate", "posted");
        var debitIdx = FindColumn(header, "debit");
        var creditIdx = FindColumn(header, "credit");
        var amountIdx = FindColumn(header, "amount", "value");
        var vendorIdx = FindColumn(header, "vendor", "merchant", "description", "payee", "name");
        var currencyIdx = FindOptionalColumn(header, "currency", "curr");

        var dualDebitCredit = debitIdx >= 0 && creditIdx >= 0;
        var signedAmountOnly = amountIdx >= 0 && !dualDebitCredit;
        var debitColumnOnly = debitIdx >= 0 && creditIdx < 0 && amountIdx < 0;
        var creditColumnOnly = creditIdx >= 0 && debitIdx < 0 && amountIdx < 0;

        logger?.LogInformation(
            "CSV header mapped: Date={DateIdx}, Debit={DebitIdx}, Credit={CreditIdx}, Amount={AmountIdx}, Vendor={VendorIdx}, Currency={CurrencyIdx}, mode={Mode}. Header cells: {Header}",
            dateIdx, debitIdx, creditIdx, amountIdx, vendorIdx, currencyIdx,
            dualDebitCredit ? "dualDebitCredit" : signedAmountOnly ? "signedAmount" : debitColumnOnly ? "debitColumn" : creditColumnOnly ? "creditColumn" : "amountFallback",
            string.Join(" | ", header));

        if (lines[0].Contains(';', StringComparison.Ordinal) && !lines[0].Contains(','))
            logger?.LogWarning(
                "CSV first line uses semicolons, not commas. This parser expects comma-separated columns. Try exporting as comma-separated or reformat the file.");

        var hasAmountColumns = dualDebitCredit || signedAmountOnly || debitColumnOnly || creditColumnOnly
            || debitIdx >= 0 || creditIdx >= 0 || amountIdx >= 0;

        if (dateIdx < 0 || vendorIdx < 0 || !hasAmountColumns)
        {
            logger?.LogWarning(
                "CSV parse failed: could not find required columns (need Date, an amount column (Amount, or Debit/Credit, or Debit or Credit alone), and Vendor/Description). Normalized header tokens: {Tokens}",
                string.Join(", ", header.Select(Normalize)));
            throw new InvalidOperationException(
                "CSV must include columns for Date, amount (Amount, or Debit and Credit, or Debit or Credit), and Vendor (or Description/Merchant).");
        }

        var rows = new List<CsvTransactionRow>();
        var skipped = new List<string>();
        const int maxSkipNotes = 12;

        for (var i = 1; i < lines.Length; i++)
        {
            var cols = SplitLine(lines[i]);
            if (cols.Length == 0) continue;

            var dateStr = Get(cols, dateIdx);
            var vendor = Get(cols, vendorIdx);
            var currency = currencyIdx >= 0 ? Get(cols, currencyIdx) : defaultCurrency;
            if (string.IsNullOrWhiteSpace(vendor))
            {
                if (skipped.Count < maxSkipNotes)
                    skipped.Add($"line {i + 1}: empty vendor/description");
                continue;
            }

            if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                if (!DateOnly.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                {
                    if (skipped.Count < maxSkipNotes)
                        skipped.Add($"line {i + 1}: could not parse date '{dateStr}'");
                    continue;
                }

            decimal rawAmount;
            var isCredit = false;

            if (dualDebitCredit)
            {
                var debitStr = Get(cols, debitIdx);
                var creditStr = Get(cols, creditIdx);
                var hasDebit = TryParseMoney(debitStr, out var d) && d != 0;
                var hasCredit = TryParseMoney(creditStr, out var c) && c != 0;
                if (hasDebit && hasCredit)
                {
                    if (skipped.Count < maxSkipNotes)
                        skipped.Add($"line {i + 1}: both Debit and Credit non-zero; using Debit");
                    rawAmount = Math.Abs(d);
                    isCredit = false;
                }
                else if (hasDebit)
                {
                    rawAmount = Math.Abs(d);
                    isCredit = false;
                }
                else if (hasCredit)
                {
                    rawAmount = Math.Abs(c);
                    isCredit = true;
                }
                else
                    continue;
            }
            else if (signedAmountOnly)
            {
                var amountStr = Get(cols, amountIdx);
                if (!TryParseMoney(amountStr, out rawAmount))
                {
                    if (skipped.Count < maxSkipNotes)
                        skipped.Add($"line {i + 1}: could not parse amount '{amountStr}'");
                    continue;
                }

                // UK-style signed Amount column: positive = money in (credit), negative = spend (debit).
                isCredit = rawAmount > 0;
                rawAmount = Math.Abs(rawAmount);
            }
            else if (debitColumnOnly)
            {
                var debitStr = Get(cols, debitIdx);
                if (!TryParseMoney(debitStr, out rawAmount) || rawAmount == 0) continue;
                isCredit = false;
                rawAmount = Math.Abs(rawAmount);
            }
            else if (creditColumnOnly)
            {
                var creditStr = Get(cols, creditIdx);
                if (!TryParseMoney(creditStr, out rawAmount) || rawAmount == 0) continue;
                isCredit = true;
                rawAmount = Math.Abs(rawAmount);
            }
            else
            {
                // Fallback: first matching column among amount, debit, credit
                var amountStr = amountIdx >= 0 ? Get(cols, amountIdx) : "";
                if (amountIdx < 0 && debitIdx >= 0) amountStr = Get(cols, debitIdx);
                if (amountIdx < 0 && debitIdx < 0 && creditIdx >= 0) amountStr = Get(cols, creditIdx);
                if (!TryParseMoney(amountStr, out rawAmount))
                {
                    if (skipped.Count < maxSkipNotes)
                        skipped.Add($"line {i + 1}: could not parse amount '{amountStr}'");
                    continue;
                }

                if (debitIdx >= 0 && creditIdx < 0 && amountIdx < 0)
                {
                    isCredit = false;
                    rawAmount = Math.Abs(rawAmount);
                }
                else if (creditIdx >= 0 && debitIdx < 0 && amountIdx < 0)
                {
                    isCredit = true;
                    rawAmount = Math.Abs(rawAmount);
                }
                else
                {
                    isCredit = rawAmount > 0;
                    rawAmount = Math.Abs(rawAmount);
                }
            }

            var amount = rawAmount;
            if (string.IsNullOrWhiteSpace(currency)) currency = defaultCurrency;

            var iso = currency.Trim().ToUpperInvariant();
            if (iso.Length != 3) iso = defaultCurrency.Trim().ToUpperInvariant();
            rows.Add(new CsvTransactionRow(date, amount, isCredit, vendor.Trim(), iso));
        }

        if (skipped.Count > 0)
            logger?.LogWarning(
                "CSV skipped {SkippedCount} row(s). Examples: {Examples}",
                skipped.Count, string.Join("; ", skipped));

        if (rows.Count == 0)
        {
            logger?.LogWarning("CSV parse: zero valid rows from {DataLineCount} data lines.", lines.Length - 1);
            throw new InvalidOperationException("No valid transaction rows were found in the CSV.");
        }

        var debits = rows.Count(r => !r.IsCredit);
        var credits = rows.Count(r => r.IsCredit);
        logger?.LogInformation(
            "CSV parse OK: {ValidRows} rows ({Debits} debits, {Credits} credits) from {DataLines} data lines.",
            rows.Count, debits, credits, lines.Length - 1);

        return rows;
    }

    private static string[] SplitLine(string line) =>
        line.Split(',', StringSplitOptions.TrimEntries);

    private static bool TryParseMoney(string? s, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
    }

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
