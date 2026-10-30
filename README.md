# Bancos

Contabilidad de la familia Baltodano Soto a partir de estados de cuenta, comprobantes y facturas.
Se anota con el MCP local `bancos` (`.mcp/bancos/`, ver su `README.md`) siguiendo el skill `record-movements`.
El contexto y el plan viven en `ia/` (workflow iaWorkflow).
El código anterior (MCP .NET + SQL) quedó en la rama `legacy`, solo para consulta.

## Niveles

| Nivel | Qué es | Dónde | ¿Suma en reportes? |
|-------|--------|-------|--------------------|
| 1. Movimiento | Línea del estado de cuenta (HTML del BCR, PDF de la BAC) | `datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl` | Sí: es la base de verdad |
| 2. Comprobante | Pago: SINPE, voucher, con su número de movimiento | `datos/comprobantes/AAAA-MM/AAAA-MM-DD.jsonl` | No |
| 3. Factura | Documento cobrado: detalle de productos o cargos | `datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl` | No |

Los niveles 2 y 3 apuntan a su movimiento con `movimiento` (id). Sin él están pendientes de banco.

El comprobante es el **pago** de una factura y no es 1 a 1 (ADR-06): una factura se paga con uno o varios
comprobantes (`comprobantes`, abonos o cuotas) y un comprobante puede pagar varias facturas. Una factura con
comprobantes se concilia por ellos; sin comprobantes se vincula directo a su movimiento. Saldo = total − lo pagado.

**Rubros:** sin `de` el movimiento es del hogar (el dueño y Karen); `de: "papa"` (u otra persona) lo separa como rubro.
Los reportes incluyen todo por defecto; el papá se excluye o se ve solo con una opción.

## Estructura (`datos/`, fuera de git)

```
datos/
  tipos-gasto.jsonl, tipos-ingreso.jsonl
  bcr/AAAA-MM/AAAA-MM-DD.jsonl        nivel 1, cuenta BCR
  bac/AAAA-MM/AAAA-MM-DD.jsonl        nivel 1, tarjeta BAC
  comprobantes/AAAA-MM/AAAA-MM-DD.jsonl   nivel 2
  facturas/AAAA-MM/AAAA-MM-DD.jsonl       nivel 3
  multimedia/AAAA-MM/                 respaldos (HTML, PDF, fotos) de los 3 niveles
  bancos.sqlite                       índice de los 3 niveles, se reconstruye desde los jsonl
  reglas.md                           cómo reconocer salarios y lo de otras personas (privado)
```

## Movimiento (nivel 1)

```json
{"id":"m-1a2b3c4d","fecha":"2026-10-24","fechaReal":"2026-10-22","referencia":"977393","clase":"gasto","tipo":"comida","total":8500,"moneda":"CRC","comercio":"Soda X","nota":"","de":"papa","archivo":"multimedia/2026-10/2026-10-18-bcr-movimientos.html"}
```

- `id`: lo pone el MCP; lo usan los comprobantes y facturas.
- `fecha`: fecha de proceso del banco (BCR «Fecha contable»; BAC, MMDD de la referencia). Es la que usan reportes y ciclos, y la del archivo del día.
- `fechaReal` (opcional): fecha de la compra o transacción, solo de referencia.
- `referencia` (opcional): número del movimiento en el banco (BCR «Documento»; BAC, referencia).
- `clase`: `gasto` o `ingreso`. `tipo`: id de `tipos-gasto.jsonl` o `tipos-ingreso.jsonl`. `moneda`: `CRC` o `USD`.
- `de` (opcional): rubro de otra persona, p. ej. `"papa"`. Sin `de` = hogar.
- `tarjeta` (solo `bac`): 4 últimos dígitos de la cuenta de tarjeta titular, la que se paga. `adicional` (opcional): plástico adicional que hizo la compra; va con la `tarjeta` de su titular porque sale en el mismo estado y se paga junto. Sin `adicional` = la usó el titular.
- `archivo`: ruta relativa a `datos/` del respaldo, o `null`.

## Comprobante (nivel 2)

```json
{"id":"c-5e6f7a8b","fecha":"2026-10-25","hora":"08:15","cuenta":"bcr","clase":"gasto","total":2000,"moneda":"CRC","comercio":"Juan Pérez","referencia":"99416599","tipo":"supermercado","nota":"Feria: tomates","archivo":"multimedia/2026-10/sinpe.jpg","movimiento":"m-1a2b3c4d"}
```

`tipo`, `de` y `nota` (opcionales) se le pasan al movimiento al vincularse.

## Factura (nivel 3)

```json
{"id":"f-9c0d1e2f","fecha":"2026-10-25","comercio":"Súper Y","numero":"FE-55","total":12000.5,"moneda":"CRC","nota":"","archivo":"multimedia/2026-10/factura.jpg","movimiento":"m-1a2b3c4d","lineas":[{"descripcion":"Arroz","cantidad":2,"precio":3000,"total":6000}]}
```

Pagada en dos abonos: en vez de `movimiento` lleva `"comprobantes":["c-5e6f7a8b","c-9a0b1c2d"]` (`movimiento` queda `null`).

## Tipo

```json
{"id":"comida","nombre":"Comida"}
```
