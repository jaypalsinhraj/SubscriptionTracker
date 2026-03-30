import { useState } from "react";
import { Link } from "react-router-dom";
import {
  useGetMeQuery,
  useGetSubscriptionsQuery,
  usePatchSubscriptionOwnerMutation,
  useRequestSubscriptionReviewMutation,
} from "@/store/api";
import { formatCurrency } from "@/utils/formatMoney";

const cadenceLabels = ["Unknown", "Weekly", "Monthly", "Yearly", "Quarterly"];

/** Matches backend RecurringType enum */
const recurringTypeLabels: Record<number, string> = {
  0: "Unknown recurring",
  1: "Software subscription",
  2: "Media subscription",
  3: "Utility bill",
  4: "Salary",
  5: "Transfer",
  6: "Insurance",
  7: "Loan payment",
  8: "Rent",
  9: "Other recurring expense",
  10: "Telecom",
  11: "Recurring income",
};

const reviewLabels = [
  "No review state",
  "Needs review",
  "Under review",
  "Confirmed needed",
  "Marked for cancellation",
  "Cancellation planned",
];

export function SubscriptionsPage() {
  const me = useGetMeQuery();
  const [likelySaaSMediaOnly, setLikelySaaSMediaOnly] = useState(false);
  const { data, isLoading, isError } = useGetSubscriptionsQuery(likelySaaSMediaOnly);
  const [patchOwner, patchResult] = usePatchSubscriptionOwnerMutation();
  const [requestReview, reviewResult] = useRequestSubscriptionReviewMutation();
  const locale = me.data?.uiCulture ?? "en-US";

  const [ownerPanelId, setOwnerPanelId] = useState<string | null>(null);
  const [ownerNameDraft, setOwnerNameDraft] = useState("");
  const [ownerEmailDraft, setOwnerEmailDraft] = useState("");

  const panelSub = ownerPanelId ? data?.find((s) => s.id === ownerPanelId) : undefined;

  const openOwnerPanel = (s: {
    id: string;
    vendorName: string;
    ownerName: string | null;
    ownerEmail: string | null;
  }) => {
    setOwnerPanelId(s.id);
    setOwnerNameDraft(s.ownerName ?? "");
    setOwnerEmailDraft(s.ownerEmail ?? "");
  };

  const closeOwnerPanel = () => {
    setOwnerPanelId(null);
    setOwnerNameDraft("");
    setOwnerEmailDraft("");
  };

  const saveOwner = () => {
    if (!ownerPanelId) return;
    void patchOwner({
      id: ownerPanelId,
      ownerName: ownerNameDraft.trim() || null,
      ownerEmail: ownerEmailDraft.trim() || null,
      ownerUserId: null,
    })
      .unwrap()
      .then(() => closeOwnerPanel());
  };

  return (
    <div className="page">
      <h2>Subscriptions</h2>
      <p className="muted">
        Active recurring subscriptions detected from your data. By default this list shows{" "}
        <strong>all</strong> of them (including unknown recurring and other types). Use the filter below to
        narrow to high-confidence software and media only. Items excluded from detection (salary, utilities,
        etc.) appear under <Link to="/recurring/review">Recurring review</Link>.
      </p>

      <label className="inline-check muted">
        <input
          type="checkbox"
          checked={likelySaaSMediaOnly}
          onChange={(e) => setLikelySaaSMediaOnly(e.target.checked)}
        />{" "}
        Likely SaaS &amp; media only (confidence ≥70)
      </label>
      {data != null && (
        <p className="muted small" style={{ marginTop: "0.35rem" }}>
          Showing {data.length} {data.length === 1 ? "row" : "rows"}
        </p>
      )}

      {patchResult.isError && <div className="banner error">Could not update owner.</div>}
      {reviewResult.isError && <div className="banner error">Could not request review.</div>}

      {isLoading && <p>Loading…</p>}
      {isError && <div className="banner error">Failed to load subscriptions.</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Vendor</th>
              <th>Type</th>
              <th>Sub. confidence</th>
              <th>Pattern</th>
              <th>Reason</th>
              <th>Avg amount</th>
              <th>Cadence</th>
              <th>Review status</th>
              <th>Owner</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((s) => (
              <tr key={s.id}>
                <td>
                  <div>{s.vendorName}</div>
                  {s.normalizedMerchant && s.normalizedMerchant !== s.vendorName.toLowerCase() && (
                    <div className="small muted">{s.normalizedMerchant}</div>
                  )}
                </td>
                <td>
                  <span className="pill subtle">
                    {recurringTypeLabels[s.recurringType] ?? `Type ${s.recurringType}`}
                  </span>
                </td>
                <td>{s.subscriptionConfidenceScore}</td>
                <td>{s.patternConfidenceScore}</td>
                <td className="small" title={s.classificationReason}>
                  {s.classificationReason || "—"}
                </td>
                <td>{formatCurrency(s.averageAmount, s.currency, locale)}</td>
                <td>{cadenceLabels[s.cadence] ?? s.cadence}</td>
                <td>
                  <span className="pill subtle">{reviewLabels[s.reviewStatus] ?? s.reviewStatus}</span>
                </td>
                <td>
                  <span className="small">
                    {s.ownerName || s.ownerEmail ? (
                      <>
                        {s.ownerName}
                        {s.ownerName && s.ownerEmail ? " · " : ""}
                        {s.ownerEmail}
                      </>
                    ) : (
                      <span className="muted">Not set</span>
                    )}
                  </span>
                </td>
                <td>
                  <div className="row-actions">
                    <button
                      type="button"
                      className="btn ghost small"
                      onClick={() => openOwnerPanel(s)}
                    >
                      Edit owner
                    </button>
                    <button
                      type="button"
                      className="btn primary small"
                      onClick={() => void requestReview({ id: s.id })}
                    >
                      Request review
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {data?.length === 0 && (
          <p className="muted">
            No subscriptions yet. <Link to="/import">Import transactions</Link> (CSV, Excel, or PDF).
          </p>
        )}
      </div>

      {panelSub && (
        <>
          <div className="drawer-backdrop" role="presentation" onClick={closeOwnerPanel} />
          <div
            className="drawer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="owner-drawer-title"
          >
            <div className="drawer-header">
              <h3 id="owner-drawer-title" className="drawer-title">
                Edit owner
              </h3>
              <button type="button" className="btn ghost small drawer-close" onClick={closeOwnerPanel}>
                Close
              </button>
            </div>
            <div className="drawer-body">
              <p className="drawer-alert-preview">{panelSub.vendorName}</p>
              <p className="muted small drawer-alert-msg">
                {formatCurrency(panelSub.averageAmount, panelSub.currency, locale)} ·{" "}
                {cadenceLabels[panelSub.cadence] ?? panelSub.cadence}
              </p>
              <label className="field">
                <span>Owner name</span>
                <input
                  value={ownerNameDraft}
                  onChange={(e) => setOwnerNameDraft(e.target.value)}
                  placeholder="Name"
                  autoComplete="name"
                />
              </label>
              <label className="field">
                <span>Owner email</span>
                <input
                  type="email"
                  value={ownerEmailDraft}
                  onChange={(e) => setOwnerEmailDraft(e.target.value)}
                  placeholder="email@company.com"
                  autoComplete="email"
                />
              </label>
              <p className="muted small drawer-owner-hint">
                Used for routing review and confirmation requests. Directory user link can be added later.
              </p>
            </div>
            <div className="drawer-footer">
              <div className="drawer-actions">
                <button
                  type="button"
                  className="btn primary small"
                  disabled={patchResult.isLoading}
                  onClick={() => void saveOwner()}
                >
                  {patchResult.isLoading ? "Saving…" : "Save owner"}
                </button>
              </div>
              <button
                type="button"
                className="btn ghost small drawer-cancel"
                disabled={patchResult.isLoading}
                onClick={closeOwnerPanel}
              >
                Cancel
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
