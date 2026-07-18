# /ia — Contexto operativo de Bancos

Esta carpeta conserva conocimiento verificable del proyecto. No incluye estados de cuenta, números de cuenta, saldos, credenciales ni archivos fuente.

> Interfaz futura: MCP `iaWorkflow`. Mientras no esté expuesto, leer solo los archivos indicados por intención.

## Índice

| Archivo | Propósito | Leer cuando |
|---|---|---|
| `00_context.md` | Identidad, límites y stack | Siempre |
| `01_requirements.md` | Reglas financieras y producto | Planificar o revisar comportamiento |
| `02_architecture.md` | Diseño técnico y datos | Implementar o depurar |
| `03_plan.md` | Fases e hitos | Planificar |
| `04_tasks.md` | Trabajo accionable | Crear o ejecutar tarea |
| `05_progress.md` | Estado de trabajo | Continuar o cerrar sesión |
| `06_decisions.md` | Índice de ADRs | Cambiar arquitectura |
| `07_issues.md` | Problemas conocidos | Depurar |
| `08_retrospective.md` | Aprendizajes | Cerrar fase |

## Flujo

1. Validar estructura y cargar contexto mínimo por intención.
2. Solo implementar una tarea `Lista`; riesgo alto requiere aprobación explícita.
3. Cerrar tarea actualizando progreso, decisiones e issues aplicables.

## Skills de workflow

| Momento | Skill |
|---|---|
| Tareas | `bancos-task-management` |
| Revisión | `bancos-code-review` |
| Cierre | `bancos-session-closeout` |

Esquemas: `SCHEMAS.md`. Templates: `templates/`. Prompts: `prompts/`.
