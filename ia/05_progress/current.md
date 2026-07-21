# Progreso actual

> **Última actualización:** 2026-07-20 CR (progreso actualizado)

## En curso

* Descubrimiento de requisitos financieros.
* Firmas de siete plantillas documentadas; faltan validación de XLS binario y semántica de CSV de crédito durante implementación.

## Completado en sesión actual

* Creado `/ia`: contexto, requisitos, arquitectura, plan, tareas, progreso, ADRs, issues, retrospectiva, templates, prompts y skills de workflow.
* Inspeccionados formatos de primera carga de forma anonimizada y documentados detectores/validaciones.
* Configurados MCP `iaWorkflow` y `dbquery`; smoke tests completos y configuración de Codex/VS Code/Claude actualizada.

## Próximo

* Completar preguntas de requisitos.
* Auditar estructura `/ia`.
* Abrir una sesión nueva de Codex para cargar MCP nativos.
* Aprobar y ejecutar `TASK-EZ-BE-01` mediante `iaWorkflow`.

## Completado en sesiones recientes

* **2026-07-20** — TASK-EBC-DB-06: aplicada la migración de identidad bancaria; creados los dos responsables y los 13 auxiliares con IBAN, banco, moneda y responsable. Pendiente: reasignar imports históricos que aún apuntan a auxiliares genéricos y validar cobertura por formato. — EBC

* **2026-07-20** — TASK-EBC-INF-09 cerrada: Configurado VS Code con launch de dotnet watch para la API en el puerto 8000, Angular ng serve HTTPS en el 4200 y compound Full Stack. — EBC

* **2026-07-20** — TASK-EBC-DB-06: Auditoría completada. Se identificaron 8 templates activos en Imports, todos apuntando a 2 auxiliares genéricos sin IBAN ni owner real. Se acordó con el usuario crear 13 AccountAuxiliaries (4 débito, 8 crédito BAC con 2 auxiliares por tarjeta CRC+USD, 1 préstamo Coopealianza) y 2 owners nuevos (Ezequiel Baltodano y Karen Soto para BCR Débito Compartida). Próximo paso: generar y aplicar SQL de inserción. — EBC

* **2026-07-20** — TASK-EBC-BE-24 cerrada: Parser BN implementado. BnCardStatementPdfParser.cs maneja texto concatenado de PdfPig. Detecta automáticamente bn-card-statement-pdf-v1. Persiste CardStatement (corte ₡210,829 + $6.49 contado, pago mínimo ₡5,000 + $6.49, vence 03/08/2026), 19 Transactions (2 pagos + 17 compras) y 3 CreditFinancings activos (BN Marchamos 12M ₡32,334 y ₡46,324; Compras 6M ₡26,142). — EBC

* **2026-07-20** — **Post-cierre TASK-EBC-FE-08 — Issues resueltos (2026-07-21)**

1. **Proxy de Angular no activo:** `angular.json` no referenciaba `proxy.conf.json` en el target `serve`. Se agregó `"options": { "proxyConfig": "proxy.conf.json" }` — sin esto, todas las llamadas a `/api/*` retornaban el `index.html` en vez de llegar al backend.

2. **Proxy apuntaba al puerto incorrecto:** `proxy.conf.json` tenía target `https://localhost:5001` pero el backend corre en `http://localhost:8000`. Corregido a `http://localhost:8000`.

3. **`ImportStatus` enum serializado como número:** El backend devolvía `status: 2` en vez de `"Completed"`, causando `TypeError: n.status.toLowerCase is not a function` en la página de importaciones. Corregido agregando `JsonStringEnumConverter` vía `ConfigureHttpJsonOptions` en `Program.cs`. — EBC

* **2026-07-20** — TASK-EBC-FE-11 cerrada: Página /loans implementada con endpoint BE GET /api/loans y feature Angular standalone. Muestra financiamientos BAC activos (OutstandingBalance > 0), préstamos Coopealianza con cuota del último pago, y total mensual CRC consolidado en el encabezado. Link "Préstamos" agregado al nav principal. — EBC

* **2026-07-20** — TASK-EBC-FE-10 cerrada: Página /reports/balance-sheet implementada con endpoint GET /api/reports/balance-sheet (activos por suma de Transactions en cuentas Asset, pasivos por último CardStatement + CreditFinancings + LoanStatements) y componente Angular BalanceSheetPage. Patrimonio neto visible al final. — E

* **2026-07-20** — TASK-EBC-FE-09 cerrada: Endpoint GET /api/reports/income-statement?year=&month= implementado en ReportsModule. Feature Angular /reports/income-statement con selector mes/año, tablas de ingresos y gastos agrupados por categoría, y resultado neto (superávit/déficit). Ruta registrada en app.routes.ts. Build Angular limpio con chunk income-statement-page generado. — EBC

