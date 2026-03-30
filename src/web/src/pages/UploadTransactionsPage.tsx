import { useCallback, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useImportTransactionsMutation } from "@/store/api";
import { importFilesSequentially } from "@/utils/importFiles";

const accept = ".csv,.txt,.xlsx,.xls,.pdf,application/pdf,text/csv,text/plain";

function fileKey(f: File) {
  return `${f.name}:${f.size}:${f.lastModified}`;
}

export function UploadTransactionsPage() {
  const [importTx, result] = useImportTransactionsMutation();
  const [dragOver, setDragOver] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [pastedCsv, setPastedCsv] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const addFiles = useCallback((incoming: File[]) => {
    if (incoming.length === 0) return;
    setLocalError(null);
    setStatusMessage(null);
    setSelectedFiles((prev) => {
      const map = new Map<string, File>();
      for (const f of prev) map.set(fileKey(f), f);
      for (const f of incoming) map.set(fileKey(f), f);
      return Array.from(map.values());
    });
    setPastedCsv("");
  }, []);

  const removeFile = useCallback((key: string) => {
    setSelectedFiles((prev) => prev.filter((f) => fileKey(f) !== key));
  }, []);

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragOver(false);
      const list = Array.from(e.dataTransfer.files ?? []);
      addFiles(list);
    },
    [addFiles]
  );

  const runImport = async () => {
    setLocalError(null);
    setStatusMessage(null);
    const csv = pastedCsv.trim();
    if (selectedFiles.length === 0 && !csv) {
      setLocalError("Add one or more files or paste CSV data.");
      return;
    }
    if (selectedFiles.length > 0 && csv) {
      setLocalError("Use either files or pasted CSV, not both. Clear one of them.");
      return;
    }

    setIsBusy(true);
    try {
      if (selectedFiles.length > 0) {
        const { imported, failed } = await importFilesSequentially(selectedFiles, (p) => importTx(p).unwrap());

        const successKeys = new Set(imported.map(fileKey));
        setSelectedFiles((prev) => prev.filter((f) => !successKeys.has(fileKey(f))));

        if (failed.length === 0) {
          setStatusMessage(
            imported.length === 1
              ? "Import completed. Detection and alerts have been refreshed."
              : `${imported.length} imports completed. Detection and alerts have been refreshed.`
          );
        } else if (imported.length > 0) {
          setLocalError(
            `Imported ${imported.length} file(s). Failed (${failed.length}):\n${failed.map((f) => `${f.fileName}: ${f.message}`).join("\n")}`
          );
          setStatusMessage(`${imported.length} file(s) imported. Fix or remove failed files and try again.`);
        } else {
          setLocalError(failed.map((f) => `${f.fileName}: ${f.message}`).join("\n"));
        }
      } else {
        await importTx({ fileName: "pasted-import.csv", csvContent: csv }).unwrap();
        setPastedCsv("");
        setStatusMessage("Import completed. Detection and alerts have been refreshed.");
      }
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "Import failed.";
      setLocalError(msg);
    } finally {
      setIsBusy(false);
    }
  };

  const hasFiles = selectedFiles.length > 0;

  return (
    <div className="page upload-page">
      <h2>Import transactions</h2>
      <p className="muted">
        Upload bank exports or paste CSV. Multiple files upload with up to{" "}
        <strong>5 concurrent</strong> imports. We support <strong>CSV</strong>, <strong>Excel</strong> (.xlsx / .xls),
        and <strong>PDF</strong> (best-effort text extraction — if parsing fails, export CSV from your bank).{" "}
        <Link to="/settings">Account currency &amp; locale</Link> apply to rows without a currency column.
      </p>

      {(result.isError || localError) && (
        <div className="banner error">
          {localError ?? "Import failed. Check the file format and API logs."}
        </div>
      )}

      {statusMessage && <div className="banner success">{statusMessage}</div>}

      <div
        className={`upload-dropzone ${dragOver ? "drag-over" : ""} ${isBusy ? "upload-dropzone-disabled" : ""}`}
        onDragOver={(e) => {
          if (isBusy) return;
          e.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={isBusy ? undefined : onDrop}
        onClick={() => {
          if (!isBusy) inputRef.current?.click();
        }}
        role="button"
        tabIndex={isBusy ? -1 : 0}
        aria-busy={isBusy}
        onKeyDown={(e) => {
          if (isBusy) return;
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            inputRef.current?.click();
          }
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          multiple
          className="upload-input-hidden"
          disabled={isBusy}
          onChange={(e) => {
            addFiles(Array.from(e.target.files ?? []));
            e.target.value = "";
          }}
        />
        <span className="upload-dropzone-ico" aria-hidden>
          📄
        </span>
        <div className="upload-dropzone-title">Drop files here</div>
        <p className="muted small upload-dropzone-hint">
          PDF bank statement, CSV, or Excel — or click to choose files (multiple allowed)
        </p>
        <p className="upload-formats small muted">.csv · .xlsx · .xls · .pdf</p>
      </div>

      {hasFiles && (
        <div className="upload-selected card flat">
          <div className="card-label">Selected files ({selectedFiles.length})</div>
          <ul className="upload-file-list">
            {selectedFiles.map((f) => (
              <li key={fileKey(f)} className="upload-selected-row">
                <span className="upload-file-name">{f.name}</span>
                <span className="muted small">{(f.size / 1024).toFixed(1)} KB</span>
                <button
                  type="button"
                  className="btn ghost small"
                  disabled={isBusy}
                  onClick={(e) => {
                    e.stopPropagation();
                    removeFile(fileKey(f));
                  }}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
          <button
            type="button"
            className="btn ghost small"
            disabled={isBusy}
            onClick={() => setSelectedFiles([])}
          >
            Clear all files
          </button>
        </div>
      )}

      <div className="upload-divider">
        <span>or paste CSV</span>
      </div>

      <label className="field upload-paste">
        <span>Paste CSV (header row with Date, Amount, Vendor)</span>
        <textarea
          rows={12}
          value={pastedCsv}
          disabled={isBusy}
          onChange={(e) => {
            setPastedCsv(e.target.value);
            setLocalError(null);
            setStatusMessage(null);
            if (e.target.value.trim()) setSelectedFiles([]);
          }}
          placeholder={`Date,Amount,Vendor\n2024-01-05,-12.99,SPOTIFY.COM\n2024-02-05,-12.99,SPOTIFY.COM`}
          spellCheck={false}
        />
      </label>

      <div className="upload-actions">
        <button
          type="button"
          className="btn primary"
          disabled={isBusy}
          aria-busy={isBusy}
          onClick={() => void runImport()}
        >
          {isBusy ? "Please wait..." : "Import & analyze"}
        </button>
        <p className="muted small upload-actions-note">
          {hasFiles
            ? "Files are sent in order; each completes import and detection before the next starts. Successful files are removed from the list."
            : "With no files selected, the pasted text is sent as a single import."}
        </p>
      </div>
    </div>
  );
}
