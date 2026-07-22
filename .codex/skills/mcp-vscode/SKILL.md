---
name: mcp-vscode
description: >
  Crear y mantener servidores MCP locales compatibles con VS Code/GitHub Copilot, Claude Code
  y Codex usando Node.js (stdio, newline-delimited JSON-RPC). Arquitectura de carpeta tools/
  con un archivo por tool y autodescubrimiento: agregar un tool = crear un archivo, sin tocar
  el server. Cubre protocolo, diseño de tools (safe writes, TOON), configuración de clientes,
  smoke tests y checklist de auditoría. Usar cuando se cree, audite o extienda cualquier
  servidor MCP local.
  Triggers: mcp, local mcp, mcp server, mcp vscode, stdio mcp, json-rpc mcp, crear mcp,
  nuevo mcp, agregar tool mcp, nueva tool mcp, mcp node, mcp js, mcp para vscode, mcp copilot,
  mcp codex, mcp claude code, mcp protocol, content-length mcp, protocolVersion, tools/list,
  initialize mcp, mcp.json, vscode mcp, safe write tool, write tool mcp, smoke test mcp,
  auditar mcp, mcp debug, registry tools, autodescubrimiento tools.
version: 2.0.0
---

# MCP local para VS Code, Claude Code y Codex — Skill de Construcción (Node.js / stdio)

**WORKFLOW SKILL** — Guía completa para construir y mantener un servidor MCP local,
project-owned, con transporte `stdio` newline-delimited JSON-RPC, compatible con
VS Code/GitHub Copilot (Agent Mode), Claude Code y Codex.

La arquitectura está optimizada para que un LLM agregue tools sin fricción:
**1 tool = 1 archivo** en `tools/`, autodescubierto por el server. Schema, handler,
formato de salida y smoke test viven co-ubicados en ese único archivo.

## Usa Este Skill Cuando:

- ✅ Crear un servidor MCP local desde cero para un proyecto.
- ✅ **Agregar o editar un tool** en un MCP existente (el caso más frecuente).
- ✅ Auditar un servidor MCP contra los requisitos de VS Code/Claude Code/Codex.
- ✅ Depurar por qué un cliente no conecta (`initialize` sin respuesta) o un tool no aparece.
- ✅ Configurar `.vscode/mcp.json`, `.mcp.json` (Claude Code) o `~/.codex/config.toml`.
- ✅ Migrar un MCP de la estructura antigua (`definitions.mjs` + `HANDLERS`) a `tools/`.

## NO Uses Este Skill Para:

- ❌ MCP remotos sobre HTTP para Copilot Studio → usar [mcp-copilot-studio](../mcp-copilot-studio/SKILL.md).
- ❌ Usar Playwright MCP (eso es consumir un MCP, no construirlo) → usar [mcp-playwright](../mcp-playwright/SKILL.md).
- ❌ Crear skills de agente (SKILL.md) → usar [create-skill](../create-skill/SKILL.md).

---

## Índice de Referencia

| Archivo | Contenido |
|---------|-----------|
| [01-protocol.md](references/01-protocol.md) | Protocolo JSON-RPC sobre stdio: framing newline-delimited, negociación de `protocolVersion`, primitivos (tools/prompts/resources), logs a stderr |
| [02-estructura.md](references/02-estructura.md) | Estructura de carpetas con `tools/`, contrato del archivo de tool, registry de autodescubrimiento, server.mjs genérico, migración desde la estructura antigua |
| [03-agregar-tool.md](references/03-agregar-tool.md) | **Flujo de 1 archivo** para agregar un tool: plantilla, reglas de schema, patrones (CLI / REST / safe write), criterio TOON vs JSON |
| [04-clientes.md](references/04-clientes.md) | Configuración por cliente: `.vscode/mcp.json` (dev.watch, inputs, gitignore), `.mcp.json` de Claude Code, `config.toml` de Codex, verificación |
| [05-smoke-tests.md](references/05-smoke-tests.md) | Smoke test genérico que autodescubre tools + `smoke()` co-ubicado por tool, helpers, reglas de aserción |
| [06-checklist-errores.md](references/06-checklist-errores.md) | Checklist de auditoría y tabla de diagnóstico rápido |

