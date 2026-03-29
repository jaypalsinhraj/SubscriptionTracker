import { useState } from "react";
import { useGetMeQuery, useImportTransactionsMutation } from "@/store/api";

export function SettingsPage() {
  const me = useGetMeQuery();
  const [fileName, setFileName] = useState("import.csv");
  const [csv, setCsv] = useState(
    "Date,Amount,Vendor\n2024-01-05,12.99,Adobe Creative Cloud\n2024-02-05,12.99,Adobe Creative Cloud\n2024-03-05,12.99,Adobe Creative Cloud"
  );
  const [importCsv, result] = useImportTransactionsMutation();

  return (
    <div className="page">
      <h2>Settings</h2>
      <p className="muted">Import CSV transaction extracts to power detection.</p>

      <div className="card flat">
        <div className="card-label">Region &amp; currency</div>
        <p className="small muted">
          Locale and default currency come from your account (seed uses <code>Globalization</code> in API config). CSV
          rows without a <strong>Currency</strong> column use the account default.
        </p>
        {me.data && (
          <p className="small">
            <strong>UI culture:</strong> {me.data.uiCulture} · <strong>Default currency:</strong>{" "}
            {me.data.defaultCurrency}
          </p>
        )}
      </div>

      <div className="card flat">
        <div className="card-label">CSV import</div>
        <p className="small muted">
          Required columns: <strong>Date</strong>, <strong>Amount</strong>, and <strong>Vendor</strong> (or Description /
          Merchant).
        </p>
        <label className="field">
          <span>File name</span>
          <input value={fileName} onChange={(e) => setFileName(e.target.value)} />
        </label>
        <label className="field">
          <span>CSV content</span>
          <textarea rows={8} value={csv} onChange={(e) => setCsv(e.target.value)} />
        </label>
        <button type="button" className="btn primary" onClick={() => importCsv({ fileName, csvContent: csv })}>
          Upload &amp; analyze
        </button>
        {result.isSuccess && (
          <p className="small success">Import {result.data.importId} completed. Detection ran on the server.</p>
        )}
        {result.isError && <p className="small error">Import failed. Check API logs.</p>}
      </div>
    </div>
  );
}
