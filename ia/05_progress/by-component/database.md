> **Última actualización:** 2026-07-20 CR (TASK-EBC-MCP-06 completada)



## Completado

* **2026-07-20** — TASK-EBC-MCP-06: Se consolidaron las tres migraciones del catálogo MCP en una única migración inicial que crea directamente las tablas tbImportTemplates y tbImportTemplatePatterns. La base local fue reconstruida con este historial limpio. — EBC

* **2026-07-20** — TASK-EBC-MCP-05: Se eliminó la base local vacía con el script reutilizable Reset-McpCatalogDatabase.ps1 y se reconstruyó mediante migraciones EF. El catálogo quedó en las tablas tbImportTemplates y tbImportTemplatePatterns con los nueve seeds y timestamps de Costa Rica. — EBC

* **2026-07-20** — TASK-EBC-DB-06: aplicada la migración de identidad bancaria; creados los dos responsables y los 13 auxiliares con IBAN, banco, moneda y responsable. Pendiente: reasignar imports históricos que aún apuntan a auxiliares genéricos y validar cobertura por formato. — EBC

* **2026-07-20** — TASK-EBC-DB-06: Auditoría completada. Se identificaron 8 templates activos en Imports, todos apuntando a 2 auxiliares genéricos sin IBAN ni owner real. Se acordó con el usuario crear 13 AccountAuxiliaries (4 débito, 8 crédito BAC con 2 auxiliares por tarjeta CRC+USD, 1 préstamo Coopealianza) y 2 owners nuevos (Ezequiel Baltodano y Karen Soto para BCR Débito Compartida). Próximo paso: generar y aplicar SQL de inserción. — EBC

* **2026-07-20** — TASK-EBC-INF-08 (cambios de BD durante validación de importación masiva):
  * **Migración `SeedGeneralCategory`**: Inserta la categoría "General" con ID fijo `00000000-0000-0000-0000-000000000301` como dato semilla. Razón: el índice único `(Name, ParentId)` en `Categories` solo aplica cuando `ParentId IS NOT NULL`. Múltiples workers concurrentes podían insertar duplicados de "General" (todos con `ParentId NULL`), causando fallo en `SingleOrDefaultAsync`. Con el seed, la categoría siempre existe desde el inicio.
  * **Exchange rates USD**: La tabla `ExchangeRates` debe tener datos para el rango de fechas de los archivos a importar. Se insertaron 92 filas para mayo-julio 2026 a 519.50 CRC/USD. Sin estos datos, los imports de tarjeta de crédito BAC con transacciones en USD fallan. Ver script en [DEV-ENV-local-sqlserver.md](../../06_decisions/DEV-ENV-local-sqlserver.md).
  * **Validación final de tablas**: 1,398 Transactions · 9 CreditFinancings · 1 LoanStatement (OutstandingBalance=₡4,372,249.85) · 10 LoanPayments · 20 Categories (1 General). — EBC

* **2026-07-18** — TASK-EBC-DB-04: Se aplicó la migración AddImportProgress y se recuperaron de forma controlada las ocho importaciones existentes que habían quedado detenidas por el esquema faltante. El listado volvió a responder; no se reenviaron ni duplicaron archivos, huellas ni movimientos. — EBC

* **2026-07-18** — TASK-EBC-DB-03: Se deshabilitaron los reintentos automáticos para ImportJobs mediante AutomaticRetry Attempts=0 y se aplicó la migración pendiente AddTransactionOperationType a dbbancos. — EBC

* **2026-07-18** — ISSUE-001 resuelto: El contexto de datos ahora crea un propietario predeterminado, el catálogo base de cuentas y dos auxiliares neutrales. El flujo de pre-revisión resuelve automáticamente un auxiliar compatible según plantilla y tipo de cuenta, por lo que la carga ya no queda bloqueada por esa selección. Se validó con compilación, pruebas automatizadas y QA de interfaz.

* **2026-07-18** — TASK-EBC-DB-02: Base reinicializada con una única migración inicial que incluye catálogo contable y auxiliares semilla no sensibles. — EBC

* **2026-07-18** — TASK-EBC-DB-01: Se aplicó y verificó la migración inicial InitialCreate en dbbancos sin insertar datos de negocio. — EBC
