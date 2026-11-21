---
name: record-movements
description: >
  Llevar la contabilidad de la familia Baltodano Soto con el MCP bancos: anotar estados de cuenta
  (HTML del BCR, PDF de la tarjeta BAC o del BN), comprobantes (SINPE, voucher) y facturas (supermercado,
  servicios públicos) en sus 3 niveles, vincularlos y sacar reportes del hogar y del papá. Usar
  también para corregir un movimiento, ver qué falta conciliar o responder cuánto se gastó o ingresó.
  Triggers: estado de cuenta, Uber, recibo de Uber, BCR, BAC, tarjeta, comprobante, SINPE, voucher, factura, recibo, gasto,
  ingreso, salario, anotá, registrá, cuánto gasté, resumen del mes, papá, gastos de papi, sin detalle.
---

# Contabilidad familiar

Todo se escribe con el MCP `bancos`; nunca editar los jsonl a mano. Formato y carpetas: `README.md` del repo.

## Niveles (ADR-EBC-03)

| Nivel | Qué es | Documento | Tool | Suma en reportes |
|-------|--------|-----------|------|------------------|
| 1. Bancos | Movimiento del estado de cuenta (ciclo 19 al 18: BCR débito de Karen, BAC tarjetas, BN tarjetas) | HTML del BCR, PDF de la BAC o del BN | `bancos_registrar` | **Sí** (base de verdad) |
| 2. Libros | El **pago**, 1 a 1 con un movimiento; lleva la **categoría** en `partidas` | SINPE, voucher, colilla, o un documento que diga «pago de factura X» (nunca la factura) | `bancos_comprobante` | Reparte el nivel 1 por categoría |
| 3. Cuentas por pagar | La **factura**: proveedor, cédula jurídica, número, líneas, ajustes | Súper, recibo de luz, agua, internet | `bancos_factura` | No |

* **La categoría vive en el nivel 2**: una o varias `partidas: [{tipo, monto}]` que suman siempre el total del movimiento. Lo que no se sabe va a «otros» y se revisa al cerrar el ciclo.
* Todo movimiento tiene su libro: `bancos_registrar` lo crea solo (sin documento) con el `tipo` que se le dé; si había un pago pendiente, se vincula y trae su categoría. Un pago anotado después **reemplaza** al libro sin documento del movimiento.
* Un libro sin movimiento es una diferencia por conciliar, salvo que se marque en tránsito (`transito: "razón"`).
* El nivel 3 **siempre cuelga de un libro** (`libro` o `movimiento`); no hay factura suelta. Un libro tiene una o varias facturas y la **suma de sus facturas (con ajustes) es igual al monto del pago**. Cada línea lleva su `tipo`; al cuadrar, las partidas del libro son la suma de las líneas por categoría.
* Un sobrante que no se gastó (p. ej. vuelto devuelto por SINPE) es otra factura con `clase: "ajuste"`, con su respaldo y su propia categoría. Ejemplo: retiro de ₡5.000 → factura ₡3.000 (comida) + ajuste ₡2.000 (personal), en la misma llamada de `bancos_factura` con `movimiento: "m-..."`. Ejemplo de compra en Palí con 2 artículos personales: 1 factura con líneas de 2 categorías, 1 pago con 2 partidas.

## Rubros

* Sin `de` = **hogar**: el dueño y Karen (los salarios de los dos son del hogar).
* `de: "papa"` = el papá (vive aparte y depende del dueño): lo que se le paga y lo que él aporta. `de: "primo"` y otros, igual.
* Ningún reporte excluye nada por defecto. Cómo reconocer cada caso: `datos/reglas.md` (privado).

## Procedimiento

1. **Leer el documento.** Los archivos de Telegram llegan a `~/Documents/ezekl/telegram-agent-remote/state/inbox/<chat>/`.
   HTML del BCR: script corto (Python `-I`) que saque las columnas de la tabla. PDF de la BAC o del BN: `pdftotext -layout`.
   Foto o PDF de comprobante o factura: leerlo con Read.
