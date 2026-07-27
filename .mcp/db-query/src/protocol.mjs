import { ToolError, errorResult, log } from "./common.mjs";

export class McpServer {
  constructor({ profile, tools }) {
    this.profile = profile;
    this.tools = tools;
    this.byName = new Map(tools.map((tool) => [tool.name, tool]));
    this.buffer = "";
  }

  start() {
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", async (chunk) => {
      this.buffer += chunk;
      let end;
      while ((end = this.buffer.indexOf("\n")) !== -1) {
        const line = this.buffer.slice(0, end).trim();
        this.buffer = this.buffer.slice(end + 1);
        if (!line) continue;
        try {
          await this.handle(JSON.parse(line));
        } catch (error) {
          log(`mensaje inválido: ${error.message}`);
        }
      }
    });
  }

  async handle(request) {
    if (!request || !Object.hasOwn(request, "id")) return;
    try {
      this.write({ jsonrpc: "2.0", id: request.id, result: await this.dispatch(request) });
    } catch (error) {
      this.write({ jsonrpc: "2.0", id: request.id, error: { code: -32603, message: error.message } });
    }
  }

  async dispatch(request) {
    if (request.method === "initialize") {
      const supported = ["2025-06-18", "2025-03-26", "2024-11-05"];
      return { protocolVersion: supported.includes(request.params?.protocolVersion) ? request.params.protocolVersion : supported[0], capabilities: { tools: {} }, serverInfo: { name: this.profile.serverName, version: "1.0.0" } };
    }
    if (request.method === "ping") return {};
    if (request.method === "tools/list") return { tools: this.tools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema })) };
    if (request.method === "tools/call") return this.call(request.params?.name, request.params?.arguments ?? {});
    throw new Error(`Método no soportado: ${request.method}`);
  }

  async call(name, args) {
    const tool = this.byName.get(name);
    if (!tool) return errorResult(`tool desconocida: ${name}`);
    try {
      validateArgs(tool.inputSchema, args);
      const payload = await tool.handler(args);
      return { content: [{ type: "text", text: JSON.stringify(payload, null, 2) }], structuredContent: payload, isError: false };
    } catch (error) {
      if (error instanceof ToolError) return errorResult(error.message);
      log(`error inesperado en ${name}: ${error.stack ?? error}`);
      return errorResult("error interno del tool");
    }
  }

  write(message) {
    process.stdout.write(`${JSON.stringify(message)}\n`);
  }
}

function validateArgs(schema, args) {
  if (!args || typeof args !== "object" || Array.isArray(args)) throw new ToolError("arguments debe ser objeto");
  for (const key of Object.keys(args)) if (!Object.hasOwn(schema.properties ?? {}, key)) throw new ToolError(`argumento no permitido: ${key}`);
  for (const key of schema.required ?? []) if (args[key] === undefined || args[key] === null || args[key] === "") throw new ToolError(`${key} requerido`);
  for (const [key, value] of Object.entries(args)) {
    const definition = schema.properties?.[key];
    if (definition?.enum && !definition.enum.includes(value)) throw new ToolError(`${key} inválido`);
  }
}