import { ToolError } from "./common.mjs";

export function createDbExecDefinition(profile) {
  const properties = {
    blocks: { type: "array", description: "Bloques T-SQL ejecutados en orden.", minItems: 1, maxItems: 25, items: { type: "object", properties: { name: { type: "string", description: "Nombre único de la sección del reporte." }, sql: { type: "string", description: "Bloque T-SQL a ejecutar." } }, required: ["name", "sql"], additionalProperties: false } },
    reportName: { type: "string", description: `Nombre del reporte Markdown en ${profile.reportDirectory}.` },
    timeoutSeconds: { type: "integer", minimum: 1, maximum: profile.maxTimeoutSeconds },
  };
  if (profile.requireApply) properties.apply = { type: "boolean", description: "Requerido solo si uno o más bloques no son de solo lectura." };
  return { name: "db_exec", order: 40, description: profile.description, inputSchema: { type: "object", properties, required: ["blocks", "reportName"], additionalProperties: false } };
}

export async function executeDbExec(runtime, args) {
  const blocks = validateBlocks(args.blocks);
  const preview = blocks.map((block) => ({ ...block, ...runtime.database.inspectScript(block.sql) }));
  if (runtime.profile.requireApply && preview.some((block) => block.requiresApply) && args.apply !== true) {
    return { applied: false, requiresApply: true, blocks: preview.map(({ name, detectedOperations }) => ({ name, operations: detectedOperations })), note: "El lote contiene operaciones no solo lectura. Repite la llamada con apply:true para ejecutarlo en EvistaProduccion." };
  }
  const results = [];
  for (const block of blocks) results.push({ ...await runtime.database.execScript({ sql: block.sql, timeoutSeconds: args.timeoutSeconds, apply: args.apply }), ...block });
  const markdownUrl = await runtime.writeReport({ tool: "db_exec", title: runtime.profile.reportTitle, reportName: args.reportName, meta: { Aplicado: true, Bloques: results.length }, sections: results.flatMap(reportSections) });
  return { ready: true, applied: true, blocks: summarize(results), markdownUrl };
}

function validateBlocks(value) {
  if (!Array.isArray(value) || value.length < 1 || value.length > 25) throw new ToolError("blocks debe contener entre 1 y 25 bloques");
  const names = new Set();
  return value.map((block, index) => {
    if (!block || typeof block !== "object" || Array.isArray(block)) throw new ToolError(`blocks[${index}] debe ser un objeto`);
    const name = String(block.name ?? "").trim();
    const sql = String(block.sql ?? "").trim();
    if (!/^[A-Za-z0-9][A-Za-z0-9 ._-]{0,119}$/.test(name) || !sql) throw new ToolError(`blocks[${index}] requiere name y sql válidos`);
    if (names.has(name)) throw new ToolError(`blocks contiene un nombre duplicado: ${name}`);
    names.add(name);
    return { name, sql };
  });
}

function reportSections(block) {
  return [{ heading: `Bloque - ${block.name}`, code: block.sql }, { heading: "Ejecución", rows: [{ estado: "ejecutado", operaciones: block.detectedOperations.join(", ") || "solo lectura", filasAfectadas: block.rowsAffected.join(", ") || "-", resultsets: block.count }] }, ...block.resultsets.map((set) => ({ heading: `Resultado ${set.index + 1} (${set.total} filas)`, rows: set.rows }))];
}

function summarize(blocks) {
  return blocks.map((block) => ({ name: block.name, executed: true, operations: block.detectedOperations, rowsAffected: block.rowsAffected, resultsets: block.resultsets.map((set) => ({ index: set.index + 1, total: set.total })) }));
}