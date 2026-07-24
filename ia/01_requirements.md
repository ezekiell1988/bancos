# 01 — Requisitos del Sistema

> Última actualización: 2026-07-18
> Fuente: definición directa de Ezequiel Baltodano.

## Objetivo

Consolidar movimientos de débito, crédito y préstamos para producir información contable familiar confiable y trazable.

## Requisitos funcionales

| ID | Requisito | Aceptación |
|---|---|---|
| REQ-001 | Cargar un PDF o Excel mensual desde navegador. | Se crea importación incremental sin requerir nombres ni carpetas predefinidos. |
| REQ-002 | Detectar formato desde contenido. | Selecciona plantilla conocida o registra caso desconocido revisable. |
| REQ-003 | Persistir importación incremental. | Crea/actualiza por huella única; nunca elimina automáticamente. |
| REQ-004 | Mantener auxiliares por cuenta, tarjeta y préstamo. | Cada auxiliar conserva saldos y movimientos conciliables. |
| REQ-005 | Clasificar cada movimiento. | Tipo contable determinista; categoría por regla, IA, categoría previa o `General`. |
| REQ-006 | Permitir reclasificación manual. | Corrección alimenta regla de misma cuenta y descripción normalizada. |
| REQ-007 | Mostrar pérdidas y ganancias. | Mes, comparación de dos meses y acumulado anual por categoría. |
| REQ-008 | Mostrar situación financiera. | Un mes seleccionado; activos, pasivos y capital consolidado. |
| REQ-009 | Mantener CRC y USD. | Cada movimiento guarda moneda original y ambas equivalencias. |
| REQ-010 | Gestionar tipos de cambio manuales. | Un tipo diario; si falta, usa último previo disponible. |
| REQ-011 | Generar diferencial cambiario mensual. | Solo pasivos USD; comprobante regenerable con encabezado y líneas por saldo/documento. |
| REQ-012 | Determinar auxiliar y propietario. | IBAN (`CR...`) es llave estable; si dueño no se infiere, asignar Ezequiel Baltodano. |
| REQ-013 | Cargar histórico inicial. | Saldos iniciales al 2025-12-31 y movimientos desde 2026-01-01. |
| REQ-014 | Administrar ciclos de tarjeta. | Conservar corte, periodo y pago agrupado por tarjeta para análisis; no sustituye contabilización mensual. |
| REQ-015 | Regenerar períodos afectados. | Importación marca cambios pendientes; usuario inicia job detallado que recalcula desde mes afectado. |
| REQ-016 | Alertar reportes desactualizados. | Dashboard conserva último cálculo y muestra advertencia hasta regeneración exitosa. |
| REQ-017 | Conciliar pagos N:N. | Proceso automático propone relaciones; usuario puede crear/corregir conciliación manual. |
| REQ-018 | Auditar cambios manuales. | Correcciones y eliminaciones conservan fecha, valores anterior/nuevo y acción. |
| REQ-019 | Completar tipo de cambio faltante. | Upload solicita tipo manual cuando no existe valor del día ni previo. |

## Reglas contables

* Débito/banco: activo contra ingreso o gasto.
* Crédito: compra aumenta pasivo; pago reduce pasivo contra activo.
* Préstamo: principal reduce pasivo; interés, comisiones y seguros son gasto cuando el documento lo identifica.
* Capital inicial es `activos - pasivos` al corte de primera carga. Luego cambia por resultados, aportes y retiros.
* Transferencias internas no son ingreso ni gasto.
* Diferencial cambiario usa una cuenta de gasto; resultado favorable reduce ese gasto.
* Julio 2026 permanece abierto hasta recibir corte y movimientos correspondientes.

## Requisito estratégico — Migración API → MCP

| ID | Requisito | Aceptación |
|---|---|---|
| REQ-020 | Migrar progresivamente cada feature de `Bancos.Api` a tools de `Bancos.Mcp`. | Cada feature migrada tiene tool(s) MCP equivalentes y los tests de la feature pasan en MCP. |
| REQ-021 | Eliminar `Bancos.Api` cuando `Bancos.Mcp` tenga paridad funcional completa. | El proyecto `Bancos.Api` y su base de datos se eliminan; solo `Bancos.Mcp` permanece. |

**Orden de migración sugerido** (de menor a mayor dependencia):

1. Catálogo y plantillas de importación — ya en MCP (`tbImportTemplates`, `tbBankAccounts`, etc.)
2. Parsers de formatos (PDF/Excel/CSV) — tools de extracción por formato
3. Tipos de cambio — tool de consulta y carga manual
4. Importación e idempotencia — tool de ingesta con huella
5. Movimientos y cortes de tarjeta — tools de consulta y carga
6. Clasificación — tools de reglas, categorías e IA
7. Préstamos — tools de extracto y cuotas
8. Contabilidad y diferencial cambiario — tools de libro mayor y FX
9. Reportes — tools de P&G y situación financiera
10. Eliminación de `Bancos.Api`

## Fuera de alcance inicial

* Autenticación y despliegue Azure.
* Activos no bancarios; efectivo solo como opción manual futura.
* Diferencial cambiario de activos USD.
* Descarga automática de tipos de cambio.
