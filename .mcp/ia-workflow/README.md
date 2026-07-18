# IA MCP

Servidor MCP local, generico y sin RAG para operar una carpeta `/ia` como contexto estructurado para LLMs.

## Enfoque

Este servidor esta pensado para agentes LLM, no para una API humana tradicional. La fuente de verdad sigue siendo Markdown en Git. El MCP enruta, lee, busca, valida y ejecuta escrituras seguras por workflow.

Expone tres superficies:

| Superficie | Uso |
|---|---|
| `tools` | Acciones model-controlled para que el LLM pida contexto por intencion, busque texto, valide `/ia` y opere workflows seguros. |
| `resources` | Archivos Markdown de `/ia` con URI `ia:///...`, legibles como contexto explicito. |
| `prompts` | Flujos guiados para planificar, implementar, revisar, depurar y cerrar sesion. |

No usa embeddings, base vectorial ni RAG.

## Workflow publico

El servidor expone una fachada publica para usuarios no tecnicos y mantiene primitivas `ia_*` para agentes avanzados:

| Accion publica | Proposito |
|---|---|
| `create_task` | Crear una TASK en `Borrador` con contrato completo. |
| `migrate_task` | Normalizar una TASK `Borrador` creada con formato legado antes de aprobarla. |
| `approve_task` | Validar campos obligatorios y mover a `Lista`. |
| `work_task` | Trabajar solo una tarea `Lista`, con rechazo seguro para borradores o riesgo alto sin aprobacion. |
| `finish_task` | Cerrar o mover a revisión; al completar sincroniza `03`, `04` y `05`. |
| `close_issue` | Resolver y archivar un issue sincronizando `05` y `07`. |

Reglas:

* `create_task` crea tareas en `Borrador`.
* `migrate_task` normaliza una tarea heredada en preview antes de aprobarla.
* `approve_task` valida contrato y mueve a `Lista`.
* `work_task` rechaza tareas en `Borrador`, `Bloqueada`, `En revision` o `Completada`.
* Si `Riesgo: alto`, `work_task` exige `Aprobacion: aprobada`.
* Las escrituras siguen usando preview por defecto y solo aplican con `apply: true`.

## Ahorro de tokens

Las tools de lectura soportan respuestas compactas:

| Opcion | Uso |
|---|---|
| `mode: "pathsOnly"` | Devuelve rutas/URIs sin contenido. |
| `mode: "summary"` | Devuelve titulo, headings, bullets y tablas principales. |
| `mode: "full"` | Devuelve texto, respetando `maxChars`. |
| `maxChars` | Limita caracteres por archivo. |
| `includeText: false` | Evita texto completo en bundles de contexto. |

Patron recomendado para LLMs:

1. `ia_validate`
2. `ia_get_context` con `mode: "summary"` o `pathsOnly`
3. Leer solo archivos necesarios con `ia_read_file` o `ia_read_task`
4. Usar `ia_search` con `maxResults` bajo antes de pedir mas texto

La separacion sigue el modelo oficial MCP:

* `tools`: funciones que el modelo puede descubrir e invocar segun el contexto.
* `resources`: datos o archivos que el cliente puede incorporar como contexto para el LLM.
* `prompts`: plantillas de mensajes e instrucciones para flujos repetibles.

Referencias oficiales:

* https://modelcontextprotocol.io/docs/getting-started/intro
* https://modelcontextprotocol.io/specification/2025-06-18/server/tools
* https://modelcontextprotocol.io/specification/2025-06-18/server/resources
* https://modelcontextprotocol.io/specification/2025-06-18/server/prompts

## Ejecucion local

Desde la raiz de cualquier proyecto con carpeta `/ia`:

```bash
node .mcp/ia-workflow/server.mjs --project-root /ruta/al/proyecto
```

Tambien se puede apuntar directamente a la carpeta `/ia`:

```bash
node .mcp/ia-workflow/server.mjs --ia-root /ruta/al/proyecto/ia
```

Variables soportadas:

| Variable | Uso |
|---|---|
| `IA_MCP_PROJECT_ROOT` | Raiz del proyecto. El servidor usara `{root}/ia`. |
| `IA_MCP_IA_ROOT` | Ruta directa a la carpeta `/ia`. |

