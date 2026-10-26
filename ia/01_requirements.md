# 01 — Requisitos del Sistema

> Última actualización: 2026-10-26
> Fuentes: pedidos del dueño por Telegram (2026-10-25 y 2026-10-26)

## Propósito del sistema

Llevar la contabilidad de la familia Baltodano Soto: los ingresos y gastos mensuales, detallados y agrupados por tipo, a partir de los documentos bancarios y facturas que el dueño le pasa al agente.

* **Hogar:** el dueño (Ezequiel Baltodano Cubillo) y su pareja (Karen Tatiana Soto Arce), que viven juntos. Es lo que se analiza primero.
* **Papá:** Ezequiel Baltodano Baltodano vive en otra casa y depende económicamente del dueño. Sus ingresos y gastos son un rubro más: entran en los reportes por defecto, se pueden excluir con una opción y se puede sacar un reporte solo de él para mostrárselo.

El detalle va en 3 niveles; solo el nivel 1 cuenta en los reportes:

| Nivel | Qué es | Origen | Para qué |
|-------|--------|--------|----------|
| 1. Movimiento bancario | Cada línea del estado de cuenta | HTML de movimientos del BCR, PDF del estado de la tarjeta BAC | Base de verdad: todos los reportes y totales salen de aquí |
| 2. Comprobante | Pago: documento del momento con el número de movimiento del banco | SINPE, voucher de una compra, transferencia | Auxiliar: dice qué fue un movimiento (tipo, de quién, detalle) y qué factura paga |
| 3. Factura | Documento cobrado: detalle de productos o cargos | Compras de supermercado, recibos de servicios públicos | Auxiliar: detalla lo que se compró; se paga con uno o varios comprobantes |

Un comprobante (nivel 2) es el pago de una factura (nivel 3). La relación no es 1 a 1: una factura se puede pagar en varios pagos (abonos, cuotas) y un mismo pago puede cubrir varias facturas.

## Roles de usuario

| Rol | Objetivo | Permisos o restricciones |
|-----|----------|-------------------------|
| Dueño | Pasar documentos y facturas; consultar y analizar ingresos y gastos | Dueño de los datos; sus datos son privados |
| Pareja (Karen) | Pasar documentos y consultar gastos del hogar desde el grupo de Telegram | Solo dentro del grupo del proyecto; permisos por definir en la Fase 4 |
| Papá | Ver sus propios gastos | No usa el sistema; el dueño le muestra un reporte solo con lo suyo |
| Agente | Leer los documentos y anotarlos en el nivel que corresponde | Escribe solo en `datos/` y con el MCP `bancos` |

## Invariantes del negocio

* Los reportes y totales salen solo del nivel 1 (movimientos bancarios). Los niveles 2 y 3 nunca suman.
* La fecha de cada movimiento es la de proceso del banco (ADR-02).
* Un comprobante o factura que todavía no tiene su movimiento en un estado de cuenta queda pendiente y no entra en los reportes.
* Cada movimiento es `gasto` o `ingreso` y su `tipo` existe en el catálogo de su clase.
* Sin `de`, un movimiento es del hogar; `de: "papa"` (u otra persona) lo separa como rubro. Ningún reporte excluye nada por defecto.
* Una factura se paga con 0, 1 o varios comprobantes; su saldo es su total menos lo pagado. Solo los movimientos de esos pagos suman en reportes, nunca la factura.
* Los jsonl son la fuente de verdad; la base SQLite se reconstruye desde ellos.
* Los datos financieros nunca salen de `datos/` ni entran a git.

## Requisitos funcionales

