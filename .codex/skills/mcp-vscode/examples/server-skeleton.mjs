#!/usr/bin/env node
// server.mjs genérico — copiar a .mcp/{servidor}/server.mjs.
//
// Este archivo NO se edita al agregar tools: el registry autodescubre todo archivo
// tools/*.mjs (sin prefijo "_") y valida su contrato al arrancar.
//
// Transporte: stdio con JSON-RPC delimitado por saltos de línea (requerido por VS Code).
// Logs solo a stderr; stdout contiene únicamente mensajes MCP.

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { McpServer } from './src/protocol.mjs';
import { loadTools } from './src/registry.mjs';
import { ToolError, textResult, toonResult, errorResult, log } from './src/common.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));

const tools = await loadTools(path.join(here, 'tools'));
const byName = new Map(tools.map((t) => [t.name, t]));

// Descriptores para tools/list — solo los campos del protocolo.
const definitions = tools.map(({ name, description, inputSchema }) => ({
  name,
  description,
  inputSchema,
}));

async function callTool(name, args) {
  const tool = byName.get(name);
  if (!tool) return errorResult(`tool desconocida: ${name}`);
  try {
    const payload = await tool.handler(args ?? {});
    return tool.format === 'toon' ? toonResult(payload) : textResult(payload);
  } catch (err) {
    if (err instanceof ToolError) return errorResult(err.message);
    log(`error inesperado en ${name}: ${err.stack}`);
    return errorResult(`error interno: ${err.message}`);
  }
}

new McpServer({ tools: definitions, callTool }).start();