## Configuracion ejemplo

Ejemplo conceptual para un cliente MCP local:

```json
{
  "mcpServers": {
    "ia-workflow": {
      "command": "node",
      "args": [
        "/ruta/al/proyecto/.mcp/ia-workflow/server.mjs",
        "--project-root",
        "/ruta/al/proyecto"
      ]
    }
  }
}
```

## VS Code / GitHub Copilot

VS Code usa `.vscode/mcp.json`. En este repo esa configuracion queda versionable para que el equipo comparta el MCP local. `.gitignore` usa:

```gitignore
**/.vscode/
!/.vscode/
/.vscode/*
!/.vscode/mcp.json
!.mcp/ia-workflow/tests/
!.mcp/ia-workflow/tests/*.mjs
```

El ejemplo reutilizable para otros proyectos vive en:

```text
.mcp/ia-workflow/examples/vscode-mcp.json
```

Para probarlo en VS Code:

1. Abrir workspace con `.vscode/mcp.json` versionado.
2. En Copilot Chat, habilitar Agent Mode y seleccionar/permitir las tools del servidor `iaWorkflow`.
3. Pedir algo como: `Usa el MCP iaWorkflow, llama ia_validate y luego ia_get_context con intent=planificar y mode=summary`.

## Codex

Para que Codex cargue este servidor, agregar al archivo local `~/.codex/config.toml`:

```toml
[mcp_servers.ia_workflow]
command = "node"
args = [
  "/ruta/al/proyecto/.mcp/ia-workflow/server.mjs",
  "--project-root",
  "/ruta/al/proyecto"
]
startup_timeout_sec = 30
```

Despues de guardar la configuracion, reiniciar Codex o abrir una sesion nueva. Los MCP servers se descubren al iniciar la sesion; una sesion ya abierta no recibe automaticamente tools nuevas.

Prompt de prueba recomendado:

```text
Usa el MCP ia_workflow. Primero llama ia_validate. Luego llama ia_get_context con intent=planificar, mode=summary e includeText=false.
```

## Estructura interna

```text
.mcp/ia-workflow/
  server.mjs               # entrypoint genérico; no cambia al agregar tools
  tools/                   # un archivo por tool: schema, handler y smoke co-ubicado
  examples/
    vscode-mcp.json        # ejemplo versionable para .vscode/mcp.json
    codex-config.toml      # ejemplo para ~/.codex/config.toml
  tests/
    smoke.mjs              # smoke test JSON-RPC newline-delimited
  src/
    constants.mjs          # version, contrato /ia e intenciones
    registry.mjs           # autodescubrimiento y validación de tools/
    protocol.mjs           # transporte stdio: JSON-RPC newline-delimited
    common.mjs             # validacion de argumentos y utilidades base
    fs.mjs                 # acceso seguro limitado a /ia
    markdown.mjs           # resumen, inserciones Markdown y diffs
    prompts.mjs            # prompts MCP
    read-tools.mjs         # operaciones de lectura compartidas
    runtime.mjs            # contexto confinado del servidor
    secrets.mjs            # escaneo basico de secrets en Markdown
    time.mjs               # fechas Costa Rica
    write-tools.mjs        # operaciones declarativas preview/apply
```

`initialize` negocia `protocolVersion` contra una lista soportada. Si el cliente pide una version conocida, la respuesta la conserva; si pide una desconocida, responde la version soportada mas nueva.

## Tools principales

Fachada publica recomendada:

| Tool | Proposito |
|---|---|
| `create_task` | Crea una TASK en `Borrador` con contrato completo y preview por defecto. |
| `approve_task` | Valida y aprueba una TASK para moverla a `Lista`. |
| `work_task` | Verifica gates y devuelve contexto de trabajo para una TASK aprobada. |
| `finish_task` | Cierra como `Completada` o deja `En revisión`; al cerrar actualiza `03_plan.md`, `04_tasks/` y `05_progress/`. |
| `close_issue` | Cierra un issue, lo mueve al archivo mensual de `07`, limpia activos y registra el resultado en `05`. |

Primitivas avanzadas:

