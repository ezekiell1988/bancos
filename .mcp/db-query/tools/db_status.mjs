import { databaseStatus } from "../src/database.mjs";
export default {
  name: "db_status", order: 0,
  description: "Verifica que la configuración SQL local existe y opcionalmente prueba conexión, sin devolver servidor, usuario, contraseña ni base.",
  inputSchema: { type: "object", properties: { checkConnection: { type: "boolean" } }, additionalProperties: false },
  handler: (args) => databaseStatus(args),
  async smoke({ callTool, check, toolJson, state }) { const configured = toolJson(await callTool("db_status", {})); check("db_status detecta configuración", configured.configured === true); check("db_status no expone credenciales", !JSON.stringify(configured).match(/password|server|user/i)); const connected = toolJson(await callTool("db_status", { checkConnection: true })); state.connected = connected.connected === true; check("db_status conecta a SQL", state.connected === true, connected.error ?? ""); },
};
