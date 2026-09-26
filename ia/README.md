# /ia — Contexto operativo de Bancos

Esta carpeta conserva conocimiento verificable del proyecto. No incluye estados de cuenta, números de cuenta, saldos, credenciales ni archivos fuente.

`Bancos.Mcp` es el único proyecto funcional y la única interfaz del producto: un LLM opera Bancos exclusivamente mediante tools MCP. No hay API HTTP ni interfaz web dentro de la arquitectura activa.

## Organización de features MCP

Los adaptadores MCP que implementan un caso de uso viven junto a su feature. Por ejemplo, `DetectImportTemplateTool`, su servicio y sus opciones pertenecen a `Features/TemplateDetection/`. La carpeta `Tools/` se reserva para componentes transversales del protocolo, como `IMcpTool`, `ToolRegistry` y `StatusTool`.

## Índice

| Archivo | Propósito | Leer cuando |
|---|---|---|
| `00_context.md` | Identidad, límites y stack | Siempre |
| `01_requirements.md` | Reglas financieras y producto | Planificar o revisar comportamiento |
| `02_architecture.md` | Diseño técnico, datos y tools MCP | Implementar o depurar |
| `03_plan.md` | Fases e hitos MCP | Planificar |
| `04_tasks.md` | Trabajo accionable | Crear o ejecutar tarea |
| `05_progress.md` | Estado de trabajo | Continuar o cerrar sesión |
| `06_decisions.md` | Índice de ADRs | Cambiar arquitectura |
| `07_issues.md` | Problemas conocidos | Depurar |
| `08_retrospective.md` | Aprendizajes | Cerrar fase |
| `rules/` | Reglas de workflow y reglas propias del proyecto | Planificar, gestionar tareas, revisar o cerrar sesión |

## Reglas de workflow (`ia/rules/`)

Los procedimientos de trabajo viven en `rules/`, no en skills globales. Leerlos con el MCP `iaWorkflow` (`ia_inspect action=read_file path=rules/<archivo>`) o directo si el MCP no está disponible. Índice completo: `rules/README.md`.

| Intención | Regla | Además |
|---|---|---|
| Iniciar sesión, planificar, elegir la próxima tarea | `rules/planificacion.md` | — |
| Crear, aprobar, iniciar, bloquear o cerrar una tarea | `rules/gestion-tareas.md` | `rules/mapa-area-skill.md` antes de implementar |
| Revisar un diff o un cambio antes de commit/PR | `rules/revision-codigo.md` | `rules/checklist-revision.md` |
| Cerrar la sesión y dejar `/ia` al día | `rules/cierre-sesion.md` | — |

## Flujo

1. El LLM usa tools MCP para cargar, consultar, clasificar, cerrar y reportar.
2. La clasificación intenta reglas .NET; solo después usa Azure AI y, si no hay resolución confiable, queda `No clasificado` para confirmación humana.
3. Una corrección del usuario se convierte en regla determinista reutilizable y conserva auditoría.
4. Solo implementar una tarea `Lista`; riesgo alto requiere aprobación explícita.
5. Cerrar tarea actualizando progreso, decisiones e issues aplicables.

Esquemas: `SCHEMAS.md`. Templates: `templates/`. Prompts: `prompts/`. Reglas: `rules/`.
