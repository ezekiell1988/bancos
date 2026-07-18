import { requireString, rpcError } from "./common.mjs";

export const prompts = [
  prompt("create_task", "Crear tarea", "Convierte una solicitud en una tarea Borrador."),
  prompt("approve_task", "Aprobar tarea", "Valida y mueve una tarea a Lista.", "taskId"),
  prompt("work_task", "Trabajar tarea", "Valida gates y prepara contexto de trabajo.", "taskId"),
  prompt("finish_task", "Finalizar tarea", "Cierra una tarea sincronizando 03, 04 y 05.", "taskId"),
  prompt("close_issue", "Cerrar issue", "Archiva un issue y sincroniza 05 y 07.", "issueId"),
  prompt("ia_planificar_sesion", "Planificar sesión", "Carga contexto mínimo para planificar."),
  prompt("ia_implementar_tarea", "Implementar tarea", "Guía la implementación de una tarea.", "taskId"),
  prompt("ia_revisar_cambios", "Revisar cambios", "Revisa cambios contra arquitectura y ADRs."),
  prompt("ia_depurar_issue", "Depurar issue", "Guía la investigación de un issue.", "issueId"),
  prompt("ia_cerrar_sesion", "Cerrar sesión", "Sincroniza el estado de la sesión."),
];

export function getPrompt(params) {
  const name = requireString(params.name, "name");
  const args = params.arguments ?? {};
  const taskId = args.taskId ?? "{taskId}";
  const issueId = args.issueId ?? "{issueId}";
  const texts = {
    create_task: "Usa create_task primero en preview y aplica solo con confirmación.",
    approve_task: `Aprueba ${taskId}: preview, confirma y aplica.`,
    work_task: `Llama work_task para ${taskId} y respeta alcance, riesgo y aprobación.`,
    finish_task: `Cierra ${taskId} con archivos, validaciones, pendientes, riesgos y rollback; verifica 03, 04 y 05.`,
    close_issue: `Cierra ${issueId} con resolución, causa raíz y aprendizaje; verifica 05 y 07.`,
    ia_planificar_sesion: "Llama ia_validate y luego ia_get_context con intent=planificar y mode=summary.",
    ia_implementar_tarea: `Carga contexto y trabaja únicamente ${taskId}.`,
    ia_revisar_cambios: "Revisa tarea, arquitectura, ADRs, issues y pruebas.",
    ia_depurar_issue: `Depura ${issueId} antes de editar código.`,
    ia_cerrar_sesion: "Actualiza tareas, progreso, issues y decisiones mediante previews seguros.",
  };
  if (!texts[name]) throw rpcError(-32602, `Prompt desconocido: ${name}`);
  return { description: prompts.find((item) => item.name === name)?.description ?? name, messages: [{ role: "user", content: { type: "text", text: texts[name] } }] };
}

function prompt(name, title, description, argument) {
  return { name, title, description, arguments: argument ? [{ name: argument, description: argument, required: true }] : [] };
}