Plantillas listas para copiar en [examples/](examples/):

| Archivo | Uso |
|---------|-----|
| [tool-template.mjs](examples/tool-template.mjs) | Plantilla anotada de un tool — copiar a `tools/{name}.mjs` y adaptar |
| [registry.mjs](examples/registry.mjs) | Implementación de referencia del autodescubrimiento (`src/registry.mjs`) |
| [server-skeleton.mjs](examples/server-skeleton.mjs) | `server.mjs` genérico que nunca se edita al agregar tools |
| [smoke-test.mjs](examples/smoke-test.mjs) | Runner de smoke test que deriva el catálogo de `tools/` y ejecuta el `smoke()` de cada tool |
| [vscode-mcp.json](examples/vscode-mcp.json) | Config `.vscode/mcp.json` con dev.watch y debug |

---

## Regla de Oro — Agregar un Tool

```text
1. Copiar examples/tool-template.mjs → .mcp/{servidor}/tools/{nombre_del_tool}.mjs
2. Completar name (= nombre del archivo), description, inputSchema, handler, smoke()
3. node --check tools/{nombre_del_tool}.mjs
4. node tests/smoke.mjs   → TODO OK
```

No se edita `server.mjs`, no hay `HANDLERS`, no hay `TOON_TOOLS`, no hay `definitions.mjs`.
El registry autodescubre todo archivo `tools/*.mjs` que no empiece con `_`.

---

## Puntos Críticos — Resumen Rápido

1. **Newline-delimited JSON-RPC** — un mensaje por línea en stdout/stdin. **Nunca
   `Content-Length` framing**: es la causa #1 de "VS Code espera `initialize` sin timeout".
2. **Logs solo a `stderr`** — stdout debe contener únicamente mensajes MCP válidos.
3. **No hacer echo ciego de `protocolVersion`** — si el cliente pide una versión no
   soportada, responder la más reciente soportada. Ver [01-protocol.md](references/01-protocol.md).
4. **1 tool = 1 archivo** en `tools/{name}.mjs` con `export default { name, description,
   inputSchema, handler, ... }`. El `name` DEBE ser igual al nombre del archivo sin `.mjs`.
5. **`server.mjs` no se toca al agregar tools** — el registry valida el contrato al cargar
   y falla rápido con el nombre del archivo problemático.
6. **`additionalProperties: false`** en todo `inputSchema` — evita props inventadas por el modelo.
7. **`ToolError`, no `Error`** — los errores de negocio se devuelven al modelo como
   `{ error: "..." }` en lugar de crashear el server.
8. **Safe writes** — toda tool de escritura retorna preview por defecto; muta solo con
   `apply: true`. Nunca exponer un `write_file` genérico.
9. **`format: 'toon'`** para arrays/listados (~40% menos tokens); JSON (default) para
   objetos de estado o campos con HTML. Ver criterio en [03-agregar-tool.md](references/03-agregar-tool.md).
10. **`smoke()` co-ubicado** — cada tool declara sus propias verificaciones en el mismo
    archivo; el runner las descubre y ejecuta. `node --check` siempre antes del smoke.
11. **`.vscode/mcp.json` se versiona** — con la excepción `.vscode/*` + `!.vscode/mcp.json`
    en `.gitignore`. Secretos via `inputs`, nunca hardcodeados.
12. **Confinar filesystem en el propio server** — `sandboxEnabled` de VS Code es
    macOS/Linux only; en Windows el server debe validar paths (rechazar `../`) y limitar
    el alcance de lecturas/escrituras al root del proyecto.