2. **Decidir el nivel** con la tabla de arriba. Una foto de SINPE o un voucher es nivel 2 aunque sea el único papel que hay.
3. **Campos del nivel 1** (cada línea del estado):

   | Campo | Regla |
   |-------|-------|
   | `cuenta` | Tarjeta BAC → `bac`. Tarjeta del Banco Nacional (BN) → `bncr`. Cuenta BCR (débito, SINPE, transferencias, salarios) → `bcr` |
   | `fecha` | La de **proceso del banco**. BCR: «Fecha contable». BAC: las 4 primeras cifras de la referencia (MMDD; si el mes es menor que el de la compra, es del año siguiente). BN: la fecha de la transacción (es la única que trae). Intereses y cuotas de tasa cero: la fecha de corte |
   | `fechaReal` | Fecha de la compra, solo referencia (BCR «Fecha transacción»; BAC, la columna de fecha) |
   | `referencia` | BCR: columna «Documento». BAC: la referencia de la línea. Intereses y cuotas no traen: se deja vacía |
   | `clase` | Cargo, compra, débito → `gasto`. Depósito, crédito, salario → `ingreso` |
   | `tipo` | La categoría de su libro: la más cercana de `bancos_tipos`; si ninguno sirve, `otros` (o sin tipo) y avisarle. Tipo nuevo solo si el dueño lo pide (`bancos_agregar_tipo`) |
   | `total` / `moneda` | Monto positivo; `₡` → `CRC`, `$` → `USD` |
   | `de` | Vacío si es del hogar; `papa` u otro según `datos/reglas.md` |
   | `tarjeta` | Solo tarjetas (`bac`, `bncr`): 4 últimos dígitos de la cuenta de tarjeta (la que se paga), según `datos/reglas.md` |
   | `adicional` | Si la línea está en el bloque de un plástico adicional del estado (otro nombre, p. ej. «KAREN»): sus 4 dígitos. Va con la `tarjeta` del titular. Sin él = titular |
   | `comercio` / `nota` | Corto, tal como aparece; la nota solo si aporta (no repetir la tarjeta) |

4. **Campos de los niveles 2 y 3:**
   * Pago (libro): `fecha`, `hora`, `cuenta`, `clase`, `total`, `moneda`, `comercio` (destinatario), `referencia` (en el SINPE del BCR, el «Documento»), `nota` (el motivo) y `origen` (la foto). Categoría: `tipo` si es una sola, o `partidas: [{tipo, monto}]` que sumen el total. `de` si es de otra persona. `transito: "razón"` si aún no sale en el banco.
   * Factura: `fecha`, `proveedor`, `cedula` (jurídica), `numero`, `total` (con IVA), `moneda`, `lineas` (`descripcion`, `cantidad`, `unidad`, `precio`, `impuesto`, `total`, **`tipo` de cada línea**), `ajustes` (descuentos o redondeo al final), `clase` (`factura` o `ajuste`) y `origen`. Cuelga de su pago: `movimiento: "m-..."` o `libro: "l-..."`; sin ninguno se busca el movimiento por monto y fecha, y si no hay se crea un pago sin movimiento (pendiente). Si el pago es de varias facturas, anotarlas todas en la misma llamada (o `parcial: true` si la otra llega después). Para pasar una factura a otro pago: `bancos_vincular { detalle: "f-...", libro: "l-..." }`.
5. **Anotar:** el tool sin `apply` (preview) → revisar `vinculados` (con `cambios` y `empate`), `pendientes`, `omitidosPorDuplicado` y `avisos` → repetir con `apply: true`. El respaldo se copia solo a `datos/multimedia/AAAA-MM/`.
   Un estado con muchas líneas va en un solo `bancos_registrar` por cuenta, con el estado como `origen`. Si `bancos_factura` da error de suma, es que las facturas del pago no llegan a su monto.