* **2026-07-20** — TASK-EBC-FE-08 cerrada: Página /categories implementada con filtro por categoría y descripción en tiempo real, total del filtro en encabezado, dropdown inline para reclasificar y PATCH que persiste en BD. Endpoints GET /api/transactions y PATCH /api/transactions/{id}/category añadidos en TransactionsModule. Nav link agregado en app.ts. — EBC

* **2026-07-20** — TASK-EBC-BE-23 cerrada: Parser, entidad, migración y handler implementados. Se importaron correctamente 4 CardStatements del PDF consolidado BAC julio 2026. Template detectado automáticamente con firma content-based. Upsert por (AccountAuxiliaryId, CardNumberMasked, StatementDate) funcional. — EBC

* **2026-07-20** — TASK-EBC-INF-08 cerrada: SQL Server 2022 en Docker configurado y validado. Los 19 archivos de src/input.zip se importaron correctamente. Bugs corregidos: SingleOrDefaultAsync→FirstOrDefaultAsync en ClassificationModule, seed de categoría General, ChangeTracker.Clear()+Attach en catch blocks para fallo correcto, BalanceRegex con \\s para non-breaking spaces de PdfPig, race condition en LoanStatements con re-attach del Import. Nuevo endpoint POST /api/imports/{id}/retry. — EBC

* **2026-07-20** — TASK-EBC-INF-07 cerrada: SQL Server local levantado en Docker (puerto 1433), migraciones EF aplicadas, db.json apunta a localhost. appsettings.Development.json limpiado de credenciales hardcodeadas. Procedimiento documentado en ia/06_decisions/DEV-ENV-local-sqlserver.md. — EBC

* **2026-07-18** — TASK-EBC-FE-07 cerrada: La confirmación de carga diferencia archivos ya importados de fallos reales. La protección de huellas permanece activa y los duplicados se conservan visibles en la cola. — EBC

* **2026-07-18** — TASK-EBC-BE-20 cerrada: El parser de estados BAC distingue resúmenes de pago y snapshots sin tabla de movimientos; los deriva a revisión manual segura sin generar movimientos sintéticos. Tras corregir el esquema de progreso, los ocho trabajos afectados finalizaron sin errores de infraestructura. — EBC

* **2026-07-18** — TASK-EBC-DB-04 cerrada: Se aplicó la migración AddImportProgress y se recuperaron de forma controlada las ocho importaciones existentes que habían quedado detenidas por el esquema faltante. El listado volvió a responder; no se reenviaron ni duplicaron archivos, huellas ni movimientos. — EBC

* **2026-07-18** — TASK-EBC-BE-19 cerrada: Se clasificaron los fallos de importación: los ocho jobs fallidos corresponden a estados de tarjeta sin movimientos detallados. Las validaciones de parsing ahora finalizan la importación como fallida, conservan el archivo y completan la invocación de Hangfire sin reintento. — EBC

* **2026-07-18** — TASK-EBC-BE-22 cerrada: Progreso observable y sanitizado de importaciones implementado con persistencia independiente, Hangfire.Console, SignalR, snapshots REST y UI Angular. — EBC

* **2026-07-18** — TASK-EBC-BE-21 cerrada: Se soportaron de forma explícita las variantes restantes de movimientos de cuenta: los CSV BCR omiten únicamente el pie de resumen estructural y continúan rechazando dobles direcciones ambiguas; las hojas reconocen fechas contables y de transacción. Los jobs 5, 9 y 11 finalizaron correctamente. — EBC

* **2026-07-18** — TASK-EBC-DB-03 cerrada: Se deshabilitaron los reintentos automáticos para ImportJobs mediante AutomaticRetry Attempts=0 y se aplicó la migración pendiente AddTransactionOperationType a dbbancos. — EBC

* **2026-07-18** — TASK-EBC-BE-18 cerrada: Upload ahora vincula entryPath, entryIndex y template explícitamente desde multipart/form-data. La búsqueda de respaldo dejó de usar SingleOrDefault, eliminando el 500 incluso si un cliente antiguo omite entryIndex. — EBC

* **2026-07-18** — TASK-EBC-FE-06 cerrada: Se normalizó el proxy local: /api mantiene endpoints de aplicación y /_api enruta infraestructura con soporte WebSocket. Health y Hangfire se movieron a /_api/health y /_api/hangfire; se dejó /hubs listo para hubs futuros. — EBC

* **2026-07-18** — TASK-EBC-BE-17 cerrada: Se eliminó la excepción masiva en la resolución de entradas de importación usando una selección tolerante por EntryIndex. La interfaz ahora resume éxitos y fallos al terminar todo el lote, mantiene solo los archivos fallidos y muestra los errores con estilo rojo. — EBC

