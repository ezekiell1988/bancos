# 00 — Contexto del Proyecto

> Última actualización: 2026-10-25
> Alcance activo: `~/Documents/ezekl/Bancos` (repo público `ezekiell1988/Bancos`, rama `main`)

## Identidad del proyecto

| Campo | Valor |
|-------|-------|
| Nombre | Bancos |
| Propósito | Registro personal y sencillo de movimientos (gastos e ingresos): el dueño pasa documentos bancarios y facturas y el agente anota cada movimiento en jsonl por día, con su respaldo. |
| Usuarios principales | El dueño, casi siempre desde Telegram (bot telegram-agent-remote) |
| Responsable | Ezequiel Baltodano Cubillo (EBC) |

## Stack tecnológico

| Capa | Tecnología | Notas |
|------|------------|-------|
| Datos | Archivos jsonl en `datos/` | Sin base de datos; una línea por movimiento |
| MCP | Node.js sin dependencias externas | `.mcp/bancos/` sobre el runtime compartido `.mcp/_shared` (enlace a `~/.mcp/_shared`), skill `mcp-vscode` |
| Skills | `SKILL.md` en `.agents/skills/` | Skill `create-skill` |
| Pruebas | Smoke test del MCP | `node .mcp/bancos/tests/smoke.mjs` |

## Constantes críticas

* El repo es **público**: `datos/` y `.local-secrets/` nunca van a git. No copiar montos, cuentas, comercios ni movimientos a `/ia`, commits, logs ni chats de grupo.
* Cuentas: `bcr` (cuenta BCR: gastos e ingresos, como salarios) y `bac` (tarjeta BAC). Ingresos: normalmente salarios de ITQS o ClickEat, en la cuenta donde caen (casi siempre BCR).
* Monedas: `CRC` y `USD`.
* El código anterior (MCP .NET + SQL, `ia/` vieja) quedó en la rama `legacy`, solo para consulta.

## Mapa del repositorio

| Ruta | Propósito |
|------|----------|
| `README.md` | Formato de `datos/`, de cada movimiento y de los tipos |
| `datos/` | Movimientos, tipos y respaldos (fuera de git) |
| `.mcp/bancos/` | MCP para anotar, listar, resumir y corregir movimientos |
| `.agents/skills/` | Skills del proyecto (cómo procesar documentos) |
| `ia/` | Contexto y plan del proyecto (workflow iaWorkflow) |

## Comandos de validación

| Alcance | Comando |
|---------|--------|
| Sintaxis MCP | `node --check .mcp/bancos/server.mjs` |
| Tests | `node .mcp/bancos/tests/smoke.mjs` |
| /ia | `ia_validate` del MCP iaWorkflow |

## Notas para agentes

* Escritos de `/ia` con iaWorkflow: preview primero; `apply: true` solo con el pedido o la aprobación del dueño.
* Anotar movimientos siempre con el MCP `bancos` (preview y luego `apply: true`), no editando los jsonl a mano.
* Si un documento no deja clara la cuenta, el tipo o la fecha, preguntar al dueño una sola cosa con opciones antes de anotar.
* Los archivos que manda por Telegram llegan a `~/Documents/ezekl/telegram-agent-remote/state/inbox/<chat>/`; se copian a `datos/multimedia/AAAA-MM/` al anotar.
