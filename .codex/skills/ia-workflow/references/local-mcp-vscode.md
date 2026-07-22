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
* Resolver una tarea por ID primero desde `04_tasks/tasks/{id}.md` y, si no existe, desde secciones exactas de todos los archivos de `04_tasks/done/`, recorriéndolos del mes más reciente al más antiguo. Las respuestas históricas deben incluir `archived: true` y la ruta del archivo mensual de origen.

## Schema de parámetros por acción

Una tool que multiplexa acciones, por ejemplo `ia_inspect` con `action: "list_tasks"` o
`action: "search"`, debe publicar un contrato distinto por cada acción. No declarar una unión
plana de todos los parámetros como opcionales cuando el handler rechaza campos ajenos a la acción.
Esa divergencia permite que el cliente construya llamadas que el propio MCP considera inválidas.

Usar un schema `oneOf` con una variante cerrada por acción:

* Cada variante incluye `action` con `const` y solo las propiedades aceptadas por esa acción
* Cada variante declara `additionalProperties: false`
* Los parámetros obligatorios se declaran en el `required` de su variante
* Una única definición de acciones debe generar tanto las variantes del schema como la lista de
  campos permitidos en el handler
* El validador de protocolo resuelve la variante correspondiente antes de validar propiedades,
  obligatorios y enumeraciones

No mantener por separado un schema con todos los parámetros y una lista de campos permitidos por
acción. Las dos estructuras se desincronizan con facilidad. Si el catálogo crece, actualizar el
presupuesto del smoke test de forma proporcional al número de tools, sin eliminar el cierre de las
variantes para ahorrar caracteres.

## Tools recomendadas

Tools o prompts de workflow público:

| Tool o prompt | Propósito |
|---|---|
| `create_task` | Crear una tarea en `Borrador` con objetivo, alcance, exclusiones, criterios de aceptación, riesgo, archivos probables, plan técnico, validación, rollback y checklist. |
| `approve_task` | Validar el contrato de tarea y moverla a `Lista`. |
| `work_task` | Ejecutar una tarea aprobada: validar `/ia`, leer contexto/ADRs/archivos, planificar, implementar, validar y registrar progreso. |
| `finish_task` | Cerrar o mover a revisión: actualizar tarea, progreso, archivos cambiados, trabajo pendiente, riesgos, docs y `03_plan.md` si el ID de tarea aparece en el plan de fases. |

Las siguientes tools son útiles internamente y pueden permanecer invocables por agentes avanzados:

Tools de lectura:

| Tool | Propósito |
|---|---|
| `ia_get_context` | Devolver el bundle mínimo para `planificar`, `implementar`, `revisar`, `depurar` o `cerrar_sesion`. |
| `ia_list_tasks` | Listar tareas activas, bloqueadas, backlog o completadas. |
| `ia_read_task` | Leer una tarea por ID: activa o archivada; para historial, prioriza el mes más reciente e incluye `archived: true` y la ruta de origen. |
| `ia_list_decisions` | Listar archivos ADR. |
| `ia_read_decision` | Leer un ADR por ID. |
| `ia_list_issues` | Listar issues activos. |
| `ia_search` | Buscar en Markdown localmente antes de leer más contexto. |
| `ia_validate` | Validar la estructura de `/ia` y emitir advertencias cuando `00_context.md` supera 20 000 chars, `01_requirements.md` supera 24 000 chars, `02_architecture.md` supera 24 000 chars o `03_plan.md` supera 20 000 chars. Cada warning incluye el conteo actual y la acción correctiva. |
| `ia_read_file` | Leer un archivo Markdown específico dentro de `/ia`. |

Tools de escritura segura:

| Tool | Propósito |
|---|---|
| `ia_preview_operation` | Previsualizar una mutación del workflow y su diff sin aplicarlo. |
| `ia_create_task` | Primitivo interno detrás de `create_task`; crear una tarea desde el template del proyecto y actualizar `04_tasks/current.md` cuando el workflow lo aprueba. |
| `ia_close_task` | Cerrar una tarea activa, actualizar progreso, agregar al historial mensual de completadas, eliminar el archivo individual de tarea y actualizar `03_plan.md` si el ID aparece en el plan de fases. |
| `ia_add_progress_entry` | Agregar entradas de progreso a current y archivos de componente opcionales. |
| `ia_create_issue` | Crear un issue abierto y actualizar el índice de issues. |
| `ia_create_decision` | Crear un archivo ADR y actualizar el índice de ADRs. |
| `archive_progress` | Mover entradas antiguas de `## Completado en sesiones recientes` en `05_progress/current.md` a archivos mensuales de `05_progress/archive/`; `keepDays` configurable (default 7), idempotente, preview por defecto. Usar cuando `ia_validate` advierta que `current.md` supera 12 000 caracteres. |

Evitar una tool genérica `ia_write_file`. El MCP debe escribir workflows, no Markdown arbitrario.

Si se exponen tanto tools de workflow público como tools internas `ia_*`, documentar primero las tools públicas y decirle a los usuarios que las prefieran. Las tools de bajo nivel siguen siendo útiles para debugging, smoke tests y composición avanzada de agentes.

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
* Para cada tool con acciones mutuamente excluyentes, `tools/list` publica una variante `oneOf`
  cerrada por acción, sin parámetros anunciados que el handler rechace.
* `ia_validate` devuelve `valid: true`.
* `ia_validate` emite advertencias de tamaño cuando `00_context.md` supera 20 000 chars, `01_requirements.md` supera 24 000 chars, `02_architecture.md` supera 24 000 chars o `03_plan.md` supera 20 000 chars; cada warning debe incluir el conteo actual y la acción correctiva.
* `ia_get_context` devuelve archivos para una solicitud de planificación compacta, por ejemplo `intent: "planificar"`, `mode: "summary"` e `includeText: false`.
* La lectura de tareas resuelve una tarea archivada y, cuando el mismo ID existe en dos meses sintéticos, devuelve la sección del mes más reciente con `archived: true`.
* Al menos una tool de escritura funciona en modo preview.
* El path traversal es rechazado (ej. `ia_read_file` con `../` devuelve un error).
* Una llamada con un parámetro de otra acción es rechazada por el contrato antes de ejecutar el
  handler.
* Si es seguro hacerlo, un workflow de escritura funciona con `apply: true` en una copia desechable de `/ia` — nunca contra la copia de trabajo.
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
## En revisión
```

