# MCP Local para VS Code y Codex

Esta referencia se mantiene en español. El contenido que el MCP genere dentro de `/ia` debe usar
el idioma del proyecto.

Usar esta referencia cuando un proyecto con `/ia` quiere que los clientes LLM locales, especialmente VS Code/GitHub Copilot y Codex, operen el workflow a través de MCP en vez de leer repetidamente archivos Markdown grandes.

Este asset es opcional. `/ia` debe seguir funcionando sin MCP pidiendo al LLM que empiece desde `ia/README.md`.

## Intención

Crear un servidor MCP local, propiedad del proyecto, que exponga `/ia` como un workflow LLM estructurado:

* **Tools** para acciones controladas por el modelo: leer contexto por intención, listar/leer tareas, listar/leer ADRs, buscar en Markdown, validar estructura y ejecutar escrituras seguras del workflow.
* **Resources** para contexto Markdown explícito con URIs como `ia:///00_context.md`.
* **Prompts** para workflows repetibles: planificar, implementar, revisar, depurar y cerrar sesión.

Para usuarios no técnicos, exponer un pequeño vocabulario de workflow público y tratar las tools de bajo nivel como bloques internos. El usuario debe poder decir "crea una tarea", "aprueba la tarea", "trabaja en la tarea" o "cierra la tarea" sin conocer nombres de tools como `ia_get_context` o `ia_validate`.

No agregar RAG por defecto. La fuente de verdad es Markdown en Git. El MCP debe reducir el uso de tokens enrutando y compactando el contexto, no creando una capa de memoria oculta.

## Carpeta recomendada

Colocar el servidor fuera de `/ia` para que `/ia` permanezca como conocimiento puro del proyecto:

```text
.mcp/
  ia-workflow/
    server.mjs
    README.md
    examples/
      vscode-mcp.json
      codex-config.toml
    tests/
      smoke.mjs
    src/
      constants.mjs
      definitions.mjs
      protocol.mjs
      common.mjs
      fs.mjs
      markdown.mjs
      secrets.mjs
      time.mjs
      write-tools.mjs
```

Usar otro runtime solo cuando el proyecto tenga una razón sólida. MCP no requiere Node, pero un servidor `.mjs` sí.

## Requisitos del servidor

El servidor debe:

* Usar transporte MCP `stdio` para que VS Code, GitHub Copilot y Codex puedan lanzarlo como proceso local.
* Implementar `stdio` como JSON-RPC delimitado por saltos de línea: un mensaje JSON-RPC válido por línea en `stdout`/`stdin`, sin saltos de línea embebidos en el marco del mensaje.
* Nunca usar encuadrado `Content-Length` como protocolo primario MCP `stdio` para VS Code. La compatibilidad legada opcional está bien, pero VS Code debe recibir JSON-RPC delimitado por saltos de línea.
* Enviar logs solo a `stderr`; `stdout` debe contener solo mensajes MCP válidos.
* Negociar `protocolVersion` en `initialize`: mantener una lista explícita de versiones de especificación soportadas, devolver la versión solicitada solo cuando está en esa lista, y de lo contrario responder con la más nueva soportada. Nunca devolver ciegamente lo que el cliente solicita — la especificación MCP evoluciona y declarar una versión no implementada causa fallos sutiles en el cliente.
* Exponer los tres primitivos MCP cuando sea útil: VS Code soporta tools, prompts (mostrados como comandos slash `/mcp.{servidor}.{prompt}`) y resources ("Add Context"), no solo tools.
* Aceptar `--project-root /ruta/al/proyecto` o `--ia-root /ruta/al/proyecto/ia`.
* Nunca leer fuera de la raíz `/ia` configurada para los resources del workflow.
* Exponer modos de lectura compactos: `pathsOnly`, `summary` y `full`.
* Soportar `maxChars` e `includeText: false` para control de tokens.
* Proveer `ia_validate` para verificar el contrato requerido de `/ia`.
* Proveer solo escrituras seguras del workflow, no escrituras crudas de archivos.
* Por defecto todas las tools de escritura en modo preview, y requerir `apply: true` para mutar archivos.
* Validar rutas, estructura Markdown y posibles secretos antes de aplicar escrituras.
* Mantener todo el contenido generado de `/ia` en el idioma del proyecto.
* Aplicar gates del ciclo de vida de tareas: `Borrador` no puede implementarse; solo `Lista` puede pasar a trabajo; las tareas de riesgo alto necesitan aprobación explícita.
* Soportar `work_task` con `transition: "blocked"` y `transition: "resumed"`, siempre con `reason`; bloquear mueve a `Bloqueada` y reanudar devuelve a `En progreso` sin reiniciar `startedAt`.
* Exponer `return_task_to_draft` para devolver una tarea `Lista`, `En progreso` o `Bloqueada` a `Borrador`. Debe requerir `reason`, previsualizar por defecto, exigir `apply: true`, conservar el historial V2 mediante `returned_to_draft`, limpiar aprobación e inicio vigentes y sincronizar `current.md` y `blocked.md`.
* Tratar las secciones operativas vacías de `04_tasks/current.md` como contenido estructurado: usar `Sin tareas registradas.` como representación canónica, aceptar también contenido vacío heredado y restaurar el marcador tras retirar la última entrada. Los helpers de inserción no pueden depender de una cantidad específica de saltos de línea.
* Resolver una tarea por ID primero desde `04_tasks/tasks/{id}.md` y, si no existe, desde secciones exactas de todos los archivos de `04_tasks/done/`, recorriéndolos del mes más reciente al más antiguo. Las respuestas históricas deben incluir `archived: true` y la ruta del archivo mensual de origen.

