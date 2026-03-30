import pdfjsWorker from "pdfjs-dist/build/pdf.worker.min.mjs?url";

function escapeCsvField(s: string): string {
  if (s.includes(",") || s.includes('"') || s.includes("\n")) {
    return `"${s.replace(/"/g, '""')}"`;
  }
  return s;
}

/**
 * Best-effort: turn noisy PDF text into Date,Amount,Vendor CSV.
 * Fails (returns null) when fewer than two data rows are inferred.
 */
export function pdfTextToHeuristicCsv(raw: string): string | null {
  const normalized = raw.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  const lines = normalized
    .split("\n")
    .map((l) => l.trim())
    .filter((l) => l.length > 0);

  if (lines.length === 0) return null;

  const first = lines[0] ?? "";
  if (/date/i.test(first) && /amount|value|debit|credit/i.test(first) && first.includes(",")) {
    return lines.join("\n");
  }

  const rows: string[] = ["Date,Amount,Vendor"];
  const dateRe = /(\d{4}-\d{2}-\d{2}|\d{1,2}[/-]\d{1,2}[/-]\d{2,4})/;

  for (const line of lines) {
    const dateMatch = line.match(dateRe);
    if (!dateMatch) continue;
    const amountMatch = line.match(/(-?£?\s*[\d,]+\.\d{2})\s*$/);
    if (!amountMatch) continue;
    const date = dateMatch[1]!;
    let amount = amountMatch[1]!.replace(/£|\s|,/g, "");
    const start = line.indexOf(dateMatch[0]!);
    const end = line.lastIndexOf(amountMatch[0]!);
    let vendor = line.slice(start + dateMatch[0]!.length, end).trim().replace(/^[\s\-–|•·]+/u, "");
    if (!vendor) vendor = "Transaction";
    vendor = vendor.replace(/\s+/g, " ").slice(0, 400);
    rows.push(`${escapeCsvField(date)},${amount},${escapeCsvField(vendor)}`);
  }

  if (rows.length < 3) return null;
  return rows.join("\n");
}

async function pdfFileToCsv(file: File): Promise<string> {
  const pdfjs = await import("pdfjs-dist");
  pdfjs.GlobalWorkerOptions.workerSrc = pdfjsWorker;
  const data = new Uint8Array(await file.arrayBuffer());
  const pdf = await pdfjs.getDocument({ data }).promise;
  let full = "";
  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i);
    const tc = await page.getTextContent();
    const line = tc.items.map((it) => ("str" in it ? it.str : "")).join(" ");
    full += line + "\n";
  }
  const csv = pdfTextToHeuristicCsv(full);
  if (!csv) {
    throw new Error(
      "Could not infer Date / Amount / Description rows from this PDF. Export CSV or Excel from your bank, or paste CSV below."
    );
  }
  return csv;
}

export type ImportPayload = { fileName: string; csvContent: string };

export type ImportFilesBatchResult = {
  imported: File[];
  failed: { fileName: string; message: string }[];
};

/**
 * Processes multiple files one after another. The API runs import + recurring detection per account;
 * parallel requests were unsafe (overlapping detection). Sequential calls plus server-side per-account
 * locking keep imports reliable.
 */
export async function importFilesSequentially(
  files: File[],
  importOne: (payload: ImportPayload) => Promise<unknown>
): Promise<ImportFilesBatchResult> {
  const imported: File[] = [];
  const failed: { fileName: string; message: string }[] = [];

  for (const file of files) {
    try {
      const payload = await fileToImportPayload(file);
      await importOne(payload);
      imported.push(file);
    } catch (e) {
      const message = e instanceof Error ? e.message : String(e);
      failed.push({ fileName: file.name, message });
    }
  }

  return { imported, failed };
}

/**
 * Reads CSV / Excel / PDF and returns payload for the existing import API (CSV text).
 */
export async function fileToImportPayload(file: File): Promise<ImportPayload> {
  const name = file.name.toLowerCase();
  const ext = name.split(".").pop() ?? "";

  if (ext === "csv" || ext === "txt") {
    return { fileName: file.name, csvContent: await file.text() };
  }

  if (ext === "xlsx" || ext === "xls") {
    const XLSX = await import("xlsx");
    const buf = await file.arrayBuffer();
    const wb = XLSX.read(buf, { type: "array", cellDates: true, raw: false });
    const sheetName = wb.SheetNames[0];
    if (!sheetName) throw new Error("No sheets found in this workbook.");
    const ws = wb.Sheets[sheetName];
    if (!ws) throw new Error("Could not read the first worksheet.");
    const csvContent = XLSX.utils.sheet_to_csv(ws);
    if (!csvContent.trim()) throw new Error("The first sheet appears empty.");
    const base = file.name.replace(/\.(xlsx|xls)$/i, "");
    return { fileName: `${base}.csv`, csvContent };
  }

  if (ext === "pdf") {
    const csvContent = await pdfFileToCsv(file);
    const base = file.name.replace(/\.pdf$/i, "");
    return { fileName: `${base}.csv`, csvContent };
  }

  throw new Error("Unsupported file type. Use .csv, .xlsx, .xls, or .pdf.");
}