| ID | Requisito | Señal de aceptación | Estado |
|----|-----------|---------------------|--------|
| REQ-001 | Anotar los movimientos de un estado de cuenta en un jsonl por día, dentro de una carpeta por mes | Tras pasar un documento existe la línea en `datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl`; repetir el documento no duplica | ✅ Hecho |
| REQ-002 | Guardar el archivo de respaldo (imagen, PDF, HTML, Excel) en `datos/multimedia/AAAA-MM/` y referenciarlo en lo anotado, en los 3 niveles | El campo `archivo` apunta a un archivo que existe | ✅ Hecho |
| REQ-003 | Clasificar cada movimiento como gasto o ingreso con un tipo de `tipos-gasto.jsonl` o `tipos-ingreso.jsonl`, y poder agregar tipos nuevos | Un tipo desconocido se rechaza; uno nuevo queda en el catálogo | ✅ Hecho |
| REQ-004 | Separar por cuenta: movimientos del BCR en `bcr/` y de la tarjeta BAC en `bac/` | Cada movimiento queda en la carpeta de su cuenta | ✅ Hecho |
| REQ-005 | Consultar movimientos y el resumen de un mes (totales por clase, tipo y moneda) | El agente responde cuánto se gastó o ingresó en un mes sin leer los jsonl a mano | ✅ Hecho |
| REQ-006 | Corregir un movimiento mal anotado | Se puede editar o borrar una línea puntual y volver a anotarla | ✅ Hecho |
| REQ-007 | Anotar y consultar movimientos desde un grupo de Telegram con el bot @MacEzequielBot, primero solo con el dueño y después con su pareja | Un documento mandado al grupo queda anotado con el MCP `bancos` y el bot responde en el grupo | Pendiente |
| REQ-008 | Llevar al papá (y a otras personas) como rubro aparte con `de`: incluido por defecto, excluible con una opción y con reporte propio | El resumen del mes muestra el hogar por tipo y al papá como rubro; con `excluirDe: ["papa"]` queda solo el hogar; con `de: "papa"` solo lo del papá | ✅ Hecho |
| REQ-009 | Anotar comprobantes (nivel 2: SINPE, voucher) con su número de movimiento del banco y vincularlos a su movimiento de nivel 1, que toma de ahí el tipo, el `de` y el detalle | Un comprobante queda en `datos/comprobantes/`; al llegar el estado se vincula solo (por referencia o por monto y fecha) y no duplica el movimiento | ✅ Hecho |
| REQ-010 | Anotar facturas (nivel 3) con el detalle de sus líneas y vincularlas a su movimiento de nivel 1 | Una factura queda en `datos/facturas/` con sus líneas y el id del movimiento | ✅ Hecho |
| REQ-011 | Tener una base SQLite con los 3 niveles para consultas, reconstruida desde los jsonl | `datos/bancos.sqlite` tiene movimientos, comprobantes, facturas y sus líneas; una consulta de solo lectura responde | ✅ Hecho |
| REQ-012 | Saber qué movimientos no tienen comprobante ni factura, y qué comprobantes o facturas no tienen movimiento | Una consulta del MCP lista ambos para un mes | ✅ Hecho |
| REQ-013 | Relacionar cada factura (nivel 3) con los comprobantes (nivel 2) que la pagan, sin limitarse a 1 a 1: una factura con varios pagos y un pago con varias facturas | Una factura acepta varios comprobantes; el MCP muestra lo pagado y el saldo, y lista las facturas con saldo pendiente | Pendiente |

## Flujos principales

### Flujo: anotar un estado de cuenta (nivel 1)

Estado inicial: el dueño manda el HTML del BCR o el PDF de la BAC
Estado final: movimientos anotados, vinculados a sus comprobantes y facturas pendientes, y respaldo guardado

1. El agente lee cada línea: fecha del banco, referencia, clase, tipo, total, moneda, comercio y `de`.
2. Si una línea no deja claro el tipo, mira si hay un comprobante o factura pendiente que lo aclare; si no, pregunta una sola cosa con opciones.
3. Anota con el MCP `bancos` (preview y `apply: true`); los comprobantes y facturas pendientes se vinculan solos.
4. Le responde al dueño qué anotó (cantidad, totales y vinculados).

### Flujo: anotar un comprobante o factura (niveles 2 y 3)

1. El agente lee el comprobante (monto, fecha, referencia, destinatario) o la factura (comercio, total y líneas).
2. Los anota con el MCP `bancos`; si su movimiento ya está, se vinculan y le completan el tipo y el detalle; si no, quedan pendientes.

### Flujo: reporte del mes

1. Resumen de todo: hogar por tipo y papá como rubro.
2. Opcional: excluir al papá, o solo el papá para mostrárselo.

## Fuera de alcance

* Servidor de base de datos, interfaz web o app.
* Carga de estados de cuenta por API (lo viejo queda en la rama `legacy`).