## Schema de parámetros por variante

Una tool que multiplexa acciones, por ejemplo `ia_inspect` con `action: "list_tasks"` o
`action: "search"`, debe publicar un contrato distinto por cada acción. No declarar una unión
plana de todos los parámetros como opcionales cuando el handler rechaza campos ajenos a la acción.
Esa divergencia permite que el cliente construya llamadas que el propio MCP considera inválidas.

Usar un schema `oneOf` con una variante cerrada por discriminante:

* Cada variante incluye una propiedad discriminante con `const` (por ejemplo, `action` u `outcome`) y solo las propiedades aceptadas por esa variante
* Cada variante declara `additionalProperties: false`
* Los parámetros obligatorios se declaran en el `required` de su variante
* Una única definición de variantes debe generar tanto las variantes del schema como la lista de
  campos permitidos en el handler
* El validador de protocolo identifica la variante por cualquier propiedad con `const` que coincida
  con el argumento; no puede asumir un nombre fijo como `action`
* Todo parámetro con `type: "array"` declara `items` con el schema de sus elementos. Los clientes
  de VS Code rechazan el catálogo cuando un array no define sus elementos.

No mantener por separado un schema con todos los parámetros y una lista de campos permitidos por
variante. Las dos estructuras se desincronizan con facilidad. Si el catálogo crece, actualizar el
presupuesto del smoke test de forma proporcional al número de tools, sin eliminar el cierre de las
variantes para ahorrar caracteres.

## Tools recomendadas

El servidor actual autodetecta un catálogo consolidado de 15 tools. Las acciones públicas se
documentan primero; las primitivas avanzadas y las escrituras internas se usan para composición,
diagnóstico y mantenimiento del workflow.

### Fachada pública

| Tool o prompt | Propósito |
|---|---|
| `create_task` | Crear una tarea en `Borrador`; usa el contrato completo del MCP y `apply: false` por defecto. |
| `approve_task` | Validar una tarea y moverla de `Borrador` a `Lista`. |
| `work_task` | Iniciar una tarea aprobada o registrar una transición `blocked`/`resumed` con `reason`. |
| `return_task_to_draft` | Devolver una tarea activa a `Borrador` sin simular un cierre o duplicidad. |
| `finish_task` | Cerrar una tarea como `Completada` y sincronizar plan, cola, historial y progreso. |
| `duplicate_task` | Archivar una tarea como duplicada desde cualquier estado, validando la referencia y el motivo. |
| `delete_task` | Eliminar permanentemente una tarea con rastro de auditoría en `done/`. Usar cuando la tarea ya no se va a trabajar y no corresponde marcarla como completada. Requiere `id` y `reason`. |
| `close_issue` | Resolver un issue abierto, sincronizar `05` y `07` y moverlo al archivo mensual. |

### Bloques avanzados

Tools de lectura:

| Tool | Propósito |
|---|---|
| `ia_get_context` | Devolver el bundle mínimo según la intención `planificar`, `implementar`, `revisar`, `depurar` o `cerrar_sesion`. |
| `ia_validate` | Validar la estructura de `/ia` y emitir advertencias de contrato y tamaño. |
| `ia_inspect` | Concentrar las lecturas de tareas, ADRs, issues, archivos, búsquedas, vínculos DevOps, migración histórica y métricas. |

`ia_inspect` publica variantes cerradas por `action`; no hay una tool de lectura separada por
recurso:

