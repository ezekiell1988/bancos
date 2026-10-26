# Bancos

Registro sencillo de movimientos (ingresos y gastos) a partir de documentos bancarios y facturas.
Los movimientos se anotan con el MCP local `bancos` (`.mcp/bancos/`, ver su `README.md`).
El contexto y el plan viven en `ia/` (workflow iaWorkflow).
El código anterior (MCP .NET + SQL) quedó en la rama `legacy`, solo para consulta.

## Estructura (`datos/`, fuera de git)

```
datos/
  tipos-gasto.jsonl
  tipos-ingreso.jsonl
  bcr/AAAA-MM/AAAA-MM-DD.jsonl     cuenta BCR (gastos e ingresos, p. ej. salarios)
  bac/AAAA-MM/AAAA-MM-DD.jsonl     tarjeta BAC
  multimedia/AAAA-MM/              fotos, PDF y Excel de respaldo
```

## Movimiento (una línea por movimiento)

```json
{"fecha":"2026-10-24","clase":"gasto","tipo":"comida","total":8500,"moneda":"CRC","comercio":"Soda X","nota":"","archivo":"multimedia/2026-10/factura-soda-x.jpg"}
```

- `clase`: `gasto` o `ingreso`.
- `tipo`: id de `tipos-gasto.jsonl` o `tipos-ingreso.jsonl`.
- `moneda`: `CRC` o `USD`.
- `archivo`: ruta relativa a `datos/` del respaldo, o `null`.

## Tipo (una línea por tipo)

```json
{"id":"comida","nombre":"Comida"}
```
