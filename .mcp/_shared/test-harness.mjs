// Arnés compartido de smoke tests: transporte JSON-RPC newline-delimited sobre stdio
// contra un server.mjs real, más el checker de aserciones. Las suites por MCP conservan
// únicamente el fixture y las aserciones de dominio.

import { spawn } from 'node:child_process';

/** Arranca `serverPath` como hijo y expone rpc/notify/callTool sobre su stdio. */
export function spawnMcp(serverPath, args = []) {
  const child = spawn(process.execPath, [serverPath, ...args], { stdio: ['pipe', 'pipe', 'pipe'] });
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
        if (resolver) {
          pending.delete(msg.id);
          resolver(msg);
        }
      } catch {
        /* líneas no-JSON se ignoran */
      }
    }
  });

  function rpc(method, params, timeoutMs = 15_000) {
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

  function notify(method, params) {
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method, params }) + '\n');
  }

  function callTool(name, args = {}, timeoutMs = 15_000) {
    return rpc('tools/call', { name, arguments: args }, timeoutMs);
  }

  function toolJson(res) {
    return JSON.parse(res.result.content[0].text);
  }

  function toolText(res) {
    return res.result.content[0].text;
  }

  return { child, rpc, notify, callTool, toolJson, toolText };
}

/** Checker de aserciones con salida `OK/FAIL` y control de process.exitCode. */
export function createChecker() {
  let failures = 0;

  function check(nombre, cond, detalle = '') {
    console.log(`${cond ? 'OK  ' : 'FAIL'} ${nombre}${detalle ? ` — ${detalle}` : ''}`);
    if (!cond) failures++;
    return cond;
  }

  function report() {
    console.log(failures === 0 ? '\nSMOKE TEST: TODO OK' : `\nSMOKE TEST: ${failures} fallo(s)`);
    process.exitCode = failures === 0 ? 0 : 1;
  }

  return { check, report, get failures() { return failures; } };
}

/** Recorre un inputSchema (properties/items/oneOf) verificando que todo type:array declare items. */
export function schemaArraysDeclareItems(schema) {
  let ok = true;
  const walk = (s) => {
    if (!s || typeof s !== 'object') return;
    if (s.type === 'array' && !s.items) ok = false;
    for (const sub of Object.values(s.properties ?? {})) walk(sub);
    if (s.items) walk(s.items);
    for (const v of s.oneOf ?? []) walk(v);
  };
  walk(schema);
  return ok;
}
