# MCP bancos

Contabilidad familiar en 3 niveles sobre `datos/` (formato en el `README.md` del repo). Node sin dependencias
(SQLite con `node:sqlite` de Node 24), sobre el runtime compartido `../_shared` (enlace a `~/.mcp/_shared`).

| Tool | Qué hace |
|------|----------|
| `bancos_tipos` | Lista los tipos de gasto e ingreso |
| `bancos_agregar_tipo` | Agrega un tipo (preview; `apply: true`) |
| `bancos_renombrar_tipo` | Cambia el nombre visible de un tipo (preview; `apply: true`) |
| `bancos_registrar` | **Nivel 1.** Anota las líneas de un estado de cuenta (BCR o BAC) con id, fecha del banco y referencia; vincula los comprobantes y facturas pendientes (preview; `apply: true`) |
| `bancos_comprobante` | **Nivel 2.** Anota SINPE o vouchers con su referencia y los vincula a su movimiento, o los deja pendientes (preview; `apply: true`) |
| `bancos_factura` | **Nivel 3.** Anota facturas con sus líneas y sus pagos (`comprobantes`, uno o varios) o las vincula a su movimiento; da pagado y saldo (preview; `apply: true`) |
| `bancos_movimientos` | Lista movimientos con id, rubro y cuántos comprobantes y facturas tienen (filtros `de`, `excluirDe`) |
| `bancos_resumen` | Reporte del nivel 1: balance, rubros (hogar, papa…) y tipos; `excluirDe`, `de`, `excluirTipos` |
| `bancos_detalles` | Lista comprobantes y facturas (por periodo o por movimiento), con sus pagos, saldo y líneas |
| `bancos_pendientes` | Movimientos sin comprobante ni factura, comprobantes o facturas sin movimiento y facturas con saldo |
| `bancos_borrar_movimiento` | Borra un movimiento por id o línea; sus detalles quedan pendientes (preview; `confirm: true`) |
| `bancos_editar_movimiento` | Cambia tipo, `de`, comercio, nota, referencia o `fechaReal` (preview; `confirm: true`) |
| `bancos_borrar_detalle` | Borra un comprobante o factura (preview; `confirm: true`) |
| `bancos_vincular` | Vincula o desvincula a mano un comprobante o factura, o fija los pagos de una factura (preview; `confirm: true`) |
| `bancos_sql` | Consulta SELECT de solo lectura sobre `datos/bancos.sqlite` |
| `bancos_sqlite_reconstruir` | Reconstruye `datos/bancos.sqlite` desde los jsonl |

Los tools de escritura reconstruyen la base SQLite solos al guardar. Flag: `--datos-dir` (default `<repo>/datos`).
Prueba: `node .mcp/bancos/tests/smoke.mjs`.