| Acción | Uso principal |
|---|---|
| `list_tasks` | Listar tareas por `status`, `mode` y usuario opcional. |
| `read_task` | Leer una tarea activa o archivada por `id`. |
| `list_decisions` / `read_decision` | Listar o leer ADRs. |
| `list_issues` | Listar issues con modo y texto opcionales. |
| `list_pending_devops_link` | Obtener IDs pendientes de vínculo con DevOps. |
| `read_file` | Leer un Markdown confinado a `/ia`. |
| `search` | Buscar texto en los ámbitos permitidos de `/ia`. |
| `metrics` | Calcular métricas temporales y, opcionalmente, forecast. |
| `migration` | Clasificar timestamps históricos en modo preview; no escribe cambios. |

No forman parte del catálogo vigente las antiguas tools `ia_list_*`, `ia_read_*`, `ia_search`,
`ia_preview_operation`, `ia_create_task` o `ia_close_task`. Las lecturas se realizan mediante
`ia_inspect` y las escrituras se previsualizan con `apply: false` en la tool correspondiente.

Tools de escritura segura:

| Tool | Propósito |
|---|---|
| `ia_add_progress_entry` | Agregar entradas de progreso a current y archivos de componente opcionales. |
| `ia_create_issue` | Crear un issue abierto y actualizar el índice de issues. |
| `ia_create_decision` | Crear un archivo ADR y actualizar el índice de ADRs. |
| `archive_progress` | Archivar entradas antiguas de `05_progress/current.md`; es idempotente y usa preview por defecto. |

Evitar una tool genérica `ia_write_file`. El MCP debe escribir workflows, no Markdown arbitrario.

Si se exponen tanto tools de workflow público como tools internas `ia_*`, documentar primero las tools públicas y decirle a los usuarios que las prefieran.

## Métricas y forecast opcional

`ia_inspect` con `action: "metrics"` es una lectura opcional para estimar un lote de tareas a
partir de eventos históricos verificables. No agrega campos obligatorios a las tareas, no requiere
story points y no crea otra tool ni persistencia externa.

Ejemplo mínimo:

```json
{
  "action": "metrics",
  "filter": "workstream=MCP",
  "targetCount": 4,
  "seed": 7
}
```

`targetCount` representa cuántas tareas futuras se quieren estimar; no es un dato que deba guardarse
en cada tarea. El resultado incluye métricas de flujo y, cuando hay muestra suficiente, percentiles
empíricos y Monte Carlo P50/P85/P95. `seed` permite repetir el cálculo. Se pueden usar `from` y
`asOf` para acotar el periodo y la fecha de corte.

El forecast usa únicamente eventos temporales V2 con precisión suficiente. Si no hay muestra
suficiente devuelve `insufficient_data` en lugar de inventar una fecha. La llamada es read-only y
el uso de forecast es opcional: primero se solicita el contexto compacto; solo se consulta metrics
cuando el agente necesita estimar un lote o revisar el flujo.

## VS Code / GitHub Copilot

VS Code descubre servidores MCP desde `.vscode/mcp.json`. La documentación oficial de VS Code recomienda **versionar la config MCP del workspace para que el equipo la comparta**. Cuando el repositorio ignore `.vscode/`, no conformarse con un ejemplo para copiar: cambiar la regla de ignorado a un glob más una negación, porque Git no puede re-incluir archivos dentro de un directorio excluido:

```gitignore
.vscode/*
!.vscode/mcp.json
```

Mantener el ejemplo versionable bajo `.mcp/ia-workflow/examples/vscode-mcp.json` como referencia reutilizable para otros proyectos.

Ejemplo de `.vscode/mcp.json`:

```json
{
  "servers": {
    "iaWorkflow": {
      "type": "stdio",
      "command": "node",
      "args": [
        "${workspaceFolder}/.mcp/ia-workflow/server.mjs",
        "--project-root",
        "${workspaceFolder}"
      ],
      "dev": {
        "watch": ".mcp/ia-workflow/**/*.mjs",
        "debug": { "type": "node" }
      }
    }
  }
}
```

`dev.watch` reinicia el servidor cuando cambian sus fuentes; `dev.debug` permite a VS Code adjuntar un debugger Node — ambos vale la pena activarlos para un servidor hecho a mano. No poner secretos en este archivo; si un servidor necesita credenciales, usar la sección `inputs` con `promptString`/`password: true`. El sandbox de MCP de VS Code (`sandboxEnabled`) es solo para macOS/Linux — en Windows el servidor mismo debe confinar lecturas/escrituras (ej. solo archivos `.md` bajo `/ia`).

