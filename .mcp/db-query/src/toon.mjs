export function encodeToon(value) {
  if (!value || typeof value !== "object") return String(value ?? "");
  const lines = [];
  for (const [key, item] of Object.entries(value)) {
    if (Array.isArray(item)) {
      const fields = [...new Set(item.flatMap((row) => row && typeof row === "object" ? Object.keys(row) : []))];
      lines.push(`${key}[${item.length}]${fields.length ? `{${fields.join(",")}}` : ""}:`);
      for (const row of item) lines.push(fields.length ? fields.map((field) => cell(row[field])).join(",") : cell(row));
    } else if (item && typeof item === "object") lines.push(`${key}: ${JSON.stringify(item)}`);
    else lines.push(`${key}: ${cell(item)}`);
  }
  return lines.join("\n");
}
function cell(value) { const text = value === null || value === undefined ? "" : String(value); return /[",\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text; }
