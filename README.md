# Bancos

Contabilidad de la familia Baltodano Soto a partir de estados de cuenta (Bancos), pagos (Libros) y facturas (Cuentas por pagar).
Se anota con el MCP local `bancos` (`.mcp/bancos/`, ver su `README.md`) siguiendo el skill `record-movements`.
El contexto y el plan viven en `ia/` (workflow iaWorkflow).
El código anterior (MCP .NET + SQL) quedó en la rama `legacy`, solo para consulta.

## Niveles (ADR-EBC-03)

| Nivel | Qué es | Dónde | ¿Suma en reportes? |
|-------|--------|-------|--------------------|
| 1. Bancos | Línea de un estado de cuenta del ciclo 19→18 (BCR débito de Karen, BAC y BN tarjetas) | `datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl` | Sí: es la base de verdad |
| 2. Libros | El **pago** (SINPE, voucher, colilla): 1 a 1 con un movimiento; lleva la **categoría** en partidas | `datos/libros/AAAA-MM/AAAA-MM-DD.jsonl` | Reparte el nivel 1 por categoría |
| 3. Cuentas por pagar | La **factura** (o un ajuste) con proveedor, cédula, número, líneas y ajustes; siempre cuelga de un libro | `datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl` | No |

* Todo movimiento tiene su libro; si no hay pago anotado, se crea uno **sin documento** solo con la categoría (por defecto «otros»; se revisa al cerrar el ciclo).
* Las `partidas` del libro (`{tipo, monto, de?, nota?}`) suman siempre el total del movimiento. Con facturas, son la suma de sus líneas por categoría.
* La suma de las facturas (y ajustes) de un libro es igual al monto del libro. Un sobrante que no se gastó (vuelto devuelto) es una factura `clase: "ajuste"` con su respaldo y su categoría.
* Un libro sin movimiento es una diferencia por conciliar, salvo que lleve `transito: {razon}`. No hay factura sin libro.
* «Pendientes» = libros sin movimiento (menos en tránsito), movimientos sin libro o con partidas en «otros», y sumas que no cuadran. Las facturas se vinculan a un movimiento hasta 35 días después y con ±1 de diferencia; los pagos, con -3/+10 días y monto exacto.

**Rubros:** sin `de` el movimiento es del hogar (el dueño y Karen); `de: "papa"` (u otra persona) lo separa como rubro.
Los reportes incluyen todo por defecto; el papá se excluye o se ve solo con una opción.

## Estructura (`datos/`, fuera de git)

```
datos/
  tipos-gasto.jsonl, tipos-ingreso.jsonl
  bcr/AAAA-MM/AAAA-MM-DD.jsonl        nivel 1, cuenta BCR
  bac/AAAA-MM/AAAA-MM-DD.jsonl        nivel 1, tarjeta BAC
  bncr/AAAA-MM/AAAA-MM-DD.jsonl       nivel 1, tarjeta del Banco Nacional
  libros/AAAA-MM/AAAA-MM-DD.jsonl         nivel 2 (pagos con la categoría en partidas)
  facturas/AAAA-MM/AAAA-MM-DD.jsonl       nivel 3 (facturas y ajustes, cuelgan de un libro)
  multimedia/AAAA-MM/                 respaldos (HTML, PDF, fotos) de los 3 niveles
  bancos.sqlite                       índice de los 3 niveles, se reconstruye desde los jsonl
  reglas.md                           cómo reconocer salarios y lo de otras personas (privado)
```

## Movimiento (nivel 1)

```json
{"id":"m-1a2b3c4d","fecha":"2026-10-24","fechaReal":"2026-10-22","referencia":"977393","clase":"gasto","total":8500,"moneda":"CRC","comercio":"Soda X","nota":"","de":"papa","archivo":"multimedia/2026-10/2026-10-18-bcr-movimientos.html"}
```

- `id`: lo pone el MCP; lo usan los libros.
- La categoría ya **no** se guarda aquí: vive en las `partidas` de su libro (al leer, `tipo` es la categoría principal).
- `fecha`: fecha de proceso del banco (BCR «Fecha contable»; BAC, MMDD de la referencia; BN, la fecha de la transacción, que es la única que trae). Es la que usan reportes y ciclos, y la del archivo del día.
- `fechaReal` (opcional): fecha de la compra o transacción, solo de referencia.
- `referencia` (opcional): número del movimiento en el banco (BCR «Documento»; BAC, referencia).
- `clase`: `gasto` o `ingreso`. `moneda`: `CRC` o `USD`. Los `tipo` son ids de `tipos-gasto.jsonl` o `tipos-ingreso.jsonl`.
- `de` (opcional): rubro de otra persona, p. ej. `"papa"`. Sin `de` = hogar.
- `tarjeta` (tarjetas `bac` y `bncr`): 4 últimos dígitos de la cuenta de tarjeta titular, la que se paga. `adicional` (opcional): plástico adicional que hizo la compra; va con la `tarjeta` de su titular porque sale en el mismo estado y se paga junto. Sin `adicional` = la usó el titular.
- `archivo`: ruta relativa a `datos/` del respaldo, o `null`.

