import { useState } from "react";
import { Link } from "react-router-dom";
import { useGetMeQuery, useResetAccountDataMutation } from "@/store/api";

export function SettingsPage() {
  const me = useGetMeQuery();
  const [resetData, resetResult] = useResetAccountDataMutation();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const runReset = () => {
    setConfirmOpen(false);
    void resetData().unwrap();
  };

  return (
    <div className="page">
      <h2>Settings</h2>
      <p className="muted">Account preferences and import shortcuts.</p>

      {resetResult.isSuccess && (
        <div className="banner success">All account data has been removed. You can import transactions again.</div>
      )}
      {resetResult.isError && (
        <div className="banner error">Could not reset data. Check the API and try again.</div>
      )}

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
        <div className="card-label">Transactions</div>
        <p className="small muted">
          Import CSV, Excel, or PDF bank exports (or paste CSV) on the dedicated import page. Required columns:{" "}
          <strong>Date</strong>, <strong>Amount</strong>, and <strong>Vendor</strong> (or Description / Merchant).
        </p>
        <Link to="/import" className="btn primary">
          Open import
        </Link>
      </div>

      <div className="card flat settings-danger">
        <div className="card-label">Danger zone</div>
        <p className="small muted">
          Permanently delete all imported transactions, subscriptions, recurring review rows, alerts, audit log entries,
          and bank connection records for <strong>this account</strong>. Your account, sign-in, and users are kept.
        </p>
        <button
          type="button"
          className="btn danger"
          disabled={resetResult.isLoading}
          onClick={() => setConfirmOpen(true)}
        >
          {resetResult.isLoading ? "Deleting…" : "Delete all data"}
        </button>
      </div>

      {confirmOpen && (
        <>
          <div className="drawer-backdrop" role="presentation" onClick={() => setConfirmOpen(false)} />
          <div className="drawer settings-reset-dialog" role="dialog" aria-modal="true" aria-labelledby="reset-title">
            <div className="drawer-header">
              <h3 id="reset-title" className="drawer-title">
                Delete all data?
              </h3>
              <button type="button" className="btn ghost small drawer-close" onClick={() => setConfirmOpen(false)}>
                Close
              </button>
            </div>
            <div className="drawer-body">
              <p className="drawer-alert-preview">This cannot be undone.</p>
              <p className="muted small drawer-alert-msg">
                All transactions, imports, subscriptions, recurring candidates, alerts, audit logs, and bank
                connections for this account will be removed.
              </p>
            </div>
            <div className="drawer-footer">
              <div className="drawer-actions">
                <button type="button" className="btn danger small" onClick={runReset}>
                  Yes, delete everything
                </button>
              </div>
              <button type="button" className="btn ghost small drawer-cancel" onClick={() => setConfirmOpen(false)}>
                Cancel
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
