#!/usr/bin/env node
// Smoke test: deriva el catálogo de tools/, corre los checks genéricos de protocolo y
// ejecuta el smoke() co-ubicado de cada tool contra el /ia real del repo, en modo
// solo lectura o preview (apply:true jamás se ejercita aquí para no mutar /ia).
//
// Ejecución: node .mcp/ia-workflow/tests/smoke.mjs

import { readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { spawnMcp, createChecker } from '../../_shared/test-harness.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const serverPath = path.join(here, '..', 'server.mjs');
const toolsDir = path.join(here, '..', 'tools');
const projectRoot = path.resolve(here, '..', '..', '..');

const { rpc, notify, callTool, toolJson, toolText, child } = spawnMcp(serverPath, [
  '--project-root',
  projectRoot,
]);
const { check, report } = createChecker();

try {
  const init = await rpc('initialize', {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: { name: 'smoke-test', version: '1.0.0' },
  });
  check(
    'initialize responde con nombre del servidor',
    init.result?.serverInfo?.name === 'iaWorkflow',
    `serverInfo=${JSON.stringify(init.result?.serverInfo)}`,
  );
  check(
    'protocolVersion conocida se respeta',
    init.result?.protocolVersion === '2024-11-05',
    `respondió ${init.result?.protocolVersion}`,
  );
  notify('notifications/initialized');

  const initFuture = await rpc('initialize', {
    protocolVersion: '9999-99-99',
    capabilities: {},
    clientInfo: { name: 'smoke-test', version: '1.0.0' },
  });
  check(
    'protocolVersion desconocida → la más reciente soportada (no echo ciego)',
    initFuture.result?.protocolVersion !== '9999-99-99',
    `respondió ${initFuture.result?.protocolVersion}`,
  );

  const toolFiles = readdirSync(toolsDir)
    .filter((f) => f.endsWith('.mjs') && !f.startsWith('_'))
    .sort();
  const expected = toolFiles.map((f) => path.basename(f, '.mjs'));

  const toolsList = await rpc('tools/list');
  const names = toolsList.result.tools.map((t) => t.name);

  check(
    `tools/list expone los ${expected.length} tools de tools/`,
    expected.every((n) => names.includes(n)) && names.length === expected.length,
    `expuestos=${names.length}, faltantes=${expected.filter((n) => !names.includes(n)).join(', ') || 'ninguno'}`,
  );
  check('tools/list incluye los 22 tools de ia-workflow', expected.length === 22, `total=${expected.length}`);

  const modules = [];
  for (const file of toolFiles) {
    const mod = await import(pathToFileURL(path.join(toolsDir, file)).href);
    modules.push(mod.default);
  }
  modules.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));

  const state = {};
  const ctx = { rpc, notify, callTool, check, toolJson, toolText, state };

  for (const tool of modules) {
    if (typeof tool.smoke !== 'function') {
      console.log(`SKIP ${tool.name} — sin smoke() co-ubicado`);
      continue;
    }
    try {
      await tool.smoke(ctx);
    } catch (err) {
      check(`smoke() de ${tool.name} no lanza`, false, err.message);
    }
  }

  report();
} catch (err) {
  console.error(`SMOKE TEST ERROR: ${err.message}`);
  process.exitCode = 1;
} finally {
  child.kill();
}
