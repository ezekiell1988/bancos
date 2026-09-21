// Puerto de DbExecTool.cs. Ejecuta un lote de sentencias T-SQL independientes contra SQL Server,
// en orden, y guarda el resultado en un Markdown con una sección numerada por consulta. Sin
// apply ni filtrado por palabra clave (ver README.md — política de escritura sin gate).
import path from 'node:path';
import { existsSync, rmSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { ToolError, clamp, getProjectRoot } from '../src/common.mjs';
import { executeQuery } from '../src/executor.mjs';
import { renderReport } from '../src/markdown.mjs';
import { resolveReportPath, toResponsePath } from '../src/paths.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));

export default {
  name: 'db_exec',

  description:
    'Ejecuta un lote de sentencias T-SQL independientes (SELECT, DECLARE, DDL, DML, procs) contra ' +
    'SQL Server, en orden, y guarda el resultado en un Markdown con una sección numerada por ' +
    'consulta (SQL, estado, filas afectadas, resultsets). Sin apply ni filtrado por palabra clave.',

  inputSchema: {
    type: 'object',
    properties: {
      queries: {
        type: 'array',
        items: { type: 'string' },
        description: 'Sentencias T-SQL a ejecutar en orden.',
      },
      filePath: {
        type: 'string',
        description: 'Ruta de destino del reporte Markdown (debe terminar en .md).',
      },
      timeoutSeconds: {
        type: 'integer',
        description: 'Timeout por consulta en segundos (1-120, default: 30).',
      },
    },
    required: ['queries', 'filePath'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!Array.isArray(args.queries) || args.queries.length === 0) {
      throw new ToolError('queries debe ser un array no vacío');
    }
    const normalized = args.queries.map((query, index) => {
      if (typeof query !== 'string' || query.trim().length === 0) throw new ToolError(`queries[${index}] no puede estar vacío`);
      return query.trim();
    });
    const timeoutSeconds = clamp(args.timeoutSeconds, 1, 120, 30);

    const projectRoot = getProjectRoot();
    const mcpRoot = path.join(projectRoot, '.mcp', 'db-query');
    const outputPath = resolveReportPath(projectRoot, mcpRoot, args.filePath);

    const results = [];
    for (const query of normalized) results.push(await executeQuery(query, timeoutSeconds));

    writeFileSync(outputPath, renderReport(normalized, results, timeoutSeconds), 'utf8');

    return {
      success: results.every((result) => result.status === 'ok'),
      filePath: toResponsePath(projectRoot, outputPath),
    };
  },

  async smoke({ callTool, check, toolJson }) {
    const missing = await callTool('db_exec', {});
    check('db_exec sin params retorna error', missing.result?.isError === true, JSON.stringify(missing.result));

    const invalidExt = await callTool('db_exec', { queries: ['SELECT 1'], filePath: 'smoke-test-report.txt' });
    check('db_exec rechaza extensión distinta de .md', invalidExt.result?.isError === true, JSON.stringify(invalidExt.result));

    const executed = await callTool('db_exec', { queries: ['SELECT 1 AS Uno'], filePath: 'smoke-test-report.md', timeoutSeconds: 10 });
    const parsed = toolJson(executed);
    check('db_exec ejecuta SELECT 1 contra la BD real', parsed.success === true, JSON.stringify(parsed));
    check('db_exec responde filePath', typeof parsed.filePath === 'string' && parsed.filePath.length > 0, JSON.stringify(parsed));

    // getProjectRoot() no aplica aquí: smoke() corre en el proceso runner, no en el server hijo
    // donde initConfig() se llamó — se resuelve la ruta desde este propio archivo.
    const reportPath = path.join(here, '..', 'queries', 'smoke-test-report.md');
    if (existsSync(reportPath)) rmSync(reportPath);
  },
};
