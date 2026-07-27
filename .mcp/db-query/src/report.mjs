import fs from "node:fs/promises";
import path from "node:path";
import { ToolError } from "./common.mjs";

export function createReportWriter({ mcpRoot, projectRoot }) {
  const reportsDir = path.join(mcpRoot, "reports");
  return async ({ tool, title, reportName, meta = {}, sections = [] }) => {
    const filePath = path.resolve(reportsDir, normalizeReportName(reportName));
    if (!filePath.startsWith(reportsDir + path.sep)) throw new ToolError("ruta de reporte fuera de reports/");
    const lines = [`# ${title}`, "", `**Tool:** \`${tool}\``, `**Timestamp:** ${new Date().toISOString()}`];
    for (const [key, value] of Object.entries(meta)) lines.push(`**${key}:** ${inline(value)}`);
    for (const section of sections) {
      lines.push("", `## ${section.heading}`, "");
      if (section.code !== undefined) lines.push("```sql", String(section.code), "```");
      else if (Array.isArray(section.rows)) lines.push(...renderTable(section.rows));
      else if (section.text !== undefined) lines.push(String(section.text));
    }
    lines.push("");
    await fs.mkdir(reportsDir, { recursive: true });
    const temporaryPath = `${filePath}.tmp`;
    await fs.writeFile(temporaryPath, lines.join("\n"), "utf8");
    await fs.rename(temporaryPath, filePath);
    return path.relative(projectRoot, filePath).split(path.sep).join("/");
  };
}

function normalizeReportName(value) {
  const name = String(value ?? "").trim();
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$/.test(name)) throw new ToolError("reportName debe usar solo letras, números, punto, guion o guion bajo");
  return name.toLowerCase().endsWith(".md") ? name : `${name}.md`;
}

function renderTable(rows) {
  if (!rows.length) return ["_Sin filas._"];
  const columns = [...new Set(rows.flatMap((row) => Object.keys(row)))];
  return [`| ${columns.join(" | ")} |`, `| ${columns.map(() => "---").join(" | ")} |`, ...rows.map((row) => `| ${columns.map((column) => inline(row[column])).join(" | ")} |`)];
}

function inline(value) {
  if (value === null || value === undefined) return "NULL";
  return String(value).replaceAll("|", "\\|").replaceAll("\r", "").replaceAll("\n", "<br>");
}