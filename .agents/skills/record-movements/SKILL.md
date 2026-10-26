---
name: record-movements
description: >
  Llevar la contabilidad de la familia Baltodano Soto con el MCP bancos: anotar estados de cuenta
  (HTML del BCR, PDF de la tarjeta BAC), comprobantes (SINPE, voucher) y facturas (supermercado,
  servicios públicos) en sus 3 niveles, vincularlos y sacar reportes del hogar y del papá. Usar
  también para corregir un movimiento, ver qué falta conciliar o responder cuánto se gastó o ingresó.
  Triggers: estado de cuenta, BCR, BAC, tarjeta, comprobante, SINPE, voucher, factura, recibo, gasto,
  ingreso, salario, anotá, registrá, cuánto gasté, resumen del mes, papá, gastos de papi, sin detalle.
---

# Contabilidad familiar

Todo se escribe con el MCP `bancos`; nunca editar los jsonl a mano. Formato y carpetas: `README.md` del repo.

## Niveles

| Nivel | Documento | Tool | Suma en reportes |
|-------|-----------|------|------------------|
| 1. Movimiento | Estado de cuenta: HTML de movimientos del BCR, PDF del estado de la BAC | `bancos_registrar` | **Sí** (base de verdad) |
| 2. Comprobante | Pago: SINPE, voucher, transferencia; trae el número de movimiento del banco | `bancos_comprobante` | No |
| 3. Factura | Detalle de productos o cargos: súper, recibo de luz, agua, internet | `bancos_factura` | No |

Los niveles 2 y 3 sirven para saber qué fue un movimiento: le pasan su `tipo`, `de` y `nota` al vincularse.
Si su movimiento todavía no está (el estado no ha llegado), quedan **pendientes** y se vinculan solos al anotar el estado.

El comprobante es el **pago** de una factura, y no es 1 a 1: una factura se puede pagar en varios comprobantes
(abonos, cuotas) y un comprobante puede pagar varias facturas. La factura con comprobantes se concilia por ellos
(sus movimientos son los de sus pagos) y muestra lo pagado y el saldo.

## Rubros

* Sin `de` = **hogar**: el dueño y Karen (los salarios de los dos son del hogar).
* `de: "papa"` = el papá (vive aparte y depende del dueño): lo que se le paga y lo que él aporta. `de: "primo"` y otros, igual.
* Ningún reporte excluye nada por defecto. Cómo reconocer cada caso: `datos/reglas.md` (privado).

## Procedimiento

1. **Leer el documento.** Los archivos de Telegram llegan a `~/Documents/ezekl/telegram-agent-remote/state/inbox/<chat>/`.
   HTML del BCR: script corto (Python `-I`) que saque las columnas de la tabla. PDF de la BAC: `pdftotext -layout`.
   Foto o PDF de comprobante o factura: leerlo con Read.
2. **Decidir el nivel** con la tabla de arriba. Una foto de SINPE o un voucher es nivel 2 aunque sea el único papel que hay.
3. **Campos del nivel 1** (cada línea del estado):

   | Campo | Regla |
   |-------|-------|
   | `cuenta` | Tarjeta BAC → `bac`. Cuenta BCR (débito, SINPE, transferencias, salarios) → `bcr` |
   | `fecha` | La de **proceso del banco**. BCR: «Fecha contable». BAC: las 4 primeras cifras de la referencia (MMDD; si el mes es menor que el de la compra, es del año siguiente). Intereses y cuotas de tasa cero: la fecha de corte |
   | `fechaReal` | Fecha de la compra, solo referencia (BCR «Fecha transacción»; BAC, la columna de fecha) |
   | `referencia` | BCR: columna «Documento». BAC: la referencia de la línea. Intereses y cuotas no traen: se deja vacía |
   | `clase` | Cargo, compra, débito → `gasto`. Depósito, crédito, salario → `ingreso` |
   | `tipo` | El más cercano de `bancos_tipos`; si ninguno sirve, `otros` y avisarle. Tipo nuevo solo si el dueño lo pide (`bancos_agregar_tipo`) |
   | `total` / `moneda` | Monto positivo; `₡` → `CRC`, `$` → `USD` |
   | `de` | Vacío si es del hogar; `papa` u otro según `datos/reglas.md` |
   | `comercio` / `nota` | Corto, tal como aparece; la nota solo si aporta |