6. **Responder corto:** cuántos anotó y en qué nivel, total por moneda, cuántos se vincularon y cuántos quedan pendientes.

## Casos especiales

* Tarjeta con adicionales: es **una sola cuenta** (un estado, un saldo, un pago). En el PDF, cada plástico tiene su bloque con su número y nombre, en compras (B) y en otros cargos (D, el IVA). Todo lleva la `tarjeta` del titular; las líneas del bloque del adicional, además `adicional`. Intereses y comisiones son de la cuenta: sin `adicional`. `bancos_resumen` da `tarjetas`: lo de cada plástico y el `total` que se paga junto.
* Tarjeta del BN (`bncr`): el PDF trae «Detalle de compras del periodo» (fecha de transacción, sin referencia) y «Otras líneas de financiamiento» (p. ej. BN Marchamos 12M, tasa cero). Cada compra es una línea; cada cuota de financiamiento, un `gasto` con la fecha de corte, el tipo de lo que se financió (marchamo → `transporte`) y nota con la cuota (N/M). Compras + cuotas = el pago de contado. Los pagos de la tarjeta (sección «pagos y créditos») no se anotan en `bncr`.
* Pago de la tarjeta BAC desde el BCR: `gasto` en `bcr` con tipo `tarjeta`. Para el gasto real, `excluirTipos: ["tarjeta"]` (los consumos ya están en `bac`).
* Un SINPE que cubre dos cosas (p. ej. alquiler y abono de otra persona): dos movimientos con la misma `referencia`, cada uno con su tipo y `de`.
* Tasas cero que pagan otros, salarios, depa y lo del papá: seguir `datos/reglas.md`, incluidos los avisos que pide.
* Recibo de Uber (PDF o captura): nivel 2 (pago), `bancos_comprobante`. PDF: `pdftotext -layout`. Uber siempre comparte el recibo como 2 archivos juntos: uno de ~1 KB vacío (ignorarlo sin avisar) y el PDF real (cientos de KB); usar solo el más pesado y no pedirle al dueño que mande uno solo (lo genera la página de Uber; él no lo controla). Pueden llegar en 2 mensajes seguidos: si en el mensaje solo viene el vacío, buscar en el inbox el PDF real de la misma hora (segundos de diferencia) y anotarlo; si el otro ya estaba anotado, no duplicarlo. Cuenta `bac`, `clase: gasto`, `tipo: transporte`, `comercio: "Uber"`, `fecha` y `hora` del viaje, `total` = total cobrado. `nota` = origen → destino con el **nombre del lugar**: el recibo trae plus codes (p. ej. `XXXX+XXX`) y la tabla «Lugares de Uber» de `datos/reglas.md` dice qué lugar es cada uno; uno nuevo se le pregunta al dueño y se agrega ahí. Cruzar el viaje con lo anotado cerca (p. ej. una compra del súper minutos antes) para saber a qué corresponde. Si el viaje es del papá (o lleva sus compras a su casa), `de: "papa"` (ver `datos/reglas.md`). En la BAC sale con la fecha del viaje como `fechaReal`; la propina o un ajuste pueden venir como cargo aparte.
* `empate: true` en un vínculo: había varios movimientos iguales; revisar con `bancos_detalles` y corregir con `bancos_vincular`.
* Si algo no está claro (cuenta, clase, una fecha ilegible): preguntar una sola cosa, con opciones y una recomendada, antes de anotar.

## Reportes y consultas

