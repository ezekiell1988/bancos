#!/usr/bin/env node
// Smoke test del MCP bancos: protocolo + smoke() de cada tool, en orden de catálogo, sobre una
// carpeta de datos temporal (--datos-dir) con tipos y un respaldo inventados. Nunca toca datos/ real.
// Ejecución: node --check .mcp/bancos/server.mjs && node .mcp/bancos/tests/smoke.mjs

import { mkdirSync, mkdtempSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { spawnMcp, createChecker, schemaArraysDeclareItems } from '../../_shared/test-harness.mjs';

const SERVER_NAME = 'bancos';
const here = path.dirname(fileURLToPath(import.meta.url));
const serverPath = path.join(here, '..', 'server.mjs');
const toolsDir = path.join(here, '..', 'tools');

const tmp = mkdtempSync(path.join(os.tmpdir(), `${SERVER_NAME}-smoke-`));
const datosDir = path.join(tmp, 'datos');
mkdirSync(datosDir);
const jsonl = (rows) => rows.map((r) => JSON.stringify(r)).join('\n') + '\n';
writeFileSync(path.join(datosDir, 'tipos-gasto.jsonl'), jsonl([{ id: 'comida', nombre: 'Comida' }, { id: 'supermercado', nombre: 'Supermercado' }, { id: 'otros', nombre: 'Otros' }]));
writeFileSync(path.join(datosDir, 'tipos-ingreso.jsonl'), jsonl([{ id: 'salario-itqs', nombre: 'Salario ITQS' }]));
const respaldo = path.join(tmp, 'Factura Soda.jpg');
writeFileSync(respaldo, 'imagen falsa');

const { rpc, notify, callTool, toolJson, toolText, child } = spawnMcp(serverPath, ['--datos-dir', datosDir]);
const { check, report } = createChecker();

try {
  const init = await rpc('initialize', { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'smoke-test', version: '1.0.0' } });
  check('initialize responde con el nombre del servidor', init.result?.serverInfo?.name === SERVER_NAME, JSON.stringify(init.result?.serverInfo));
  check('protocolVersion conocida se respeta', init.result?.protocolVersion === '2024-11-05', init.result?.protocolVersion);

  const future = await rpc('initialize', { protocolVersion: '9999-99-99', capabilities: {}, clientInfo: { name: 'smoke-test', version: '1.0.0' } });
  check('protocolVersion desconocida → la más reciente soportada (sin echo ciego)', future.result?.protocolVersion !== '9999-99-99', future.result?.protocolVersion);
  notify('notifications/initialized');

  const files = readdirSync(toolsDir).filter((f) => f.endsWith('.mjs') && !f.startsWith('_')).sort();
  const expected = files.map((f) => path.basename(f, '.mjs'));
  const list = await rpc('tools/list');
  const names = list.result.tools.map((t) => t.name);
  check(`tools/list expone los ${expected.length} tools de tools/`,
    expected.every((n) => names.includes(n)) && names.length === expected.length,
    `faltantes=${expected.filter((n) => !names.includes(n)).join(', ') || 'ninguno'}`);
  check('todo parámetro array declara items', list.result.tools.every((t) => schemaArraysDeclareItems(t.inputSchema)));

  const modules = await Promise.all(files.map(async (f) => (await import(pathToFileURL(path.join(toolsDir, f)).href)).default));
  modules.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));
  const state = { datosDir, respaldo };
  for (const tool of modules) await tool.smoke?.({ rpc, notify, callTool, check, toolJson, toolText, state });

  report();
} catch (error) {
  console.error(`SMOKE TEST ERROR: ${error.message}`);
  process.exitCode = 1;
} finally {
  child.kill();
  rmSync(tmp, { recursive: true, force: true });
}
