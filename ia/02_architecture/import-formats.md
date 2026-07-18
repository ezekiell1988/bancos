# 02 — Formatos de importación detectados

> Última actualización: 2026-07-18
> Alcance: primera carga bajo `src/input/`; sin valores ni identificadores financieros.

## Regla de selección

El detector recibe bytes, normaliza texto y evalúa firmas ordenadas. Ruta y nombre de archivo son metadatos no confiables.

| Prioridad | Plantilla | Firma mínima | Salida |
|---|---|---|---|
| 1 | `bcr-debit-csv-v1` | CSV `;` con `oficina`, `fechaMovimiento`, `numeroDocumento`, `debito`, `credito`, `descripcion` | Movimientos de cuenta débito. |
| 2 | `bac-credit-csv-v1` | CSV `,` con `Product`, `Name`, `Date`, pagos mínimo/contado CRC y USD | Resumen/saldos de tarjeta; no asumir que cada fila es movimiento contable. |
| 3 | `bcr-debit-html-xls-v1` | HTML con `Banco de Costa Rica` y `Movimientos por rango de fechas` | Movimientos de cuenta débito. |
| 4 | `bac-credit-financing-xls-v1` | XLS BIFF/OLE con `Consulta de Financiamientos`, `Fecha`, `Concepto`, `Cuotas`, `Monto de cuota`, `Saldo inicial`, `Saldo faltante` | Financiamientos asociados a tarjeta; auxiliar de pasivo. |
| 5 | `bac-credit-online-pdf-v1` | PDF con `Tarjeta de crédito`, `Saldo en colones`, `Saldo en dólares`, `Pago de tarjeta al día` | Snapshot de tarjeta y validación de auxiliar; no hay evidencia suficiente de detalle de movimientos. |
| 6 | `coopealianza-loan-pdf-v1` | PDF con `Ver detalles del préstamo` y tabla `CAPITAL`, `INTERÉS`, `MORA`, `OTROS`, `TOTAL`, `SALDO` | Préstamo: saldo, cuota y pagos desglosados. |
| 7 | `unknown` | Ninguna firma única o varias coincidencias | No contabilizar; crear importación `Pendiente de plantilla`. |

## Campos normalizados

| Concepto | Fuente esperada |
|---|---|
| Fecha de movimiento | CSV/HTML/PDF de movimientos o pagos. |
| Referencia externa | Documento/transacción si formato lo entrega. |
| Descripción | Campo de descripción o concepto. |
| Débito / crédito | Columnas explícitas, nunca inferidas por signo textual. |
| Saldo inicial y final | Encabezado o snapshot; validar cuando ambos existan. |
| Moneda | Marca explícita CRC/USD; si no existe, plantilla debe definirla. |
| Principal, interés, mora y otros | Tabla de pagos de préstamo. |

## Validación por plantilla

* CSV débito: columnas obligatorias, fecha válida, una sola dirección por movimiento y consistencia de filas.
* CSV crédito: conservar filas originales estructuradas y marcar el significado contable hasta confirmar si contiene movimientos o resumen de pagos.
* HTML/XLS: rechazar si no hay tabla de movimientos identificable; XLS BIFF se lee con biblioteca que soporte formato binario, no por extensión.
* PDF tarjeta: extraer snapshot solo si encuentra moneda y saldo; nunca crear compras si no están en tabla de movimientos.
* PDF préstamo: cada pago debe cumplir `capital + interés + mora + otros = total` cuando campos estén disponibles; saldo posterior debe ser coherente.
* Toda importación: deduplicación por auxiliar, fecha, referencia, descripción normalizada, importes y moneda. Si falta referencia, registrar menor confianza y pedir revisión si hay colisión.

## Cobertura observada

La primera carga contiene CSV delimitado, XLS binario BIFF/OLE, XLS basado en HTML y PDF de dos a cuatro páginas. Hay múltiples muestras de los formatos BAC y Coopealianza; se utilizarán como fixtures anonimizados durante implementación. Los archivos de muestra no se moverán ni se modificarán.

## Gaps que requieren implementación o confirmación

1. Extraer encabezados reales del XLS binario usando biblioteca .NET durante `TASK-EZ-BE-01`.
2. Confirmar semántica exacta de filas de `bac-credit-csv-v1` con una importación de prueba no persistente.
3. Crear fixture anonimizado o prueba de contrato para cada plantilla, sin versionar documentos reales.