* **El periodo es el ciclo del 19 al 18, no el mes calendario.** Siempre usar `ciclo: "AAAA-MM"`, que va del 19 del mes anterior al 18 de ese mes (ciclo 2026-11 = 19-oct → 18-nov). Al hablar con el dueño, nombrar el ciclo por sus fechas («ciclo 19-oct → 18-nov»), nunca solo «octubre» o «noviembre». `mes` (1 al 31) solo si lo pide explícitamente. Un ingreso que se adelanta o atrasa unos días (p. ej. el salario de Karen el 31 en vez del 1) cae en el mismo ciclo si no cruza el 18/19.
* **Pagar las tarjetas BAC:** `bancos_pago_tarjetas` lee el resumen del último estado (PDF), pasa los dólares a colones con la venta de la BAC de hoy, resta los pagos ya hechos desde el BCR y da `pedirAKaren` (lo que Karen pasa del BCR a la BAC). Devuelve el detalle como imagen PNG (`imagen`, en `datos/reportes/`): mandásela al dueño por Telegram en vez de la tabla en texto.
* **Pagar la tarjeta BN de Karen:** `bancos_pago_tarjeta_bn` hace lo mismo con el último estado del BN (`…-estado-bn-2921.pdf`): dólares a colones con la venta del **BN** de hoy (no la de la BAC), resta los «Pago BN» del BCR desde el corte y da `faltaPagar`. También devuelve la imagen para mandarla por Telegram.
* **Ciclo completo:** `bancos_resumen { ciclo }` → balance, rubros (hogar, papa…) y tipos por rubro. Incluye al papá.
* **Solo el hogar:** `excluirDe: ["papa"]` (y `"primo"` si hace falta). **Solo el papá, para mostrárselo:** `de: "papa"`.
* **Detalle:** `bancos_movimientos` (con su libro, sus `partidas` y `f` = cuántas facturas cuelgan); `bancos_detalles { movimiento: "m-..." }` o `{ ciclo, conLineas: true }`.
* **Qué falta conciliar:** `bancos_pendientes { ciclo }` → `libroSinMovimiento` (pagos sin línea del banco; los en tránsito salen aparte en `enTransito`), `sinCategoria` (movimientos sin libro o con partidas en «otros») y `noCuadran` (facturas contra el pago, partidas contra líneas). Un movimiento sin documento no es pendiente: solo necesita su categoría. Las facturas se vinculan a un movimiento hasta 35 días después y con ±₡1; los pagos, con ventana de -3/+10 días y monto exacto.
* **Preguntas cruzadas** (productos comprados, gasto por línea de factura, etc.): `bancos_sql` con un SELECT (tablas en su descripción). Los totales oficiales siguen saliendo de `movimientos`.

## Corregir

* Categoría (`tipo` o `partidas`), `de`, comercio, nota, referencia o `fechaReal` de un movimiento: `bancos_editar_movimiento { id, ..., confirm: true }` (la categoría se guarda en su libro).
* Fecha, total o clase: `bancos_borrar_movimiento { id, confirm: true }` → volver a anotar (su libro con documento o facturas queda sin movimiento y se vuelve a vincular; uno sin documento se borra con él).
* Vínculo equivocado: `bancos_vincular { detalle: "l-...", movimiento }` (o `movimiento: ""` para soltarlo; 1 a 1, mismo monto). Pago o factura mal anotado: `bancos_borrar_detalle` (un pago con facturas pide `conFacturas: true`) y volver a anotar.
* Nombre de un tipo: `bancos_renombrar_tipo` (el id no cambia).

## Privacidad

El repo es público: montos, comercios y archivos solo viven en `datos/`. No copiarlos a `ia/`, commits ni grupos de Telegram; solo al chat del dueño.

## Reportes en imagen

Para ver o mandar reportes por periodo (resumen, gastos por tipo, pago de tarjetas, pendientes, movimientos) está el dashboard de la Mini App, pestaña «💰 Finanzas»: cada reporte tiene «Enviar al chat» y llega como imagen legible. Preferirlo a mandar tablas por `telegram_grafico` cuando los montos o descripciones se cortan. Para abrirlo desde el celular sin Telegram: «mandame el dashboard» → `bancos_dashboard_enlace` (`para: yo` o `novia`) manda por WhatsApp un enlace de 1 hora; solo a esos dos números.
