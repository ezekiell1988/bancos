# 01 — Requisitos del Sistema

> Última actualización: 2026-10-24
> Fuente: definición directa de Ezequiel Baltodano.

## Objetivo

Consolidar movimientos de débito, crédito y préstamos para producir información contable familiar confiable y trazable.

## Requisitos funcionales

Cada fase de `03_plan.md` dice de qué REQ sale (`> Requisitos: …`); el estado de un REQ es el de sus fases.

| ID | Requisito | Señal de aceptación | Estado |
|---|---|---|---|
| REQ-001 | Cargar un PDF o Excel mensual mediante una tool MCP. | Se crea importación incremental sin requerir nombres ni carpetas predefinidos. | En curso (Fase 2) |
| REQ-002 | Detectar formato desde contenido. | Selecciona plantilla conocida o registra caso desconocido revisable. | En curso (Fase 2) |
| REQ-003 | Persistir importación incremental. | Crea/actualiza por huella única; nunca elimina automáticamente. | En curso (Fase 2) |
| REQ-004 | Mantener auxiliares por cuenta, tarjeta y préstamo. | Cada auxiliar conserva saldos y movimientos conciliables. | En curso (Fase 2) |
| REQ-005 | Clasificar cada movimiento. | Primero reglas .NET deterministas; si no resuelven, Azure AI; si no hay confianza suficiente, `No clasificado`. | Pendiente (Fase 3) |
| REQ-006 | Permitir reclasificación manual aprendible. | La corrección conserva auditoría y crea o actualiza una regla reutilizable por cuenta, descripción normalizada y contexto. | Pendiente (Fase 3) |
| REQ-007 | Generar estado de resultados HTML. | Tool MCP devuelve HTML autocontenido de ingresos versus gastos por período, comparación y acumulado anual por categoría. | Pendiente (Fase 4) |
| REQ-008 | Generar situación financiera HTML. | Tool MCP devuelve HTML autocontenido de activos, pasivos y capital para un período o fecha. | Pendiente (Fase 4) |
| REQ-009 | Mantener CRC y USD. | Cada movimiento guarda moneda original y ambas equivalencias. | En curso (Fase 2) |
| REQ-010 | Gestionar tipos de cambio manuales. | Un tipo diario; si falta, usa último previo disponible. | En curso (Fase 2) |
| REQ-011 | Generar diferencial cambiario mensual. | Solo pasivos USD; comprobante regenerable con encabezado y líneas por saldo/documento. | Pendiente (Fase 4) |
| REQ-012 | Determinar auxiliar y propietario. | IBAN (`CR...`) es llave estable; si dueño no se infiere, asignar Ezequiel Baltodano. | En curso (Fase 2) |
| REQ-013 | Cargar histórico inicial. | Saldos iniciales al 2025-12-31 y movimientos desde 2026-01-01. | En curso (Fase 2) |
| REQ-014 | Administrar ciclos de tarjeta. | Conservar corte, periodo y pago agrupado por tarjeta para análisis; no sustituye contabilización mensual. | En curso (Fase 2) |
| REQ-015 | Regenerar períodos afectados. | Importación marca cambios pendientes; usuario inicia job detallado que recalcula desde mes afectado. | Pendiente (Fase 4) |
| REQ-016 | Alertar reportes desactualizados. | Las tools de reporte devuelven fecha de último cálculo y advertencia hasta regeneración exitosa. | Pendiente (Fase 4) |
| REQ-017 | Conciliar pagos N:N. | Proceso automático propone relaciones; usuario puede crear/corregir conciliación manual. | Pendiente (Fase 4) |
| REQ-018 | Auditar cambios manuales. | Correcciones y eliminaciones conservan fecha, valores anterior/nuevo y acción. | Pendiente (Fase 3) |
| REQ-019 | Completar tipo de cambio faltante. | Upload solicita tipo manual cuando no existe valor del día ni previo. | En curso (Fase 2) |

## Reglas contables

* Débito/banco: activo contra ingreso o gasto.
* Crédito: compra aumenta pasivo; pago reduce pasivo contra activo.
* Préstamo: principal reduce pasivo; interés, comisiones y seguros son gasto cuando el documento lo identifica.
* Capital inicial es `activos - pasivos` al corte de primera carga. Luego cambia por resultados, aportes y retiros.
* Transferencias internas no son ingreso ni gasto.
* Diferencial cambiario usa una cuenta de gasto; resultado favorable reduce ese gasto.
* Julio 2026 permanece abierto hasta recibir corte y movimientos correspondientes.

## Requisito estratégico — Operación exclusiva mediante MCP

| ID | Requisito | Señal de aceptación | Estado |
|---|---|---|---|
| REQ-020 | Operar todo el producto mediante `Bancos.Mcp`. | Cada caso de uso funcional está disponible mediante tools MCP y probado en `Bancos.Mcp`. | Pendiente (Fase 5) |
| REQ-021 | Mantener el LLM dentro de un flujo controlado. | Las tools devuelven resultados explicables, piden confirmación para cambios manuales y no exponen datos sensibles innecesarios. | Pendiente (Fase 5) |

**Orden de implementación sugerido** (de menor a mayor dependencia):

1. Carga y cierres — ya disponibles o en revisión en MCP.
2. Categorías, reglas e historial de clasificación.
3. Tools de clasificación determinista y revisión manual.
4. Azure AI como fallback seguro.
5. Contabilidad y diferencial cambiario.
6. Reportes HTML de resultados y situación financiera.

## Fuera de alcance inicial

* Autenticación y despliegue Azure.
* Activos no bancarios; efectivo solo como opción manual futura.
* Diferencial cambiario de activos USD.
* Descarga automática de tipos de cambio.
