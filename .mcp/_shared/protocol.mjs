// Protocolo MCP sobre stdio — JSON-RPC delimitado por saltos de línea.
// stdout: solo mensajes MCP. Logs → stderr (results.mjs#log).

import { createInterface } from 'node:readline';
import { log } from './results.mjs';

const SUPPORTED_VERSIONS = ['2025-03-26', '2024-11-05'];

export class McpServer {
  /**
   * @param {{ name: string, version: string, tools: Array, callTool: Function }} opts
   */
  constructor({ name = 'mcp-server', version = '1.0.0', tools, callTool }) {
    this.serverInfo = { name, version };
    this.tools = tools;
    this.callTool = callTool;
  }

  start() {
    const rl = createInterface({ input: process.stdin, terminal: false });
    rl.on('line', (line) => {
      const trimmed = line.trim();
      if (!trimmed) return;
      let msg;
      try {
        msg = JSON.parse(trimmed);
      } catch {
        log(`línea no-JSON ignorada: ${trimmed.slice(0, 120)}`);
        return;
      }
      this.#dispatch(msg).catch((err) => log(`dispatch error: ${err.stack}`));
    });
    log(`servidor ${this.serverInfo.name} v${this.serverInfo.version} listo (stdio)`);
  }

  #send(payload) {
    process.stdout.write(JSON.stringify(payload) + '\n');
  }

  #reply(id, result) {
    this.#send({ jsonrpc: '2.0', id, result });
  }

  #replyError(id, code, message) {
    this.#send({ jsonrpc: '2.0', id, error: { code, message } });
  }

  async #dispatch(msg) {
    const { id, method, params } = msg;
    // Notificaciones (sin id): nunca se responden.
    if (id === undefined || id === null) return;

    switch (method) {
      case 'initialize': {
        const requested = params?.protocolVersion;
        const version = SUPPORTED_VERSIONS.includes(requested)
          ? requested
          : SUPPORTED_VERSIONS[0];
        this.#reply(id, {
          protocolVersion: version,
          capabilities: { tools: {} },
          serverInfo: this.serverInfo,
        });
        return;
      }
      case 'tools/list':
        this.#reply(id, { tools: this.tools });
        return;
      case 'tools/call': {
        const { name, arguments: args } = params ?? {};
        const validation = this.#validateArgs(name, args ?? {});
        if (validation) {
          this.#reply(id, {
            content: [{ type: 'text', text: JSON.stringify({ error: validation }) }],
            isError: true,
          });
          return;
        }
        const result = await this.callTool(name, args ?? {});
        this.#reply(id, result);
        return;
      }
      case 'ping':
        this.#reply(id, {});
        return;
      default:
        this.#replyError(id, -32601, `método no soportado: ${method}`);
    }
  }

  /**
   * Valida args contra el inputSchema publicado (incluidas variantes oneOf
   * discriminadas por cualquier propiedad con `const`). Retorna string de error o null.
   */
  #validateArgs(name, args) {
    const tool = this.tools.find((t) => t.name === name);
    if (!tool) return null; // callTool responderá "tool desconocida"
    return validateSchema(args, tool.inputSchema);
  }
}

/** Validación mínima de JSON Schema: oneOf discriminado, required, tipos, enum, additionalProperties. */
export function validateSchema(args, schema) {
  if (!schema || typeof schema !== 'object') return null;

  if (Array.isArray(schema.oneOf)) {
    const variant = resolveVariant(args, schema.oneOf);
    if (!variant) {
      const discriminators = collectDiscriminators(schema.oneOf);
      return `argumentos no coinciden con ninguna variante; discriminantes válidos: ${discriminators.join(', ')}`;
    }
    return validateSchema(args, variant);
  }

  if (schema.type === 'object') {
    const props = schema.properties ?? {};
    for (const req of schema.required ?? []) {
      if (args[req] === undefined) return `falta parámetro requerido: ${req}`;
    }
    if (schema.additionalProperties === false) {
      for (const key of Object.keys(args)) {
        if (!(key in props)) return `parámetro no permitido en esta variante: ${key}`;
      }
    }
    for (const [key, value] of Object.entries(args)) {
      const propSchema = props[key];
      if (!propSchema || value === undefined) continue;
      const err = validateValue(key, value, propSchema);
      if (err) return err;
    }
  }
  return null;
}

/** Resuelve la variante oneOf por cualquier propiedad con `const` que coincida. */
function resolveVariant(args, variants) {
  for (const variant of variants) {
    const props = variant.properties ?? {};
    const constEntries = Object.entries(props).filter(([, s]) => s.const !== undefined);
    if (constEntries.length === 0) continue;
    if (constEntries.every(([key, s]) => args[key] === s.const)) return variant;
  }
  return null;
}

function collectDiscriminators(variants) {
  const out = [];
  for (const variant of variants) {
    for (const [key, s] of Object.entries(variant.properties ?? {})) {
      if (s.const !== undefined) out.push(`${key}=${s.const}`);
    }
  }
  return out;
}

function validateValue(key, value, schema) {
  if (schema.const !== undefined) {
    return value === schema.const ? null : `${key} debe ser ${JSON.stringify(schema.const)}`;
  }
  if (schema.enum && !schema.enum.includes(value)) {
    return `${key} debe ser uno de: ${schema.enum.join(', ')}`;
  }
  const type = schema.type;
  if (!type) return null;
  if (type === 'string' && typeof value !== 'string') return `${key} debe ser string`;
  if (type === 'number' && typeof value !== 'number') return `${key} debe ser number`;
  if (type === 'integer' && !Number.isInteger(value)) return `${key} debe ser entero`;
  if (type === 'boolean' && typeof value !== 'boolean') return `${key} debe ser boolean`;
  if (type === 'array') {
    if (!Array.isArray(value)) return `${key} debe ser array`;
    if (schema.minItems !== undefined && value.length < schema.minItems) {
      return `${key} debe tener al menos ${schema.minItems} elemento(s)`;
    }
    if (schema.items) {
      for (const item of value) {
        const err = validateValue(`${key}[]`, item, schema.items);
        if (err) return err;
      }
    }
  }
  if (type === 'object' && (typeof value !== 'object' || value === null || Array.isArray(value))) {
    return `${key} debe ser objeto`;
  }
  return null;
}
