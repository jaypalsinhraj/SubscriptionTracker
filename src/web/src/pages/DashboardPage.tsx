import { useGetDashboardSummaryQuery, useGetMeQuery } from "@/store/api";
import { formatCurrency } from "@/utils/formatMoney";

export function DashboardPage() {
  const me = useGetMeQuery();
  const summary = useGetDashboardSummaryQuery();
  const locale = me.data?.uiCulture ?? "en-US";
  const acctCurrency = me.data?.defaultCurrency ?? "USD";
  const data = summary.data;

  const monthlySpend = data?.monthlySaaSSpendEstimate ?? 0;
  const duplicateSpend = data?.potentialDuplicateSpend ?? 0;
  const nonDuplicateSpend = Math.max(0, monthlySpend - duplicateSpend);
  const duplicatePct = monthlySpend > 0 ? Math.min(100, Math.max(0, (duplicateSpend / monthlySpend) * 100)) : 0;
  const nonDuplicatePct = Math.max(0, 100 - duplicatePct);
  const activeSubs = data?.activeSubscriptionCount ?? 0;
  const openAlerts = data?.openAlertCount ?? 0;
  const pending = data?.pendingConfirmationCount ?? 0;
  const alertLoadPct = activeSubs > 0 ? Math.min(100, (openAlerts / activeSubs) * 100) : 0;
  const pendingPct = openAlerts > 0 ? Math.min(100, (pending / openAlerts) * 100) : 0;

  return (
    <div className="page">
      <h2>Dashboard</h2>
      <p className="muted">Monthly SaaS spend, subscription coverage, and risk signals.</p>

      {me.isError && (
        <div className="banner error">
          Could not load profile. Ensure the API is running and CORS/proxy is configured.
        </div>
      )}

      <div className="grid cards">
        <div className="card">
          <div className="card-label">Estimated monthly SaaS</div>
          <div className="card-value">
            {summary.data
              ? formatCurrency(summary.data.monthlySaaSSpendEstimate, acctCurrency, locale)
              : "—"}
          </div>
          {data && (
            <>
              <div className="dashboard-stacked">
                <div className="dashboard-stacked-fill dashboard-stacked-fill-primary" style={{ width: `${nonDuplicatePct}%` }} />
                <div className="dashboard-stacked-fill dashboard-stacked-fill-warning" style={{ width: `${duplicatePct}%` }} />
              </div>
              <p className="muted small" style={{ marginTop: "0.45rem", marginBottom: 0 }}>
                Duplicate exposure: {formatCurrency(duplicateSpend, acctCurrency, locale)} ({duplicatePct.toFixed(1)}%)
              </p>
            </>
          )}
        </div>
        <div className="card">
          <div className="card-label">Active subscriptions</div>
          <div className="card-value">{summary.data?.activeSubscriptionCount ?? "—"}</div>
          {data && (
            <>
              <div className="dashboard-track">
                <div className="dashboard-fill" style={{ width: `${Math.max(0, 100 - alertLoadPct)}%` }} />
              </div>
              <p className="muted small" style={{ marginTop: "0.45rem", marginBottom: 0 }}>
                Coverage score: {(Math.max(0, 100 - alertLoadPct)).toFixed(0)}%
              </p>
            </>
          )}
        </div>
        <div className="card">
          <div className="card-label">Open alerts</div>
          <div className="card-value">{summary.data?.openAlertCount ?? "—"}</div>
          {data && (
            <>
              <div className="dashboard-track">
                <div className="dashboard-fill dashboard-fill-warning" style={{ width: `${alertLoadPct}%` }} />
              </div>
              <p className="muted small" style={{ marginTop: "0.45rem", marginBottom: 0 }}>
                Alert load vs subscriptions: {alertLoadPct.toFixed(0)}%
              </p>
            </>
          )}
        </div>
        <div className="card">
          <div className="card-label">Pending confirmations</div>
          <div className="card-value">{summary.data?.pendingConfirmationCount ?? "—"}</div>
          {data && (
            <>
              <div className="dashboard-track">
                <div className="dashboard-fill dashboard-fill-warning" style={{ width: `${pendingPct}%` }} />
              </div>
              <p className="muted small" style={{ marginTop: "0.45rem", marginBottom: 0 }}>
                Pending share of open alerts: {pendingPct.toFixed(0)}%
              </p>
            </>
          )}
        </div>
        <div className="card">
          <div className="card-label">Duplicate-tool exposure (est.)</div>
          <div className="card-value">
            {summary.data
              ? formatCurrency(summary.data.potentialDuplicateSpend, acctCurrency, locale)
              : "—"}
          </div>
          <p className="muted small" style={{ marginTop: "0.5rem", marginBottom: 0 }}>
            Only when two or more active subscriptions share the same normalized vendor (e.g. overlapping
            Slack or Zoom). One charge per vendor stays at zero.
          </p>
        </div>
      </div>

      <div className="card flat">
        <div className="card-label">Signed-in context</div>
        <pre className="code-block">
          {me.isLoading && "Loading…"}
          {me.data && JSON.stringify(me.data, null, 2)}
        </pre>
      </div>
    </div>
  );
}
