import { runtime } from "../src/runtime.mjs";

export default {
  name: "ia_validate", order: 0,
  description: "Valida archivos, carpetas, formato y posibles secretos del contrato /ia.",
  inputSchema: { type: "object", properties: {}, additionalProperties: false },
  handler: () => runtime.read.validateIa(),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_validate")); check("ia_validate devuelve valid", result.valid === true); check("ia_validate no tiene faltantes", result.missingFiles?.length === 0 && result.missingDirs?.length === 0); },
};
