# MCP bancos

Anota y consulta los movimientos de `datos/` (formato en el `README.md` del repo). Node sin dependencias,
sobre el runtime compartido `../_shared` (enlace a `~/.mcp/_shared`).

| Tool | Qué hace |
|------|----------|
| `bancos_tipos` | Lista los tipos de gasto e ingreso |
| `bancos_agregar_tipo` | Agrega un tipo (preview; `apply: true`) |
| `bancos_registrar` | Anota movimientos y copia el respaldo a `multimedia/AAAA-MM/` (preview; `apply: true`) |
| `bancos_movimientos` | Lista movimientos de un mes o rango, con cuenta y línea |
| `bancos_resumen` | Totales por tipo y balance por moneda |
| `bancos_borrar_movimiento` | Borra una línea (preview; `confirm: true`) |

Flag: `--datos-dir` (default `<repo>/datos`). Prueba: `node .mcp/bancos/tests/smoke.mjs`.
