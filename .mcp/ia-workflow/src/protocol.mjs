import { errorResult, log, ToolError } from "./common.mjs";

export class McpServer {
  constructor({ serverInfo, supportedVersions, tools, callTool, extraDispatch }) {
    this.serverInfo = serverInfo;
    this.supportedVersions = supportedVersions;
    this.tools = tools;
    this.callTool = callTool;
    this.extraDispatch = extraDispatch;
    this.buffer = "";
  }

  start() {
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", async (chunk) => {
      this.buffer += chunk;
      let lineEnd;
      while ((lineEnd = this.buffer.indexOf("\n")) !== -1) {
        const line = this.buffer.slice(0, lineEnd).trim();
        this.buffer = this.buffer.slice(lineEnd + 1);
        if (!line) continue;
        try {
          await this.handle(JSON.parse(line));
        } catch (error) {
          log(`mensaje MCP inválido: ${error.message}`);
        }
      }
    });
  }

  async handle(request) {
    if (!request || typeof request !== "object" || !Object.hasOwn(request, "id")) return;
    try {
      const result = await this.dispatch(request);
      this.write({ jsonrpc: "2.0", id: request.id, result });
    } catch (error) {
      this.write({ jsonrpc: "2.0", id: request.id, error: error.jsonRpcError ?? { code: -32603, message: error.message ?? "Error interno" } });
    }
  }

  async dispatch(request) {
    if (request.method === "initialize") {
      const requested = request.params?.protocolVersion;
      return {
        protocolVersion: this.supportedVersions.includes(requested) ? requested : this.supportedVersions[0],
        capabilities: { tools: {}, resources: {}, prompts: {} },
        serverInfo: this.serverInfo,
      };
    }
    if (request.method === "ping") return {};
    if (request.method === "tools/list") return { tools: this.tools };
    if (request.method === "tools/call") return this.callTool(request.params?.name, request.params?.arguments ?? {});
    const extra = await this.extraDispatch?.(request);
    if (extra !== undefined) return extra;
    const error = new Error(`Método no soportado: ${request.method}`);
    error.jsonRpcError = { code: -32601, message: error.message };
    throw error;
  }

  write(message) {
    process.stdout.write(`${JSON.stringify(message)}\n`);
  }
}

export function createToolCaller(tools) {
  const byName = new Map(tools.map((tool) => [tool.name, tool]));
  return async (name, args) => {
    const tool = byName.get(name);
    if (!tool) return errorResult(`tool desconocida: ${name}`);
    try {
      validateArgs(tool.inputSchema, args);
      return textResultFor(tool, await tool.handler(args));
    } catch (error) {
      if (error instanceof ToolError || error?.jsonRpcError) return errorResult(error.message);
      log(`error inesperado en ${name}: ${error.stack ?? error}`);
      return errorResult("error interno del tool");
    }
  };
}

function textResultFor(tool, payload) {
  const text = tool.format === "toon" ? encodeToon(payload) : JSON.stringify(payload, null, 2);
  return { content: [{ type: "text", text }], structuredContent: payload, isError: false };
}

function validateArgs(schema, args) {
  if (!args || typeof args !== "object" || Array.isArray(args)) throw new ToolError("arguments debe ser objeto");
  const selectedSchema = selectSchemaVariant(schema, args);
  const properties = selectedSchema.properties ?? {};
  for (const key of Object.keys(args)) if (!Object.hasOwn(properties, key)) throw new ToolError(`argumento no permitido: ${key}`);
  for (const key of selectedSchema.required ?? []) if (args[key] === undefined || args[key] === null || args[key] === "") throw new ToolError(`${key} requerido`);
  for (const [key, value] of Object.entries(args)) {
    const definition = properties[key];
    validateValue(key, value, definition);
  }
}

function selectSchemaVariant(schema, args) {
  if (!Array.isArray(schema.oneOf)) return schema;

  const matching = schema.oneOf.filter((variant) => matchesConstProperties(variant, args));
  if (matching.length !== 1) throw new ToolError("los argumentos no coinciden con una variante valida");
  return matching[0];
}

function matchesConstProperties(schema, args) {
  const properties = schema.properties ?? {};
  return Object.entries(properties)
    .filter(([, definition]) => Object.hasOwn(definition ?? {}, "const"))
    .every(([key, definition]) => args[key] === definition.const);
}

function validateValue(key, value, definition) {
  if (!definition) return;
  if (definition.const !== undefined && value !== definition.const) {
    throw new ToolError(`${key} debe ser ${definition.const}`);
  }
  if (definition.enum && !definition.enum.includes(value)) {
    throw new ToolError(`${key} debe ser uno de: ${definition.enum.join(", ")}`);
  }
  if (definition.type === "array") {
    if (!Array.isArray(value)) throw new ToolError(`${key} debe ser un array`);
    if (definition.items) value.forEach((item, index) => validateValue(`${key}[${index}]`, item, definition.items));
  }
  if (definition.type === "object") {
    if (!value || typeof value !== "object" || Array.isArray(value)) throw new ToolError(`${key} debe ser un objeto`);
    if (definition.additionalProperties === false || definition.properties) validateArgs(definition, value);
  }
  if (definition.type === "boolean" && typeof value !== "boolean") throw new ToolError(`${key} debe ser booleano`);
  if (definition.type === "integer" && (!Number.isInteger(value) || (definition.minimum !== undefined && value < definition.minimum))) {
    throw new ToolError(`${key} debe ser un entero valido`);
  }
  if (definition.type === "string") {
    if (typeof value !== "string") throw new ToolError(`${key} debe ser texto`);
    if (definition.minLength !== undefined && value.length < definition.minLength) throw new ToolError(`${key} no puede estar vacio`);
    if (definition.maxLength !== undefined && value.length > definition.maxLength) throw new ToolError(`${key} excede la longitud maxima`);
  }
}

function encodeToon(payload) {
  return JSON.stringify(payload);
}
