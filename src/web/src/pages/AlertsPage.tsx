import { useEffect, useMemo, useRef, useState } from "react";
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
const severityLabels = ["Info", "Warning", "Critical"];

type ReadFilter = "all" | "unread" | "read";

function allStatusesTrue(): Record<number, boolean> {
  return { 0: true, 1: true, 2: true, 3: true };
}

function allTypesTrue(): Record<number, boolean> {
  return { 0: true, 1: true, 2: true, 3: true, 4: true };
}

function allSeveritiesTrue(): Record<number, boolean> {
  return { 0: true, 1: true, 2: true };
}

function allFalseStatus(): Record<number, boolean> {
  return { 0: false, 1: false, 2: false, 3: false };
}

function allFalseTypes(): Record<number, boolean> {
  return { 0: false, 1: false, 2: false, 3: false, 4: false };
}

function allFalseSeverities(): Record<number, boolean> {
  return { 0: false, 1: false, 2: false };
}

/** Alert types that support confirmation responses */
function canRespond(alertType: number, alertStatus: number) {
  if (alertStatus !== 1) return false;
  return alertType === 0 || alertType === 2 || alertType === 3 || alertType === 4;
}

export function AlertsPage() {
  const { data, isLoading, isError } = useGetAlertsQuery();
  const [respond, respondState] = useRespondToAlertMutation();
  const [notesById, setNotesById] = useState<Record<string, string>>({});
  const [panelAlertId, setPanelAlertId] = useState<string | null>(null);

  const [includeStatus, setIncludeStatus] = useState(allStatusesTrue);
  const [includeType, setIncludeType] = useState(allTypesTrue);
  const [includeSeverity, setIncludeSeverity] = useState(allSeveritiesTrue);
  const [readFilter, setReadFilter] = useState<ReadFilter>("all");
  const [filtersOpen, setFiltersOpen] = useState(false);

  const statusAllRef = useRef<HTMLInputElement>(null);
  const typeAllRef = useRef<HTMLInputElement>(null);
  const severityAllRef = useRef<HTMLInputElement>(null);

  const filteredAlerts = useMemo(() => {
    if (!data?.length) return [];
    return data.filter((a) => {
      if (!includeStatus[a.alertStatus]) return false;
      if (!includeType[a.alertType]) return false;
      if (!includeSeverity[a.severity]) return false;
      if (readFilter === "unread" && a.isRead) return false;
      if (readFilter === "read" && !a.isRead) return false;
      return true;
    });
  }, [data, includeStatus, includeType, includeSeverity, readFilter]);

  const statusAllSelected = [0, 1, 2, 3].every((i) => includeStatus[i]);
  const statusSomeSelected = [0, 1, 2, 3].some((i) => includeStatus[i]);
  const typeAllSelected = [0, 1, 2, 3, 4].every((i) => includeType[i]);
  const typeSomeSelected = [0, 1, 2, 3, 4].some((i) => includeType[i]);
  const severityAllSelected = [0, 1, 2].every((i) => includeSeverity[i]);
  const severitySomeSelected = [0, 1, 2].some((i) => includeSeverity[i]);

  useEffect(() => {
    const el = statusAllRef.current;
    if (el) el.indeterminate = statusSomeSelected && !statusAllSelected;
  }, [statusSomeSelected, statusAllSelected]);

  useEffect(() => {
    const el = typeAllRef.current;
    if (el) el.indeterminate = typeSomeSelected && !typeAllSelected;
  }, [typeSomeSelected, typeAllSelected]);

  useEffect(() => {
    const el = severityAllRef.current;
    if (el) el.indeterminate = severitySomeSelected && !severityAllSelected;
  }, [severitySomeSelected, severityAllSelected]);

  const toggleAllStatus = () => {
    setIncludeStatus(statusAllSelected ? allFalseStatus() : allStatusesTrue());
  };

  const toggleAllTypes = () => {
    setIncludeType(typeAllSelected ? allFalseTypes() : allTypesTrue());
  };

  const toggleAllSeverities = () => {
    setIncludeSeverity(severityAllSelected ? allFalseSeverities() : allSeveritiesTrue());
  };

  const resetFilters = () => {
    setIncludeStatus(allStatusesTrue());
    setIncludeType(allTypesTrue());
    setIncludeSeverity(allSeveritiesTrue());
    setReadFilter("all");
  };

  const filtersActive =
    readFilter !== "all" ||
    Object.values(includeStatus).some((v) => !v) ||
    Object.values(includeType).some((v) => !v) ||
    Object.values(includeSeverity).some((v) => !v);

  const panelAlert = panelAlertId ? data?.find((a) => a.id === panelAlertId) : undefined;
  const panelNotes = panelAlertId ? (notesById[panelAlertId] ?? "") : "";

  const closePanel = () => setPanelAlertId(null);

  const submitResponse = (response: number) => {
    if (!panelAlertId) return;
    const notes = notesById[panelAlertId] ?? "";
    void respond({ id: panelAlertId, response, notes }).then(() => {
      closePanel();
      setNotesById((m) => ({ ...m, [panelAlertId]: "" }));
    });
  };

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

      {data && data.length > 0 && (
        <div className="card flat alert-filters-card">
          <div className="alert-filters-toolbar">
            <button
              type="button"
              className="alert-filters-toggle"
              onClick={() => setFiltersOpen((o) => !o)}
              aria-expanded={filtersOpen}
              id="alert-filters-toggle"
            >
              <span className="alert-filters-chevron" aria-hidden>
                {filtersOpen ? "▼" : "▶"}
              </span>
              <span className="card-label">Filters</span>
            </button>
            <span className="muted small alert-filters-summary-inline">
              Showing {filteredAlerts.length} of {data.length} alert{data.length === 1 ? "" : "s"}
              {filtersActive ? " · Custom" : ""}
            </span>
            {filtersActive && (
              <button type="button" className="btn ghost small" onClick={resetFilters}>
                Reset filters
              </button>
            )}
          </div>

          {filtersOpen && (
            <div
              className="alert-filters-panel"
              role="region"
              aria-labelledby="alert-filters-toggle"
            >
              <div className="alert-filters-grid">
                <fieldset className="alert-filter-group">
                  <legend>Status</legend>
                  <label className="inline-check muted alert-filter-all">
                    <input
                      ref={statusAllRef}
                      type="checkbox"
                      checked={statusAllSelected}
                      onChange={toggleAllStatus}
                    />{" "}
                    All
                  </label>
                  <div className="alert-filter-checks">
                    {statusLabels.map((label, i) => (
                      <label key={label} className="inline-check muted">
                        <input
                          type="checkbox"
                          checked={includeStatus[i] ?? false}
                          onChange={() =>
                            setIncludeStatus((s) => ({ ...s, [i]: !(s[i] ?? false) }))
                          }
                        />{" "}
                        {label}
                      </label>
                    ))}
                  </div>
                </fieldset>

                <fieldset className="alert-filter-group">
                  <legend>Type</legend>
                  <label className="inline-check muted alert-filter-all">
                    <input
                      ref={typeAllRef}
                      type="checkbox"
                      checked={typeAllSelected}
                      onChange={toggleAllTypes}
                    />{" "}
                    All
                  </label>
                  <div className="alert-filter-checks">
                    {typeLabels.map((label, i) => (
                      <label key={label} className="inline-check muted">
                        <input
                          type="checkbox"
                          checked={includeType[i] ?? false}
                          onChange={() =>
                            setIncludeType((s) => ({ ...s, [i]: !(s[i] ?? false) }))
                          }
                        />{" "}
                        {label}
                      </label>
                    ))}
                  </div>
                </fieldset>

                <fieldset className="alert-filter-group">
                  <legend>Severity</legend>
                  <label className="inline-check muted alert-filter-all">
                    <input
                      ref={severityAllRef}
                      type="checkbox"
                      checked={severityAllSelected}
                      onChange={toggleAllSeverities}
                    />{" "}
                    All
                  </label>
                  <div className="alert-filter-checks">
                    {severityLabels.map((label, i) => (
                      <label key={label} className="inline-check muted">
                        <input
                          type="checkbox"
                          checked={includeSeverity[i] ?? false}
                          onChange={() =>
                            setIncludeSeverity((s) => ({ ...s, [i]: !(s[i] ?? false) }))
                          }
                        />{" "}
                        {label}
                      </label>
                    ))}
                  </div>
                </fieldset>

                <fieldset className="alert-filter-group">
                  <legend>Read state</legend>
                  <div className="alert-filter-checks">
                    <label className="inline-check muted">
                      <input
                        type="radio"
                        name="alert-read-filter"
                        checked={readFilter === "all"}
                        onChange={() => setReadFilter("all")}
                      />{" "}
                      All
                    </label>
                    <label className="inline-check muted">
                      <input
                        type="radio"
                        name="alert-read-filter"
                        checked={readFilter === "unread"}
                        onChange={() => setReadFilter("unread")}
                      />{" "}
                      Unread only
                    </label>
                    <label className="inline-check muted">
                      <input
                        type="radio"
                        name="alert-read-filter"
                        checked={readFilter === "read"}
                        onChange={() => setReadFilter("read")}
                      />{" "}
                      Read only
                    </label>
                  </div>
                </fieldset>
              </div>
            </div>
          )}
        </div>
      )}

      <div className="stack">
        {filteredAlerts.map((a) => {
          const showRespond = canRespond(a.alertType, a.alertStatus);
          return (
            <div key={a.id} className="alert-row">
              <div className="alert-row-main">
                <div className="alert-title">{a.title}</div>
                <div className="muted small">{a.message}</div>
                <div className="alert-badges">
                  <span className="pill subtle">{typeLabels[a.alertType] ?? a.alertType}</span>
                  <span className="pill subtle">{statusLabels[a.alertStatus] ?? a.alertStatus}</span>
                  <span className="pill subtle">{severityLabels[a.severity] ?? a.severity}</span>
                  {a.isRead ? (
                    <span className="pill subtle">Read</span>
                  ) : (
                    <span className="pill subtle">Unread</span>
                  )}
                </div>
              </div>
              <div className="alert-row-aside">
                <span className="muted small">{new Date(a.createdAt).toLocaleString()}</span>
                {showRespond && (
                  <button type="button" className="btn primary small alert-respond-btn" onClick={() => setPanelAlertId(a.id)}>
                    Respond
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
      {data?.length === 0 && <p className="muted">No alerts yet.</p>}
      {data && data.length > 0 && filteredAlerts.length === 0 && (
        <p className="muted">
          No alerts match these filters.{" "}
          <button type="button" className="btn ghost small" onClick={resetFilters}>
            Clear filters
          </button>
        </p>
      )}

      {panelAlert && (
        <>
          <div className="drawer-backdrop" role="presentation" onClick={closePanel} />
          <div className="drawer" role="dialog" aria-modal="true" aria-labelledby="alert-drawer-title">
            <div className="drawer-header">
              <h3 id="alert-drawer-title" className="drawer-title">
                Respond to alert
              </h3>
              <button type="button" className="btn ghost small drawer-close" onClick={closePanel}>
                Close
              </button>
            </div>
            <div className="drawer-body">
              <p className="drawer-alert-preview">{panelAlert.title}</p>
              <p className="muted small drawer-alert-msg">{panelAlert.message}</p>
              <label className="field drawer-notes">
                <span>Notes (optional)</span>
                <textarea
                  rows={5}
                  value={panelNotes}
                  onChange={(e) => setNotesById((m) => ({ ...m, [panelAlert.id]: e.target.value }))}
                  placeholder="Add context for your team…"
                />
              </label>
            </div>
            <div className="drawer-footer">
              <div className="drawer-actions">
                <button type="button" className="btn primary small" onClick={() => submitResponse(0)}>
                  Still needed
                </button>
                <button type="button" className="btn ghost small" onClick={() => submitResponse(1)}>
                  Not needed
                </button>
                <button type="button" className="btn ghost small" onClick={() => submitResponse(2)}>
                  Not sure
                </button>
              </div>
              <button type="button" className="btn ghost small drawer-cancel" onClick={closePanel}>
                Cancel
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