* **2026-07-18** — TASK-EBC-BE-16 cerrada: Se corrigió la confirmación de ZIP con entradas de ruta repetida: preview y carga usan EntryIndex estable, evitando SingleOrDefault ambiguo. Se añadió el acceso visible «Ver jobs y reintentos» y el proxy local para /hangfire. — EBC

* **2026-07-18** — ISSUE-001 resuelto: El contexto de datos ahora crea un propietario predeterminado, el catálogo base de cuentas y dos auxiliares neutrales. El flujo de pre-revisión resuelve automáticamente un auxiliar compatible según plantilla y tipo de cuenta, por lo que la carga ya no queda bloqueada por esa selección. Se validó con compilación, pruebas automatizadas y QA de interfaz.

* **2026-07-18** — TASK-EBC-QA-01 cerrada: Se completó la revisión colaborativa del flujo solicitado: archivos sueltos o ZIP, preclasificación por contenido, job por archivo, parsers bancarios, clasificación reglas→IA→General y revisión/categorías manuales. — EBC

* **2026-07-18** — TASK-EBC-BE-07 cerrada: Se completó la pre-revisión automática por contenido sin auxiliar obligatorio, resolución por plantilla, aprendizaje estructural local e importación idempotente por archivo. — EBC

* **2026-07-18** — TASK-EBC-FE-05 cerrada: Se restauró la lista completa de entradas ZIP, el selector por archivo pendiente y el conteo exacto de archivos listos. — EBC

* **2026-07-18** — TASK-EBC-BE-08 cerrada: Se implementó revisión segura de ZIP con entradas independientes, rutas relativas, exclusión de metadatos, límites anti ZIP-bomb y creación de un job por archivo confirmado. — EBC

* **2026-07-18** — TASK-EBC-BE-10 cerrada: Se completaron extractores de estados de tarjeta para CSV, XLS/HTML y PDF, diferenciando compras, pagos, intereses y cargos y preservando USD y equivalente CRC. — EBC

* **2026-07-18** — TASK-EBC-BE-11 cerrada: Se habilitaron movimientos de cuenta XLS binario y XLS basado en HTML con detección por contenido, encabezados normalizados, validación de dirección e idempotencia. — EBC

* **2026-07-18** — TASK-EBC-BE-13 cerrada: Se completó la clasificación familiar: historial y reglas antes de Azure AI, creación/reutilización segura de categorías, fallback General pendiente, alta manual desde UI y temporales reintentables. — EBC

* **2026-07-18** — TASK-EBC-BE-14 cerrada: Se agregó ClassificationSource.Ai como valor compatible al final del enum para auditar clasificaciones IA sin renumerar fuentes existentes. — EBC

* **2026-07-18** — TASK-EBC-BE-15 cerrada: Se corrigió el parser dual para XLS binario y tablas HTML/XLS, reutilizando encabezados y validaciones y cubriéndolo con fixture anonimizado. — EBC

* **2026-07-18** — TASK-EBC-BE-12 cerrada: Se analizaron confidencialmente todas las muestras bancarias disponibles y se separaron en siete plantillas estructurales. Se corrigió la firma del resumen CSV de tarjeta para admitir el esquema real sin columna Product y se agregó una plantilla independiente para movimientos de cuenta en XLS binario. La pre-revisión de archivos sueltos y de un ZIP completo clasificó 19 de 19 archivos, con cero pendientes y sin usar nombres o rutas. — EBC

* **2026-07-18** — TASK-EBC-BE-09 cerrada: Implementada la revisión guiada de formatos y el aprendizaje de firmas estructurales seguras. Las firmas aprendidas se consultan antes de las reglas estáticas. — EBC

