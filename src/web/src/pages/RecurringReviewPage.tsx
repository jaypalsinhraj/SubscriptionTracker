import { useState } from "react";
import { Link } from "react-router-dom";
import {
  useClassifyRecurringCandidateMutation,
  useGetMeQuery,
  useGetRecurringReviewQuery,
} from "@/store/api";
import { formatCurrency } from "@/utils/formatMoney";

const cadenceLabels = ["Unknown", "Weekly", "Monthly", "Yearly", "Quarterly"];

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

export function RecurringReviewPage() {
  const me = useGetMeQuery();
  const [includeNonSubscription, setIncludeNonSubscription] = useState(false);
  const { data, isLoading, isError } = useGetRecurringReviewQuery({ includeNonSubscription });
  const [classify, classifyResult] = useClassifyRecurringCandidateMutation();
  const locale = me.data?.uiCulture ?? "en-US";

  return (
    <div className="page">
      <h2>Recurring review</h2>
      <p className="muted">
        Borderline recurring patterns (needs review) and optional non-subscription recurring charges (utilities,
        rent, transfers). Confirm a true subscription to move it to the{" "}
        <Link to="/subscriptions">Subscriptions</Link> list.
      </p>

      <label className="inline-check muted">
        <input
          type="checkbox"
          checked={includeNonSubscription}
          onChange={(e) => setIncludeNonSubscription(e.target.checked)}
        />{" "}
        Include non-subscription recurring (utilities, rent, income, etc.)
      </label>

      {classifyResult.isError && <div className="banner error">Action failed — check the message from the API.</div>}

      {isLoading && <p>Loading…</p>}
      {isError && <div className="banner error">Failed to load recurring items.</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Label</th>
              <th>Vendor</th>
              <th>Type</th>
              <th>Sub. confidence</th>
              <th>Pattern</th>
              <th>Reason</th>
              <th>Avg amount</th>
              <th>Cadence</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((c) => (
              <tr key={c.id}>
                <td>
                  <span className="pill subtle">{c.uiLabel}</span>
                </td>
                <td>
                  <div>{c.vendorName}</div>
                  <div className="small muted">{c.normalizedMerchant}</div>
                </td>
                <td>{recurringTypeLabels[c.recurringType] ?? c.recurringType}</td>
                <td>{c.subscriptionConfidenceScore}</td>
                <td>{c.patternConfidenceScore}</td>
                <td className="small" title={c.classificationReason}>
                  {c.classificationReason || "—"}
                </td>
                <td>{formatCurrency(c.averageAmount, c.currency, locale)}</td>
                <td>{cadenceLabels[c.cadence] ?? c.cadence}</td>
                <td>
                  <div className="row-actions">
                    {c.status === 0 && (
                      <>
                        <button
                          type="button"
                          className="btn primary small"
                          onClick={() =>
                            void classify({ id: c.id, action: "confirmSubscription" })
                          }
                        >
                          Likely subscription
                        </button>
                        <button
                          type="button"
                          className="btn ghost small"
                          onClick={() => void classify({ id: c.id, action: "dismiss" })}
                        >
                          Dismiss
                        </button>
                      </>
                    )}
                    {c.status !== 0 && (
                      <span className="small muted">Informational — use imports + detection to refresh.</span>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {data?.length === 0 && (
          <p className="muted">
            Nothing in the review queue. <Link to="/import">Import transactions</Link> — detection runs automatically
            after import.
          </p>
        )}
      </div>
    </div>
  );
}
