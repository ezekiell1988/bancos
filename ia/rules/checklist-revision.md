# Checklist de revisión — Bancos

Regla propia del proyecto. La aplica [`revision-codigo.md`](./revision-codigo.md) sobre cada diff, además
de sus verificaciones generales. `init-ia.mjs --refresh-rules` nunca sobrescribe este archivo.

## Seguridad y privacidad (repo público)

* [ ] Ningún archivo de `datos/` ni de `.local-secrets/` entra al diff; `.gitignore` los sigue excluyendo.
* [ ] Ni el código, ni `/ia`, ni el mensaje de commit traen montos, cuentas, comercios reales ni nombres de archivos de respaldo.
* [ ] Las pruebas usan datos inventados en una carpeta temporal, nunca `datos/` real.

## Datos (`datos/`)

* [ ] Cada movimiento sigue el formato de `README.md`: `fecha`, `clase`, `tipo`, `total`, `moneda`, `comercio`, `nota`, `archivo`.
* [ ] Ruta del movimiento: `datos/<bcr|bac>/AAAA-MM/AAAA-MM-DD.jsonl`; respaldo en `datos/multimedia/AAAA-MM/`.
* [ ] `tipo` existe en `tipos-gasto.jsonl` o `tipos-ingreso.jsonl` según `clase`; `moneda` es `CRC` o `USD`; `total` es positivo.
* [ ] Un cambio de formato actualiza a la vez `README.md`, `02_architecture.md` y el MCP `bancos`.

## MCP `bancos` (`.mcp/bancos/`)

* [ ] Sigue el contrato del skill `mcp-vscode`: un tool por archivo, `additionalProperties: false`, `items` en todo array, `ToolError` para errores de negocio.
* [ ] Todo tool que escribe hace preview por defecto y solo escribe con `apply: true`; borrar pide `confirm: true`.
* [ ] Nunca reescribe un jsonl entero salvo para borrar o corregir una línea pedida; anotar es agregar al final.
* [ ] `node .mcp/bancos/tests/smoke.mjs` termina en `SMOKE TEST: TODO OK`.

## Skills del proyecto (área DOC)

* [ ] Siguen el skill `create-skill`: `name` igual a la carpeta, `description` con triggers, `SKILL.md` corto.
* [ ] El skill remite a los tools del MCP `bancos` en vez de describir cómo escribir los jsonl a mano.

## Trazabilidad con /ia

* [ ] El cambio pertenece a una tarea en curso de `04_tasks/` y respeta su alcance permitido.
* [ ] Una decisión nueva de formato o de estructura queda como ADR en `06_decisions/`.