| Tool | Proposito |
|---|---|
| `ia_get_context` | Devuelve el bundle correcto segun `intent`: `planificar`, `implementar`, `revisar`, `depurar`, `cerrar_sesion`. |
| `ia_list_tasks` | Lista tareas activas, backlog, bloqueadas, completadas o todas. |
| `ia_read_task` | Lee una TASK activa por ID. |
| `ia_list_decisions` | Lista ADRs individuales. |
| `ia_read_decision` | Lee un ADR por ID. |
| `ia_list_issues` | Lista issues activos. |
| `ia_search` | Busqueda textual local en Markdown. |
| `ia_validate` | Valida archivos y carpetas obligatorias del contrato `/ia`. |
| `ia_read_file` | Lee un archivo puntual dentro de `/ia`. |

## Tools de escritura segura

La V2 agrega escritura declarativa. No existe `ia_write_file` ni escritura raw: el MCP escribe procesos, no archivos.

| Tool | Proposito |
|---|---|
| `ia_preview_operation` | Genera preview/diff estructurado sin aplicar cambios. |
| `ia_create_task` | Primitiva interna compatible con `create_task`: crea TASK desde template y actualiza `04_tasks/current.md`. |
| `ia_close_task` | Cierra TASK activa: `03` plan, `04` current/done y `05` current/componente. |
| `ia_add_progress_entry` | Agrega una entrada de progreso actual y opcionalmente por componente. |
| `ia_create_issue` | Crea ISSUE abierto y actualiza `07_issues/current.md`. |
| `ia_close_issue` | Cierra ISSUE abierto: actualiza `05`, `07/current`, `07/archive` y elimina el archivo activo. |
| `ia_create_decision` | Crea ADR individual y actualiza `06_decisions.md`. |

Todas las tools de escritura usan `apply: false` por defecto. Para aplicar, repetir la misma llamada con:

```json
{
  "apply": true
}
```

Antes de aplicar, cada operacion valida rutas permitidas dentro de `/ia`, ausencia basica de secrets, idioma espanol aproximado y estructura general.

## Ejemplo de flujo LLM

```text
1. Llama ia_get_context con intent=planificar y mode=summary.
2. Si debes crear una tarea, llama create_task con apply=false.
3. Muestra el preview al usuario.
4. Solo si el usuario confirma, llama create_task con los mismos argumentos y apply=true.
5. Llama ia_validate.
```

## Prompts

| Prompt | Uso |
|---|---|
| `create_task` | Convertir una solicitud en TASK Borrador. |
| `approve_task` | Aprobar una TASK Borrador validando su contrato. |
| `work_task` | Validar gates y preparar contexto de una TASK Lista. |
| `finish_task` | Cerrar o mover a revision una TASK trabajada. |
| `close_issue` | Resolver un issue y sincronizar progreso e historial. |
| `ia_planificar_sesion` | Iniciar sesion leyendo solo contexto necesario. |
| `ia_implementar_tarea` | Implementar una TASK existente. |
| `ia_revisar_cambios` | Revisar cambios contra arquitectura, ADRs e issues. |
| `ia_depurar_issue` | Investigar un issue documentado. |
| `ia_cerrar_sesion` | Cerrar sesion actualizando `/ia`. |

## Reglas

* Las escrituras son declarativas y requieren `apply: true`.
* No lee fuera de `/ia`.
* No guarda memoria oculta.
* No indexa semanticamente.
* No ofrece escritura raw de Markdown.
* En `stdio`, stdout emite solo mensajes MCP JSON-RPC delimitados por newline. Los logs deben ir a stderr.

## Validacion

Antes de considerar listo el MCP:

```bash
node --check .mcp/ia-workflow/server.mjs
find .mcp/ia-workflow/src .mcp/ia-workflow/tools -name '*.mjs' -exec node --check {} \;
node .mcp/ia-workflow/tests/smoke.mjs
```

El smoke deriva el catálogo desde `tools/` y ejecuta el `smoke()` co-ubicado de cada tool. En una copia temporal verifica el cierre físico de tareas en `03/04/05`, el cierre de issues en `05/07`, previews seguros y rechazo de path traversal.
