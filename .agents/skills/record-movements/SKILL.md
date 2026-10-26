---
name: record-movements
description: >
  Anotar gastos e ingresos del dueño a partir de documentos bancarios y facturas (foto, PDF, Excel,
  comprobante SINPE, estado de cuenta BCR o de la tarjeta BAC, salario de ITQS o ClickEat) con el MCP
  bancos, guardando el respaldo en datos/multimedia. Usar también para corregir un movimiento o
  responder cuánto se gastó o ingresó en un mes. Triggers: factura, gasto, ingreso, comprobante,
  SINPE, estado de cuenta, BCR, BAC, tarjeta, salario, anotá, registrá, cuánto gasté, resumen del mes.
---

# Anotar movimientos

Todo se escribe con el MCP `bancos`; nunca editar los jsonl a mano. Formato y carpetas: `README.md` del repo.

## Procedimiento

1. **Leer el documento.** Los archivos de Telegram llegan a
   `~/Documents/ezekl/telegram-agent-remote/state/inbox/<chat>/`. Imagen o PDF: leerlo con Read;
   Excel: leerlo con un script corto de Node o Python (`-I`), solo las columnas de fecha, detalle y monto.
2. **Decidir cada campo:**

   | Campo | Regla |
   |-------|-------|
   | `cuenta` | Tarjeta BAC → `bac`. Cuenta BCR (débito, SINPE, transferencias, salarios) → `bcr`. Factura suelta sin medio de pago visible → preguntar |
   | `clase` | Cargo, compra, débito → `gasto`. Depósito, crédito, salario → `ingreso` |
   | `tipo` | El más cercano de `bancos_tipos`; si ninguno sirve, `otros` y avisarle. Tipo nuevo solo si el dueño lo pide (`bancos_agregar_tipo`) |
   | `fecha` | La de la transacción, no la del estado de cuenta (AAAA-MM-DD) |
   | `total` / `moneda` | Monto positivo; `₡` → `CRC`, `$` → `USD`. En facturas, el total pagado (con IVA y servicio) |
   | `comercio` / `nota` | Comercio o persona tal como aparece, corto; la nota solo si aporta (p. ej. "almuerzo equipo") |

3. **Casos especiales:**
   * Salario: `ingreso` en `bcr`, tipo `salario-itqs` o `salario-clickeat` según quién paga.
   * Pago de la tarjeta BAC desde el BCR: `gasto` en `bcr` con tipo `tarjeta` (los consumos ya están en `bac`; al hablar de gasto real, excluir `tarjeta`).
   * Estado de cuenta con muchas líneas: un solo `bancos_registrar` con todos los movimientos de esa cuenta y el estado como `origen`.
4. **Si algo no está claro** (cuenta, clase o una fecha ilegible): preguntar una sola cosa, con opciones y una recomendada, antes de anotar.
5. **Anotar:** `bancos_registrar` sin `apply` (preview) → revisar totales, archivos y `omitidosPorDuplicado` → repetir con `apply: true` y `origen` = ruta del archivo recibido. El respaldo se copia solo a `datos/multimedia/AAAA-MM/`.
6. **Responder corto:** cuántos movimientos, cuenta, total por moneda y los omitidos por duplicado.

## Corregir y consultar

* Corregir: `bancos_movimientos` (da `cuenta`, `fecha` y `linea`) → `bancos_borrar_movimiento` con `confirm: true` → volver a anotar con `bancos_registrar`.
* ¿Cuánto gasté o ingresé?: `bancos_resumen` con `mes` (y `cuenta` si la nombra). Detalle: `bancos_movimientos` con filtros.

## Privacidad

El repo es público: montos, comercios y archivos solo viven en `datos/`. No copiarlos a `ia/`, commits ni grupos de Telegram; solo al chat del dueño.