4. **Campos de los niveles 2 y 3:**
   * Comprobante: `fecha`, `hora`, `cuenta`, `clase`, `total`, `moneda`, `comercio` (destinatario), `referencia` (en el SINPE del BCR, el «Documento»), `nota` (el motivo) y `origen` (la foto). Poner `tipo` y `de` si el comprobante los aclara.
   * Factura: `fecha`, `comercio`, `numero`, `total` (lo pagado, con IVA), `moneda`, `lineas` (`descripcion`, `cantidad`, `unidad`, `precio`, `total`; `tipo` opcional para una línea distinta, p. ej. `salud` en el súper) y `origen`. Si se pagó con comprobantes ya anotados, `comprobantes: ["c-...", ...]` (uno por pago); entonces no lleva `movimiento`. Un pago que llega después se agrega con `bancos_vincular { detalle: "f-...", comprobantes: [lista completa] }`.
5. **Anotar:** el tool sin `apply` (preview) → revisar `vinculados` (con `cambios` y `empate`), `pendientes`, `omitidosPorDuplicado` y `avisos` → repetir con `apply: true`. El respaldo se copia solo a `datos/multimedia/AAAA-MM/`.
   Un estado con muchas líneas va en un solo `bancos_registrar` por cuenta, con el estado como `origen`.
6. **Responder corto:** cuántos anotó y en qué nivel, total por moneda, cuántos se vincularon y cuántos quedan pendientes.

## Casos especiales

* Pago de la tarjeta BAC desde el BCR: `gasto` en `bcr` con tipo `tarjeta`. Para el gasto real, `excluirTipos: ["tarjeta"]` (los consumos ya están en `bac`).
* Un SINPE que cubre dos cosas (p. ej. alquiler y abono de otra persona): dos movimientos con la misma `referencia`, cada uno con su tipo y `de`.
* Tasas cero que pagan otros, salarios, depa y lo del papá: seguir `datos/reglas.md`, incluidos los avisos que pide.
* `empate: true` en un vínculo: había varios movimientos iguales; si el comprobante tenía nota o tipo propios, revisar con `bancos_detalles` y corregir con `bancos_vincular`.
* Si algo no está claro (cuenta, clase, una fecha ilegible): preguntar una sola cosa, con opciones y una recomendada, antes de anotar.

## Reportes y consultas

* **Mes completo:** `bancos_resumen { mes }` → balance, rubros (hogar, papa…) y tipos por rubro. Incluye al papá.
* **Solo el hogar:** `excluirDe: ["papa"]` (y `"primo"` si hace falta). **Solo el papá, para mostrárselo:** `de: "papa"`.
* **Detalle:** `bancos_movimientos` (con `c` y `f` = cuántos comprobantes y facturas tiene cada uno); `bancos_detalles { movimiento: "m-..." }` o `{ mes, conLineas: true }`.
* **Qué falta conciliar:** `bancos_pendientes { mes }` → movimientos sin comprobante ni factura, comprobantes o facturas sin movimiento y facturas con saldo (pagadas en parte).
* **Preguntas cruzadas** (productos comprados, gasto por línea de factura, etc.): `bancos_sql` con un SELECT (tablas en su descripción). Los totales oficiales siguen saliendo de `movimientos`.

## Corregir

* Tipo, `de`, comercio, nota, referencia o `fechaReal` de un movimiento: `bancos_editar_movimiento { id, ..., confirm: true }`.
* Fecha, total o clase: `bancos_borrar_movimiento { id, confirm: true }` → volver a anotar (sus detalles quedan pendientes y se vuelven a vincular).
* Vínculo equivocado: `bancos_vincular { detalle, movimiento }` (o `movimiento: ""` para soltarlo). Comprobante o factura mal anotado: `bancos_borrar_detalle` y volver a anotar.
* Nombre de un tipo: `bancos_renombrar_tipo` (el id no cambia).

## Privacidad

El repo es público: montos, comercios y archivos solo viven en `datos/`. No copiarlos a `ia/`, commits ni grupos de Telegram; solo al chat del dueño.
