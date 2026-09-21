#!/usr/bin/env node
// Smoke test contra la instancia real de Bancos (dbQuery/db.json). Deriva el catálogo de
// tools/, corre los checks genéricos de protocolo y ejecuta el smoke() co-ubicado de cada tool.
//
// Ejecución: node .mcp/db-query/tests/smoke.mjs

import { readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { spawnMcp, createChecker } from '../../_shared/test-harness.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const serverPath = path.join(here, '..', 'server.mjs');
const toolsDir = path.join(here, '..', 'tools');
const projectRoot = path.resolve(here, '..', '..', '..');
const { check, report } = createChecker();

const INSTANCES = [{ label: 'dbQuery', secretsFile: 'db.json', serverName: null }];

async function runInstance({ label, secretsFile, serverName }, { withFullProtocolChecks }) {
  const args = ['--project-root', projectRoot, '--secrets-file', secretsFile];
  if (serverName) args.push('--server-name', serverName);
  const expectedName = serverName ?? 'dbQuery';

  const { rpc, notify, callTool, toolJson, toolText, child } = spawnMcp(serverPath, args);
  try {
    const init = await rpc('initialize', {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: { name: 'smoke-test', version: '1.0.0' },
    });
    check(
      `[${label}] initialize responde serverInfo.name=${expectedName}`,
      init.result?.serverInfo?.name === expectedName,
      `serverInfo=${JSON.stringify(init.result?.serverInfo)}`,
    );
    notify('notifications/initialized');

    if (withFullProtocolChecks) {
      check(
        '[dbQuery] protocolVersion conocida se respeta',
        init.result?.protocolVersion === '2024-11-05',
        `respondió ${init.result?.protocolVersion}`,
      );

      const initFuture = await rpc('initialize', {
        protocolVersion: '9999-99-99',
        capabilities: {},
        clientInfo: { name: 'smoke-test', version: '1.0.0' },
      });
      check(
        '[dbQuery] protocolVersion desconocida → la más reciente soportada (no echo ciego)',
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
        `[dbQuery] tools/list expone los ${expected.length} tools de tools/`,
        expected.every((n) => names.includes(n)) && names.length === expected.length,
        `expuestos=${names.length}, faltantes=${expected.filter((n) => !names.includes(n)).join(', ') || 'ninguno'}`,
      );

      const tool = toolsList.result.tools.find((t) => t.name === 'db_exec');
      const required = new Set(tool.inputSchema.required ?? []);
      check(
        '[dbQuery] db_exec requiere queries y filePath',
        required.has('queries') && required.has('filePath'),
        [...required].join(','),
      );
    }

    const modules = [];
    for (const file of readdirSync(toolsDir).filter((f) => f.endsWith('.mjs') && !f.startsWith('_')).sort()) {
      const mod = await import(pathToFileURL(path.join(toolsDir, file)).href);
      modules.push(mod.default);
    }
    modules.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));

    const state = {};
    const ctx = { rpc, notify, callTool, check: (name, cond, detail) => check(`[${label}] ${name}`, cond, detail), toolJson, toolText, state };
    for (const tool of modules) {
      if (typeof tool.smoke !== 'function') {
        console.log(`SKIP [${label}] ${tool.name} — sin smoke() co-ubicado`);
        continue;
      }
      try {
        await tool.smoke(ctx);
      } catch (err) {
        check(`[${label}] smoke() de ${tool.name} no lanza`, false, err.message);
      }
    }
  } finally {
    child.kill();
  }
}

try {
  await runInstance(INSTANCES[0], { withFullProtocolChecks: true });
  report();
} catch (err) {
  console.error(`SMOKE TEST ERROR: ${err.message}`);
  process.exitCode = 1;
}
