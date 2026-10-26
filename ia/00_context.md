# 00 — Contexto del Proyecto

> Última actualización: 2026-10-26
> Alcance activo: `~/Documents/ezekl/Bancos` (repo público `ezekiell1988/Bancos`, rama `main`)

## Identidad del proyecto

| Campo | Valor |
|-------|-------|
| Nombre | Bancos |
| Propósito | Contabilidad de la familia Baltodano Soto (hogar: el dueño y Karen; el papá como rubro aparte) en 3 niveles: movimientos de los estados de cuenta (base de verdad), comprobantes y facturas (detalle auxiliar), en jsonl por día con su respaldo y un índice SQLite. |
| Usuarios principales | El dueño, casi siempre desde Telegram (bot telegram-agent-remote) |
| Responsable | Ezequiel Baltodano Cubillo (EBC) |

## Stack tecnológico

| Capa | Tecnología | Notas |
|------|------------|-------|
| Datos | Archivos jsonl en `datos/` + `datos/bancos.sqlite` | Los jsonl son la fuente de verdad; SQLite (`node:sqlite`) es un índice que se reconstruye (ADR-05) |
| MCP | Node.js 24 sin dependencias externas | `.mcp/bancos/` sobre el runtime compartido `.mcp/_shared` (enlace a `~/.mcp/_shared`), skill `mcp-vscode` |
| Skills | `SKILL.md` en `.agents/skills/` | Skill `create-skill` |
| Pruebas | Smoke test del MCP | `node .mcp/bancos/tests/smoke.mjs` |

## Constantes críticas

* El repo es **público**: `datos/` y `.local-secrets/` nunca van a git. No copiar montos, cuentas, comercios ni movimientos a `/ia`, commits, logs ni chats de grupo.
* Cuentas: `bcr` (cuenta BCR: gastos e ingresos, como salarios) y `bac` (tarjeta BAC). Ingresos: normalmente salarios de ITQS o ClickEat, en la cuenta donde caen (casi siempre BCR).
* Monedas: `CRC` y `USD`.
* Niveles (ADR-03): 1 = movimiento del estado de cuenta (único que suma en reportes); 2 = comprobante (SINPE, voucher); 3 = factura con líneas. Los niveles 2 y 3 apuntan a su movimiento por id. El comprobante es el pago de la factura, varios a varios (ADR-06): la factura con `comprobantes` se concilia por ellos.
* Rubros (ADR-04): sin `de` = hogar; `de: "papa"` (u otro) = rubro aparte. Los reportes no excluyen nada por defecto.
* El código anterior (MCP .NET + SQL, `ia/` vieja) quedó en la rama `legacy`, solo para consulta.

## Mapa del repositorio

| Ruta | Propósito |
|------|----------|
| `README.md` | Formato de `datos/`, de cada movimiento y de los tipos |
| `datos/` | Movimientos, tipos y respaldos (fuera de git) |
| `.mcp/bancos/` | MCP para anotar, vincular, consultar y corregir los 3 niveles |
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
* Anotar siempre con el MCP `bancos` (preview y luego `apply: true`) siguiendo el skill `record-movements`, no editando los jsonl a mano.
* Si un documento no deja clara la cuenta, el tipo o la fecha, preguntar al dueño una sola cosa con opciones antes de anotar.
* Los archivos que manda por Telegram llegan a `~/Documents/ezekl/telegram-agent-remote/state/inbox/<chat>/`; se copian a `datos/multimedia/AAAA-MM/` al anotar.