Prompt de prueba recomendado para Copilot Chat en Agent Mode:

```text
Usa el MCP iaWorkflow. Primero llama ia_validate. Luego llama ia_get_context con intent=planificar, mode=summary e includeText=false.
```

Si el cliente pide permiso para usar tools, permitir las tools `/ia` necesarias para el workflow. No pegar secretos en prompts ni en la config de MCP.

## Codex

Codex carga los servidores MCP al inicio de sesión. Después de editar la config local, reiniciar Codex o abrir una sesión nueva.

Ejemplo de entrada en `~/.codex/config.toml`:

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

Prompt de prueba recomendado:

```text
Usa el MCP ia_workflow. Primero llama ia_validate. Luego llama ia_get_context con intent=planificar, mode=summary e includeText=false.
```

Si el MCP está presente en `~/.codex/config.toml` pero no está expuesto como tool nativa en la sesión actual de Codex, no simular una llamada nativa. O pedir al usuario que abra una nueva sesión, o ejecutar el servidor directamente sobre MCP `stdio` para un smoke test y explicar que el descubrimiento nativo solo ocurre al inicio de sesión.

## Smoke Tests

Ejecutar esto antes de considerar el MCP listo:

```bash
node --check .mcp/ia-workflow/server.mjs
find .mcp/ia-workflow/src -name '*.mjs' -exec node --check {} \;
```

Luego ejecutar el smoke test con script (`node .mcp/ia-workflow/tests/smoke.mjs`), que debe verificar:

* `initialize` devuelve el nombre y versión esperados del servidor.
* Negociación de versión de protocolo: un `protocolVersion` soportado se devuelve; uno desconocido recibe la versión más nueva soportada, nunca un eco ciego.
* El smoke test envía JSON-RPC delimitado por saltos de línea, coincidiendo con el transporte `stdio` de VS Code.
* `tools/list` incluye las tools de lectura y escritura segura.
* Todos los parámetros publicados con `type: "array"` incluyen `items`; este check debe recorrer
  recursivamente `properties`, `items` y las variantes `oneOf` del catálogo.
* Para cada tool con variantes mutuamente excluyentes, `tools/list` publica una variante `oneOf`
  cerrada por su discriminante, sin parámetros anunciados que el handler rechace.
* `ia_validate` devuelve `valid: true`.
* `ia_validate` emite advertencias de tamaño cuando `00_context.md` supera 20 000 chars, `01_requirements.md` supera 24 000 chars, `02_architecture.md` supera 24 000 chars o `03_plan.md` supera 20 000 chars; cada warning debe incluir el conteo actual y la acción correctiva.
* `ia_get_context` devuelve archivos para una solicitud de planificación compacta, por ejemplo `intent: "planificar"`, `mode: "summary"` e `includeText: false`.
* La lectura de tareas resuelve una tarea archivada y, cuando el mismo ID existe en dos meses sintéticos, devuelve la sección del mes más reciente con `archived: true`.
* Al menos una tool de escritura funciona en modo preview.
* `duplicate_task` funciona desde `Borrador` en una copia desechable, no muta durante el preview, marca el checklist, registra la duplicidad y archiva la tarea al aplicar `apply: true`.
* El path traversal es rechazado (ej. `ia_inspect` con `action: "read_file"` y `../` devuelve un error).
* Una llamada con un parámetro de otra acción es rechazada por el contrato antes de ejecutar el
  handler.
* Si es seguro hacerlo, el ciclo `create_task` → `approve_task` → `work_task` → `finish_task`
  funciona con `apply: true` en una copia desechable de `/ia`, incluyendo un `current.md` con la
  tabla de borradores canónica y las secciones `Lista` y `En progreso` inicialmente vacías; nunca
  contra la copia de trabajo. Debe comprobar tanto el marcador `Sin tareas registradas.` como una
  representación heredada compuesta solo por saltos de línea.
* `archive_progress keepDays=9999` (preview) devuelve `changes: []` sin error; `keepDays=0 apply=true` reduce `current.md` y la segunda ejecución devuelve `changes: []` (idempotencia).

## Checklist de documentación

El README del MCP debe incluir:

* Objetivo: MCP local LLM-first para `/ia`, sin RAG por defecto.
* Cómo se mapean tools, resources y prompts a las superficies de MCP.
* Patrón de ahorro de tokens: validar, solicitar contexto compacto, luego leer solo los archivos seleccionados.
* Comandos de ejecución local con `--project-root` y `--ia-root`.
* Config de VS Code/GitHub Copilot.
* Config de Codex.
* Política de escritura segura: primero preview, aplicar solo con `apply: true` explícito.
* Comandos de validación y expectativas del smoke test.
* URLs de documentación oficial usadas para conceptos MCP cuando estén disponibles.

