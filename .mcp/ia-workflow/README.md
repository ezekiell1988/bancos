# iaWorkflow (Node.js — skill mcp-vscode)

Servidor MCP local, propiedad del proyecto, para operar `/ia` mediante `stdio` (JSON-RPC
delimitado por saltos de línea). Migrado de .NET a Node.js en TASK-EBC-MCP-97 (ADR-123, revierte
ADR-119): 1 tool = 1 archivo en `tools/`, autodescubierto por el runtime genérico compartido
`.mcp/_shared/`.

La política de escritura es segura: cada operación ofrece preview por defecto y requiere
`apply=true`; `delete_task` exige además `confirm=true`. Las rutas se confinan a `/ia`, las
escrituras usan una allowlist (`WRITABLE_ROOTS` en `src/store.mjs`) y se rechazan patrones de
secretos antes de escribir.

El patrón operativo de ahorro de tokens es: `ia_validate` → `ia_get_context` en `summary` o
`pathsOnly` → `ia_inspect` únicamente para el archivo necesario.

## Arquitectura

```text
.mcp/ia-workflow/
├── server.mjs           ← adaptador delgado sobre _shared/runtime.mjs. No se edita al agregar tools
├── src/
│   ├── common.mjs       ← initWorkflow()/getWorkflow() (delega en paths.mjs)
│   ├── paths.mjs        ← workflowPathsFromArgs(): --project-root/--ia-root/--runtime studio
│   ├── store.mjs        ← IO confinado a /ia: read/readOptional/enumerateMarkdown/resolveTask/
│   │                        resolveIssue/commit (preview/apply atómico) — puerto de las
│   │                        primitivas de WorkflowService.cs
│   ├── text.mjs         ← helpers de texto: timestamp (America/Costa_Rica), Field/SetField,
│   │                        Slug, Bullets, AppendEvent (historial JSONL V2), etc.
│   ├── markdown.mjs     ← manipulación de secciones Markdown: InsertTaskRow, MarkPlanTaskComplete,
│   │                        AddBulletUnderHeading, UpdateIndependentPlan*, etc.
│   └── workflow.mjs     ← las 22 operaciones (createWorkflow(iaRoot)), puerto 1:1 de
│                            WorkflowService.cs (validate/getContext/inspect + 19 tools de escritura)
├── tools/                ← 22 archivos, uno por tool (ver catálogo abajo)
└── tests/
    └── smoke.mjs         ← corre contra el /ia real del repo en modo solo lectura/preview
```

## Configuración por perfil

El perfil `stdio` local de VS Code, Claude Code y Codex usa `--runtime studio` y lee `name`,
`initials`, `email` y `platform` desde `<proyecto>/.local-secrets/ia-workflow.json`
(`src/paths.mjs#workflowPathsFromArgs`, puerto de `WorkflowPaths.FromArgs`). Al crear una tarea,
esos valores son los defaults del autor (vía `IA_WORKFLOW_AUTHOR_*` en `process.env`, fijadas por
`paths.mjs` igual que el `.NET` anterior). Las rutas se derivan del proyecto y el archivo no se
versiona.

## Catálogo de tools (22)

`ia_validate`, `ia_get_context`, `ia_inspect` (lectura) — `create_task`, `approve_task`,
`work_task`, `return_task_to_draft`, `finish_task`, `restore_task`, `reopen_task`,
`duplicate_task`, `delete_task`, `assign_task_to_phase`, `update_independent_plan`,
`ia_create_issue`, `close_issue`, `ia_link_issue_to_task`, `ia_add_progress_entry`,
`archive_progress`, `ia_create_decision`, `resolve_phase_review`, `migrate_tasks_to_v2`.

Cada tool vive en `tools/{name}.mjs` con `export default { name, description, inputSchema,
handler, smoke? }`, autodescubierto por `.mcp/_shared/registry.mjs` — agregar uno nuevo no toca
`server.mjs`.

## Validación

```bash
node --check .mcp/_shared/*.mjs .mcp/ia-workflow/server.mjs \
  .mcp/ia-workflow/src/*.mjs .mcp/ia-workflow/tools/*.mjs
node .mcp/ia-workflow/tests/smoke.mjs
```

El smoke corre contra el `/ia` real del repositorio: los tools de escritura se ejercitan siempre
en preview (`apply` omitido) o con IDs inexistentes para forzar la ruta de error, nunca mutan el
repo. La paridad de formato con la versión .NET se verificó manualmente comparando `read_task`,
`ia_validate` y previews de `approve_task`/`create_task` byte a byte, y con una escritura real de
`ia_add_progress_entry` cuyo resultado coincide exactamente con el formato que producía el host
.NET.

## Recursos y prompts (gap conocido)

La versión .NET expone además `resources` (`ia:///...`, lectura de Markdown vía `Add Context`) y
`prompts` (`planificar`, `implementar`, `cerrar_sesion`). El runtime compartido `.mcp/_shared/`
(heredado de antes de ADR-119) solo implementa `tools`. `readResource` quedó portado en
`workflow.mjs` pero sin exponerse todavía como primitivo MCP — usar `ia_inspect action=read_file`
como equivalente vía tool mientras no se agregue soporte de `resources`/`prompts` al runtime
compartido.

## Código .NET anterior (ADR-119)

`src/IaWorkflow.Tools`, `src/IaWorkflow.Stdio`, `tests/IaWorkflow.StdioSmoke`, `IaWorkflow.sln`,
`Directory.Build.props` y el `.gitignore` de .NET se eliminaron tras validar el equivalente Node
(smoke 22/22, paridad byte a byte contra la versión .NET aún conectada en esa sesión, y una
escritura real con formato idéntico) — recuperables en el historial de git si hiciera falta.
