import { useState } from "react";
import {
  useGetAlertsQuery,
  useRespondToAlertMutation,
} from "@/store/api";

const typeLabels = [
  "Renewal",
  "Duplicate",
  "Suspected unused",
  "Confirmation request",
  "Owner missing",
];
const statusLabels = ["Open", "Pending confirmation", "Resolved", "Dismissed"];

/** Alert types that support confirmation responses */
function canRespond(alertType: number, alertStatus: number) {
  if (alertStatus !== 1) return false;
  return alertType === 0 || alertType === 2 || alertType === 3 || alertType === 4;
}

export function AlertsPage() {
  const { data, isLoading, isError } = useGetAlertsQuery();
  const [respond, respondState] = useRespondToAlertMutation();
  const [notesById, setNotesById] = useState<Record<string, string>>({});
  const [respondingId, setRespondingId] = useState<string | null>(null);

  return (
    <div className="page">
      <h2>Alerts</h2>
      <p className="muted">
        Renewals, duplicate tools, and suspected unused subscriptions. Confirmations use careful wording — bank
        data alone does not prove a subscription is unused.
      </p>

      {respondState.isError && <div className="banner error">Could not submit response.</div>}

      {isLoading && <p>Loading…</p>}
      {isError && <div className="banner error">Failed to load alerts.</div>}

      <div className="stack">
        {data?.map((a) => {
          const showRespond = canRespond(a.alertType, a.alertStatus);
          const notes = notesById[a.id] ?? "";
          return (
            <div key={a.id} className="alert-row">
              <div>
                <div className="alert-title">{a.title}</div>
                <div className="muted small">{a.message}</div>
                <div className="alert-badges">
                  <span className="pill subtle">{typeLabels[a.alertType] ?? a.alertType}</span>
                  <span className="pill subtle">{statusLabels[a.alertStatus] ?? a.alertStatus}</span>
                </div>
              </div>
              <div className="alert-meta">
                <span className="muted small">{new Date(a.createdAt).toLocaleString()}</span>
                {showRespond && (
                  <div className="respond-panel">
                    {respondingId === a.id ? (
                      <>
                        <label className="field small">
                          <span>Notes (optional)</span>
                          <input
                            value={notes}
                            onChange={(e) => setNotesById((m) => ({ ...m, [a.id]: e.target.value }))}
                          />
                        </label>
                        <div className="row-actions">
                          <button
                            type="button"
                            className="btn primary small"
                            onClick={() => {
                              void respond({ id: a.id, response: 0, notes }).then(() => {
                                setRespondingId(null);
                                setNotesById((m) => ({ ...m, [a.id]: "" }));
                              });
                            }}
                          >
                            Still needed
                          </button>
                          <button
                            type="button"
                            className="btn ghost small"
                            onClick={() => {
                              void respond({ id: a.id, response: 1, notes }).then(() => {
                                setRespondingId(null);
                                setNotesById((m) => ({ ...m, [a.id]: "" }));
                              });
                            }}
                          >
                            Not needed
                          </button>
                          <button
                            type="button"
                            className="btn ghost small"
                            onClick={() => {
                              void respond({ id: a.id, response: 2, notes }).then(() => {
                                setRespondingId(null);
                                setNotesById((m) => ({ ...m, [a.id]: "" }));
                              });
                            }}
                          >
                            Not sure
                          </button>
                          <button type="button" className="btn ghost small" onClick={() => setRespondingId(null)}>
                            Cancel
                          </button>
                        </div>
                      </>
                    ) : (
                      <button type="button" className="btn primary small" onClick={() => setRespondingId(a.id)}>
                        Respond
                      </button>
                    )}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
      {data?.length === 0 && <p className="muted">No alerts yet.</p>}
    </div>
  );
}
