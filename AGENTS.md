# AGENTS.md — Bancos

## Workflow

`/ia` es fuente de verdad y MCP `iaWorkflow` su interfaz: `ia_validate` + `ia_get_context` al iniciar, `work_task` antes de editar y `finish_task` al cerrar.

## Qué hace el proyecto

El dueño pasa documentos bancarios y facturas; el agente anota cada movimiento en `datos/` según `README.md` (jsonl por día, carpeta por mes, respaldo en `multimedia/`). Gastos del BCR a `bcr/`, de la tarjeta BAC a `bac/`; ingresos (salarios ITQS o ClickEat) a la cuenta donde caen, normalmente BCR. Si aparece un tipo nuevo, agregarlo al jsonl de tipos.

## Seguridad

* El repo es público: `datos/` y `.local-secrets/` nunca van a git.
* No copiar montos, cuentas ni movimientos a `/ia`, commits ni logs.
* Código anterior: rama `legacy`, solo consulta.
