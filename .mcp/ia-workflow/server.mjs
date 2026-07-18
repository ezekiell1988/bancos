#!/usr/bin/env node
import path from "node:path";
import { fileURLToPath } from "node:url";
import { PROTOCOL_VERSION, SERVER_VERSION, SUPPORTED_PROTOCOL_VERSIONS } from "./src/constants.mjs";
import { errorResult, log } from "./src/common.mjs";
import { prompts, getPrompt } from "./src/prompts.mjs";
import { loadTools } from "./src/registry.mjs";
import { runtime } from "./src/runtime.mjs";
import { createToolCaller, McpServer } from "./src/protocol.mjs";

const here = path.dirname(fileURLToPath(import.meta.url));
const tools = await loadTools(path.join(here, "tools"));
const definitions = tools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema }));
const callTool = createToolCaller(tools);

new McpServer({
  serverInfo: { name: "ia-workflow-mcp", version: SERVER_VERSION },
  supportedVersions: SUPPORTED_PROTOCOL_VERSIONS.length ? SUPPORTED_PROTOCOL_VERSIONS : [PROTOCOL_VERSION],
  tools: definitions,
  callTool,
  async extraDispatch(request) {
    try {
      if (request.method === "resources/list") return { resources: await runtime.read.listResources() };
      if (request.method === "resources/read") return runtime.read.readResource(request.params ?? {});
      if (request.method === "resources/templates/list") return { resourceTemplates: [{ uriTemplate: "ia:///{path}", name: "ia_file", title: "Archivo Markdown de /ia", description: "Lee Markdown dentro de /ia.", mimeType: "text/markdown" }] };
      if (request.method === "prompts/list") return { prompts };
      if (request.method === "prompts/get") return getPrompt(request.params ?? {});
      return undefined;
    } catch (error) {
      if (error?.jsonRpcError) throw error;
      log(error.stack ?? error);
      return errorResult("error interno");
    }
  },
}).start();
