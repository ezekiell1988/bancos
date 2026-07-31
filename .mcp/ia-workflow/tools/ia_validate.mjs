import { runtime } from "../src/runtime.mjs";

export default {
  name: "ia_validate", order: 0,
  description: "Valida archivos, carpetas, formato y posibles secretos del contrato /ia; devuelve acciones correctivas si faltan rutas requeridas.",
  inputSchema: { type: "object", properties: {}, additionalProperties: false },
  handler: () => runtime.read.validateIa(),
  async smoke({ callTool, check, toolJson, state }) {
    const issuesOpen = `${state.iaRoot}/07_issues/open`;
    await state.fs.rm(issuesOpen, { recursive: true, force: true });
    const missing = toolJson(await callTool("ia_validate"));
    check("ia_validate identifica faltantes", missing.valid === false && missing.missingDirs?.includes("07_issues/open"));
    check("ia_validate explica la reparación", missing.remediation?.some((item) => item.includes("07_issues/open")) === true);
    await state.fs.mkdir(issuesOpen, { recursive: true });
    const result = toolJson(await callTool("ia_validate"));
    check("ia_validate devuelve valid tras reparar", result.valid === true);
    check("ia_validate no tiene faltantes tras reparar", result.missingFiles?.length === 0 && result.missingDirs?.length === 0);
  },
};