## Reglas de auditoría

Al auditar un MCP existente para `/ia`, marcar estos como gaps importantes:

* El servidor vive en una carpeta no clara como `tools/` cuando el proyecto ya usa `.mcp/`.
* `.vscode/mcp.json` está en el gitignore sin la excepción `.vscode/*` + `!.vscode/mcp.json` — el equipo no puede compartir la config MCP del workspace, contradiciendo la recomendación oficial de VS Code.
* El servidor lee fuera de `/ia` sin una razón documentada.
* Las tools de escritura mutan archivos por defecto.
* El catálogo serializado (`JSON.stringify(tools).length`) supera ~625 chars por tool; acortar descripciones y schemas, y actualizar el presupuesto en el smoke test proporcional al número de tools. No incluir `description` en propiedades del `inputSchema` cuando el nombre del parámetro sea autoexplicativo.
* `05_progress/current.md` supera 12 000 caracteres y no existe la tool `archive_progress` ni un mecanismo equivalente de archivado — registrarlo como gap.
* `04_tasks/current.md` acumula más de 5 líneas `> **Completado` en el header porque `buildCloseTaskChanges` no limpia las antiguas — registrarlo como gap importante; la corrección es agregar `trimCompletedHeaderLines` (ver sección siguiente).
* `04_tasks/current.md` contiene un segundo encabezado `# 04 —` o una tabla `## Cola activa` legada junto con las secciones operativas nuevas — registrarlo como gap bloqueante y ejecutar la limpieza descrita en la sección siguiente.
* `approve_task`, `work_task` o `finish_task` fallan con una sección operativa vacía porque el helper solo reconoce saltos de línea específicos — registrarlo como gap bloqueante; normalizar el marcador y hacer el helper tolerante a contenido vacío heredado.

## Convenciones de write-tools y markdown

### trimCompletedHeaderLines

Sin limpieza activa, el header de `04_tasks/current.md` acumula una línea `> **Completado` por cada tarea cerrada, desperdiciando ~1 000 tokens por lectura en sesiones largas.

**Implementar en `src/markdown.mjs`:**

```js
/**
 * Keeps only the last `maxLines` `> **Completado` lines in the document header,
 * removing older completed-task lines and any other stale history blockquotes
 * (e.g. `> **Cerrado`, `> **Agregado`, `> **Urgencia`).
 * The `> **Última actualización:` line is always preserved.
 */
export function trimCompletedHeaderLines(text, maxLines = 5) {
  if (!text) return text;
  const lines = text.split(/\r?\n/);

  // All "history" blockquote lines: starts with `> **` but NOT the standard last-updated line
  const historyIndices = [];
  for (let i = 0; i < lines.length; i++) {
    if (/^> \*\*/.test(lines[i]) && !/^> \*\*Última actualización:/.test(lines[i])) {
      historyIndices.push(i);
    }
  }

  // Subset that are completion lines
  const completedIndices = historyIndices.filter((i) => /^> \*\*Completado/.test(lines[i]));

  // Determine which completed lines to keep (last maxLines)
  const keepSet = new Set(completedIndices.slice(-maxLines));

  // Remove all history lines not in keepSet
  const removeSet = new Set(historyIndices.filter((i) => !keepSet.has(i)));

  if (removeSet.size === 0) return text;

  return lines.filter((_, i) => !removeSet.has(i)).join("\n");
}
```

**Llamarla en `buildCloseTaskChanges` de `src/write-tools.mjs`**, envolviendo la cadena existente:

```js
const updatedCurrent = trimCompletedHeaderLines(
  updateLastUpdatedLine(
    removeTaskRows(currentText, id),
    `${todayCrDate()} CR (${id} completada)`
  )
);
```

Recordar exportar la función desde `markdown.mjs` e incluirla en el import de `write-tools.mjs`.

### Limpieza de estructura legacy en current.md

Si `04_tasks/current.md` contiene estructura duplicada (segundo encabezado `# 04 —`, tabla `## Cola activa`, blockquotes `> **Última actualización:` sueltos), eliminarla manualmente. La estructura canónica es:

```
# 04 — Tareas activas

> **Última actualización:** {fecha} CR ({referencia})
> **Completado {fecha}:** {resumen}    ← máximo 5 líneas

## Reglas para agentes

{reglas}

## En progreso
## Lista
## Borradores
## Bloqueadas
```

