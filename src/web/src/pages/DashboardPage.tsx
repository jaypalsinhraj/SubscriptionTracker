import { useGetDashboardSummaryQuery, useGetMeQuery } from "@/store/api";
import { formatCurrency } from "@/utils/formatMoney";

export function DashboardPage() {
  const me = useGetMeQuery();
  const summary = useGetDashboardSummaryQuery();
  const locale = me.data?.uiCulture ?? "en-US";
  const acctCurrency = me.data?.defaultCurrency ?? "USD";

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
        </div>
        <div className="card">
          <div className="card-label">Active subscriptions</div>
          <div className="card-value">{summary.data?.activeSubscriptionCount ?? "—"}</div>
        </div>
        <div className="card">
          <div className="card-label">Open alerts</div>
          <div className="card-value">{summary.data?.openAlertCount ?? "—"}</div>
        </div>
        <div className="card">
          <div className="card-label">Pending confirmations</div>
          <div className="card-value">{summary.data?.pendingConfirmationCount ?? "—"}</div>
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