* **2026-07-18** — TASK-EBC-QA-01 en revisión: entorno local Bancos levantado (frontend https://localhost:4200, API https://localhost:5001 y Hangfire). Hallazgo de UI registrado y resuelto: ISSUE-002 / TASK-EBC-FE-03 eliminó el límite de 640 px del formulario de Importaciones; build Angular exitoso y verificación sin desbordamiento a 904 px (hero y formulario 840 px) y 390 px (formulario 350 px). Revisión de arquitectura CSS registrada y resuelta: ISSUE-003 / TASK-EBC-FE-04 separó tokens globales, estilos compartidos, layout App y estilos encapsulados de Importaciones; build exitoso y pantalla Revisión conservada a 904 px. Documentación: skill angular-css-architecture creado en TASK-EBC-DOC-02 y sincronización de skills completada en TASK-EBC-DOC-03 (56 idénticos, 0 diferencias). Pendiente: importar un archivo de prueba no sensible, recorrer resultados en Revisión y confirmar catálogo/datos semilla; ISSUE-001 continúa pendiente. — EBC

* **2026-07-18** — TASK-EBC-DOC-03 cerrada: Se sincronizaron las skills canónicas desde .agents hacia .claude y .codex; el reporte final confirma 56 skills idénticas. — EBC

* **2026-07-18** — TASK-EBC-DOC-02 cerrada: Se creó la skill angular-css-architecture con la convención de tokens globales, styleUrl por componente y validación responsive. — EBC

* **2026-07-18** — TASK-EBC-FE-04 cerrada: Se crearon tokens CSS semánticos globales y se separaron los estilos de App e Imports mediante styleUrl. — EBC

* **2026-07-18** — ISSUE-003 resuelto: Se definieron tokens semánticos en :root y se trasladaron los estilos de App e Imports a sus hojas CSS asociadas.

* **2026-07-18** — TASK-EBC-FE-03 cerrada: Se eliminó el límite fijo del formulario de importaciones y se habilitó ancho fluido dentro del contenedor. — EBC

* **2026-07-18** — ISSUE-002 resuelto: El formulario ahora usa inline-size: 100% dentro del contenedor principal.

* **2026-07-18** — TASK-EBC-FE-02 cerrada: Pantalla de importaciones renovada con arrastrar y soltar múltiples archivos, cola de carga y auxiliares disponibles. — EBC

* **2026-07-18** — TASK-EBC-DB-02 cerrada: Base reinicializada con una única migración inicial que incluye catálogo contable y auxiliares semilla no sensibles. — EBC

* **2026-07-18** — TASK-EBC-INF-06 cerrada: Script de arranque reubicado en .codex y referencias actualizadas. — EBC

* **2026-07-18** — TASK-EBC-INF-05 cerrada: La acción del entorno ahora nombra e invoca explícitamente el arranque integrado de API y web. — EBC

* **2026-07-18** — TASK-EBC-INF-04 cerrada: Entorno reutilizable de Bancos creado; reemplaza dos configuraciones ajenas y arranca el desarrollo HTTPS integrado. — EBC

* **2026-07-18** — TASK-EBC-FE-01 cerrada: Dashboard Angular standalone minimalista por features para importaciones y revisión, servido por la API, con proxy HTTPS y arranque integrado en modo watch. — EBC

* **2026-07-18** — TASK-EBC-BE-06 cerrada: Implementada y validada clasificación determinística de movimientos: coincidencia exacta aprobada, reglas por patrón, categoría General pendiente de revisión y endpoints mínimos de categorías, reglas y revisión. — EBC

* **2026-07-18** — TASK-EBC-BE-05 cerrada: Implementado extractor PDF Coopealianza con validación de saldo y composición de pagos, persistencia idempotente de estados y pagos, migración SQL y pruebas con fixture PDF anonimizado. — EBC

* **2026-07-18** — TASK-EBC-DOC-01 cerrada: ADR-02 y 00_context.md actualizados para que activos y pasivos USD generen diferencial cambiario. — EBC

* **2026-07-18** — TASK-EBC-BE-04 cerrada: Implementado el lector BAC de financiamientos XLS binario con persistencia idempotente por auxiliar. — EBC

* **2026-07-18** — TASK-EBC-BE-03 cerrada: Se agregaron endpoints mínimos de propietarios, cuentas y auxiliares; el upload BCR ahora agenda mediante Hangfire y el job es idempotente tras completar. Se eliminó la precisión decimal global para usar los valores predeterminados de EF y se añadió la migración correspondiente. — EBC

* **2026-07-18** — TASK-EBC-INF-02 cerrada: Aplicada y verificada la migración de importaciones; el workflow de despliegue quedó deshabilitado y documentado hasta definir el contenedor. Se preparó la publicación de los cambios del proyecto. — EBC

* **2026-07-18** — TASK-EBC-BE-02 cerrada: Implementado detector de plantillas por firma de contenido para CSV, HTML/XLS, XLS BIFF y PDF; lector inicial BCR débito CSV con validación y persistencia idempotente mediante huella de movimiento. El job de Hangfire recibe ImportId y registra sus etapas. — EBC

* **2026-07-18** — TASK-EBC-INF-01 cerrada: Se inicializó Hangfire localmente y se verificaron su esquema SQL y dashboard. — EBC

* **2026-07-18** — TASK-EBC-BE-01 cerrada: Se agregó carga segura de appsettings.Development.json y db.json para SQL, reutilizada por EF Core y Hangfire. — EBC

* **2026-07-18** — TASK-EBC-DB-01 cerrada: Se aplicó y verificó la migración inicial InitialCreate en dbbancos sin insertar datos de negocio. — EBC

* **2026-07-18** — TASK-EZ-BE-01 cerrada: Se creó la base .NET 10 con EF Core/MSSQL, Hangfire, importación temporal regenerable, esquema inicial y pruebas. — EBC
