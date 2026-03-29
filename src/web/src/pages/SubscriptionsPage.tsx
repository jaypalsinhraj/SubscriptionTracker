import { useState } from "react";
import {
  useGetMeQuery,
  useGetSubscriptionsQuery,
  usePatchSubscriptionOwnerMutation,
  useRequestSubscriptionReviewMutation,
} from "@/store/api";
import { formatCurrency } from "@/utils/formatMoney";

const cadenceLabels = ["Unknown", "Weekly", "Monthly", "Yearly"];

/** Matches backend RecurringType enum order */
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
  const [includeReview, setIncludeReview] = useState(false);
  const { data, isLoading, isError } = useGetSubscriptionsQuery({ includeReview });
  const [patchOwner, patchResult] = usePatchSubscriptionOwnerMutation();
  const [requestReview, reviewResult] = useRequestSubscriptionReviewMutation();
  const locale = me.data?.uiCulture ?? "en-US";

  const [ownerDraft, setOwnerDraft] = useState<{ id: string; name: string; email: string } | null>(null);

  return (
    <div className="page">
      <h2>Subscriptions</h2>
      <p className="muted">
        Likely software and media subscriptions (classification score ≥70). Enable “Include review bucket” to
        show borderline candidates (40–69). Recurring non-subscription items stay out of this list.
      </p>

      <label className="inline-check muted">
        <input
          type="checkbox"
          checked={includeReview}
          onChange={(e) => setIncludeReview(e.target.checked)}
        />{" "}
        Include review bucket (40–69 score)
      </label>

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
              <th>Sub. score</th>
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
            {data?.map((s) => {
              const editing = ownerDraft?.id === s.id;
              return (
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
                  <td>{s.classificationScore}</td>
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
                    {editing ? (
                      <div className="inline-fields">
                        <input
                          placeholder="Name"
                          value={ownerDraft.name}
                          onChange={(e) => setOwnerDraft({ ...ownerDraft, name: e.target.value })}
                        />
                        <input
                          placeholder="Email"
                          type="email"
                          value={ownerDraft.email}
                          onChange={(e) => setOwnerDraft({ ...ownerDraft, email: e.target.value })}
                        />
                      </div>
                    ) : (
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
                    )}
                  </td>
                  <td>
                    <div className="row-actions">
                      {editing ? (
                        <>
                          <button
                            type="button"
                            className="btn small"
                            onClick={() => {
                              void patchOwner({
                                id: s.id,
                                ownerName: ownerDraft.name || null,
                                ownerEmail: ownerDraft.email || null,
                                ownerUserId: null,
                              }).then(() => setOwnerDraft(null));
                            }}
                          >
                            Save owner
                          </button>
                          <button type="button" className="btn ghost small" onClick={() => setOwnerDraft(null)}>
                            Cancel
                          </button>
                        </>
                      ) : (
                        <>
                          <button
                            type="button"
                            className="btn ghost small"
                            onClick={() =>
                              setOwnerDraft({
                                id: s.id,
                                name: s.ownerName ?? "",
                                email: s.ownerEmail ?? "",
                              })
                            }
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
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {data?.length === 0 && <p className="muted">No subscriptions yet. Import a CSV from Settings.</p>}
      </div>
    </div>
  );
}
