> **Última actualización:** 2026-07-20 CR (progreso actualizado)



## Completado

* **2026-07-20** — **Post-cierre TASK-EBC-FE-08 — Issues resueltos (2026-07-21)**

1. **Proxy de Angular no activo:** `angular.json` no referenciaba `proxy.conf.json` en el target `serve`. Se agregó `"options": { "proxyConfig": "proxy.conf.json" }` — sin esto, todas las llamadas a `/api/*` retornaban el `index.html` en vez de llegar al backend.

2. **Proxy apuntaba al puerto incorrecto:** `proxy.conf.json` tenía target `https://localhost:5001` pero el backend corre en `http://localhost:8000`. Corregido a `http://localhost:8000`.

3. **`ImportStatus` enum serializado como número:** El backend devolvía `status: 2` en vez de `"Completed"`, causando `TypeError: n.status.toLowerCase is not a function` en la página de importaciones. Corregido agregando `JsonStringEnumConverter` vía `ConfigureHttpJsonOptions` en `Program.cs`. — EBC

* **2026-07-20** — TASK-EBC-FE-11: Página /loans implementada con endpoint BE GET /api/loans y feature Angular standalone. Muestra financiamientos BAC activos (OutstandingBalance > 0), préstamos Coopealianza con cuota del último pago, y total mensual CRC consolidado en el encabezado. Link "Préstamos" agregado al nav principal. — EBC

* **2026-07-20** — TASK-EBC-FE-10: Página /reports/balance-sheet implementada con endpoint GET /api/reports/balance-sheet (activos por suma de Transactions en cuentas Asset, pasivos por último CardStatement + CreditFinancings + LoanStatements) y componente Angular BalanceSheetPage. Patrimonio neto visible al final. — E

* **2026-07-20** — TASK-EBC-FE-09: Endpoint GET /api/reports/income-statement?year=&month= implementado en ReportsModule. Feature Angular /reports/income-statement con selector mes/año, tablas de ingresos y gastos agrupados por categoría, y resultado neto (superávit/déficit). Ruta registrada en app.routes.ts. Build Angular limpio con chunk income-statement-page generado. — EBC

* **2026-07-20** — TASK-EBC-FE-08: Página /categories implementada con filtro por categoría y descripción en tiempo real, total del filtro en encabezado, dropdown inline para reclasificar y PATCH que persiste en BD. Endpoints GET /api/transactions y PATCH /api/transactions/{id}/category añadidos en TransactionsModule. Nav link agregado en app.ts. — EBC

* **2026-07-18** — TASK-EBC-FE-07: La confirmación de carga diferencia archivos ya importados de fallos reales. La protección de huellas permanece activa y los duplicados se conservan visibles en la cola. — EBC

* **2026-07-18** — TASK-EBC-FE-06: Se normalizó el proxy local: /api mantiene endpoints de aplicación y /_api enruta infraestructura con soporte WebSocket. Health y Hangfire se movieron a /_api/health y /_api/hangfire; se dejó /hubs listo para hubs futuros. — EBC

* **2026-07-18** — TASK-EBC-FE-05: Se restauró la lista completa de entradas ZIP, el selector por archivo pendiente y el conteo exacto de archivos listos. — EBC

* **2026-07-18** — TASK-EBC-FE-04: Se crearon tokens CSS semánticos globales y se separaron los estilos de App e Imports mediante styleUrl. — EBC

* **2026-07-18** — ISSUE-003 resuelto: Se definieron tokens semánticos en :root y se trasladaron los estilos de App e Imports a sus hojas CSS asociadas.

* **2026-07-18** — TASK-EBC-FE-03: Se eliminó el límite fijo del formulario de importaciones y se habilitó ancho fluido dentro del contenedor. — EBC

* **2026-07-18** — ISSUE-002 resuelto: El formulario ahora usa inline-size: 100% dentro del contenedor principal.

* **2026-07-18** — TASK-EBC-FE-02: Pantalla de importaciones renovada con arrastrar y soltar múltiples archivos, cola de carga y auxiliares disponibles. — EBC

* **2026-07-18** — TASK-EBC-FE-01: Dashboard Angular standalone minimalista por features para importaciones y revisión, servido por la API, con proxy HTTPS y arranque integrado en modo watch. — EBC
