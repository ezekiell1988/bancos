#!/usr/bin/env node
// Runner de smoke test para un servidor MCP stdio con estructura tools/.
// Copiar a .mcp/{servidor}/tests/smoke.mjs. NO crece al agregar tools:
//   1. Deriva el catálogo esperado leyendo la carpeta tools/ (mismo criterio que el registry).
//   2. Corre los checks genéricos de protocolo.
//   3. Importa cada tool y ejecuta su smoke(ctx) si existe, en orden de catálogo.
//
// Ejecución:
//   node --check .mcp/mi-servidor/server.mjs && node .mcp/mi-servidor/tests/smoke.mjs
//
// Requiere: sesión activa del sistema de auth que use el servidor (az cli, PAT, etc.).
// Los smoke() de tools que requieren auth deben early-return si state.loggedIn es false.

import { spawn } from 'node:child_process';
import { readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const serverPath = path.join(here, '..', 'server.mjs');
const toolsDir = path.join(here, '..', 'tools');

// ─── Infraestructura JSON-RPC sobre stdio (newline-delimited, nunca Content-Length) ──

const child = spawn(process.execPath, [serverPath], { stdio: ['pipe', 'pipe', 'pipe'] });
child.stderr.on('data', (d) => process.stderr.write(`[server] ${d}`));

let buffer = '';
const pending = new Map();
let nextId = 1;

child.stdout.on('data', (chunk) => {
  buffer += chunk.toString();
  let idx;
  while ((idx = buffer.indexOf('\n')) !== -1) {
    const line = buffer.slice(0, idx).trim();
    buffer = buffer.slice(idx + 1);
    if (!line) continue;
    try {
      const msg = JSON.parse(line);
      const resolver = pending.get(msg.id);
      if (resolver) { pending.delete(msg.id); resolver(msg); }
    } catch { /* ignorar líneas no-JSON del servidor */ }
  }
});

/** Envía un request JSON-RPC y espera la respuesta. */
function rpc(method, params, timeoutMs = 30_000) {
  const id = nextId++;
  return new Promise((resolve, reject) => {
    pending.set(id, resolve);
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n');
    setTimeout(() => {
      if (pending.has(id)) {
        pending.delete(id);
        reject(new Error(`timeout esperando ${method} (id=${id})`));
      }
    }, timeoutMs);
  });
}

/** Envía una notificación sin esperar respuesta. */
function notify(method, params) {
  child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method, params }) + '\n');
}

/** Atajo para llamar un tool. */
function callTool(name, args = {}, timeoutMs = 30_000) {
  return rpc('tools/call', { name, arguments: args }, timeoutMs);
}

// ─── Helpers de extracción ─────────────────────────────────────────────────────

/** Payload JSON de un tool sin format:'toon'. */
function toolJson(res) {
  return JSON.parse(res.result.content[0].text);
}

/** Texto crudo de un tool TOON — asserts por substring/regex, no JSON.parse. */
function toolText(res) {
  return res.result.content[0].text;
}

// ─── Aserciones ────────────────────────────────────────────────────────────────

let failures = 0;
function check(nombre, cond, detalle = '') {
  console.log(`${cond ? 'OK  ' : 'FAIL'} ${nombre}${detalle ? ` — ${detalle}` : ''}`);
  if (!cond) failures++;
}

// ─── Suite ─────────────────────────────────────────────────────────────────────

try {
  // 1. Handshake MCP
  const init = await rpc('initialize', {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: { name: 'smoke-test', version: '1.0.0' },
  });
  check(
    'initialize responde con nombre del servidor',
    typeof init.result?.serverInfo?.name === 'string',
    `serverInfo=${JSON.stringify(init.result?.serverInfo)}`,
  );
  check(
    'protocolVersion conocida se respeta',
    init.result?.protocolVersion === '2024-11-05',
    `respondió ${init.result?.protocolVersion}`,
  );
  notify('notifications/initialized');

  // 2. protocolVersion desconocida → newest soportada (no echo ciego)
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

  // 3. Catálogo derivado de la carpeta tools/ (mismo criterio que el registry)
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

  // 4. Cargar los módulos de tool y ejecutar su smoke() en orden de catálogo
  const modules = [];
  for (const file of toolFiles) {
    const mod = await import(pathToFileURL(path.join(toolsDir, file)).href);
    modules.push(mod.default);
  }
  modules.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));

  if (modules.length && (modules[0].order ?? 100) === 0) {
    check('tool con order:0 aparece primero en tools/list', names[0] === modules[0].name, `primero=${names[0]}`);
  }

  const state = {}; // compartido entre smokes (p.ej. state.loggedIn del tool de login)
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

  // ─── Resultado final ─────────────────────────────────────────────────────────
  console.log(failures === 0 ? '\nSMOKE TEST: TODO OK' : `\nSMOKE TEST: ${failures} fallo(s)`);
  process.exitCode = failures === 0 ? 0 : 1;
} catch (err) {
  console.error(`SMOKE TEST ERROR: ${err.message}`);
  process.exitCode = 1;
} finally {
  child.kill();
}