## Libro (nivel 2)

```json
{"id":"l-5e6f7a8b","fecha":"2026-10-25","hora":"08:15","cuenta":"bcr","clase":"gasto","total":5000,"moneda":"CRC","comercio":"Juan Pérez","referencia":"99416599","nota":"Retiro para papá","documento":"multimedia/2026-10/sinpe.jpg","movimiento":"m-1a2b3c4d","partidas":[{"tipo":"gastos-papi","monto":3000},{"tipo":"personal","monto":2000}]}
```

- `movimiento`: id del nivel 1 (1 a 1); `null` = por conciliar. `transito: {"razon":"..."}` (opcional) lo marca en tránsito.
- `documento`: respaldo del pago, o `null` si el libro solo guarda la categoría de un movimiento sin papel.
- `partidas`: `{tipo, monto, de?, nota?}`; la suma es el total del movimiento. `de` (rubro) y `nota` se le pasan al movimiento al vincularse.

## Factura (nivel 3)

```json
{"id":"f-9c0d1e2f","fecha":"2026-10-25","proveedor":"Súper Y","cedula":"3-101-123456","numero":"FE-55","clase":"factura","total":3000,"moneda":"CRC","nota":"","archivo":"multimedia/2026-10/factura.jpg","libro":"l-5e6f7a8b","lineas":[{"descripcion":"Arroz","cantidad":2,"precio":1500,"impuesto":0,"total":3000,"tipo":"gastos-papi"}],"ajustes":[]}
```

- `libro`: el pago del que cuelga (obligatorio). `clase`: `factura` o `ajuste` (sobrante que no se gastó, con su respaldo y su propia categoría).
- Cada línea lleva su `tipo` (categoría); `ajustes` (descuentos, redondeo) van al final y suman al total.
- Ejemplo del retiro: libro de ₡5.000 con una factura de ₡3.000 y un ajuste de ₡2.000 (vuelto devuelto por SINPE); sus partidas salen de las líneas.

## Tipo

```json
{"id":"comida","nombre":"Comida"}
```

## Dashboard de finanzas (`dashboard/`)

Pestaña «💰 Finanzas» de la Mini App (al elegir el proyecto Bancos). Solo consulta: periodo (ciclo 19→18, mes o rango), resumen, gastos por tipo, pago de contado de las tarjetas, pendientes y movimientos por cuenta. Cada reporte tiene «Enviar al chat», que manda una imagen de 1080 px sin textos cortados.

* Servidor: `node dashboard/server.mjs` (solo 127.0.0.1:4780); en la Mac lo deja arriba launchd (`~/Library/LaunchAgents/com.ezekl.bancos-dashboard.plist`).
* Config: copiar `dashboard/config.example.json` a `dashboard/config.json` (ignorado en git) y poner el `chatId` del chat de Bancos.
* Por WhatsApp: la tool `bancos_dashboard_enlace { para: 'yo' | 'novia' }` manda un enlace de 1 hora (firmado por el bot) solo a los números de `whatsapp` en `config.json` (también deben estar en `whatsappEnlace` de la pestaña en la config del bot). Desde ese enlace, «Enviar al chat» contesta al mismo WhatsApp (header `x-canal-destino`). El número escribió al bot en las últimas 24 h, o Meta no deja mandarlo.
* Detalle: cada monto es un enlace a `detalle.html` (filtros `f_clase`, `f_rubro`, `f_moneda`, `f_tipo`, `f_tarjeta`, `f_desde`, `f_hasta`) con la tabla de movimientos que lo forman, total, «Enviar al chat» (`/enviar?reporte=detalle`) y «Atrás». En las tarjetas del pago muestra además el monto del estado y lo que falta anotar.
* Los números salen de los mismos handlers que `bancos_resumen`, `bancos_movimientos`, `bancos_pendientes` y `bancos_pago_tarjetas`.
