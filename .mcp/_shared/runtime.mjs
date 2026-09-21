// Factory de entrypoint compartida por los servidores MCP locales.
// Cada server.mjs es un adaptador delgado: resuelve su configuración de dominio
// (--project-root, --secrets-file, --ia-root, etc.) y delega el arranque aquí.

import { McpServer } from './protocol.mjs';
import { loadTools } from './registry.mjs';
import { ToolError, textResult, toonResult, errorResult, setServerName, log } from './results.mjs';

/**
 * @param {{ name: string, version?: string, toolsDir: string }} config
 */
export async function runServer({ name, version = '1.0.0', toolsDir }) {
  setServerName(name);

  const tools = await loadTools(toolsDir);
  const byName = new Map(tools.map((t) => [t.name, t]));
  const definitions = tools.map(({ name: toolName, description, inputSchema }) => ({
    name: toolName,
    description,
    inputSchema,
  }));

  async function callTool(toolName, args) {
    const tool = byName.get(toolName);
    if (!tool) return errorResult(`tool desconocida: ${toolName}`);
    try {
      const payload = await tool.handler(args ?? {});
      return tool.format === 'toon' ? toonResult(payload) : textResult(payload);
    } catch (err) {
      if (err instanceof ToolError) return errorResult(err.message);
      log(`error inesperado en ${toolName}: ${err.stack}`);
      return errorResult(`error interno: ${err.message}`);
    }
  }

  new McpServer({ name, version, tools: definitions, callTool }).start();
}
