> **Última actualización:** 2026-07-20 CR (TASK-EBC-BE-23 completada)



## Completado

* **2026-07-20** — TASK-EBC-BE-23: Parser, entidad, migración y handler implementados. Se importaron correctamente 4 CardStatements del PDF consolidado BAC julio 2026. Template detectado automáticamente con firma content-based. Upsert por (AccountAuxiliaryId, CardNumberMasked, StatementDate) funcional. — EBC

* **2026-07-20** — TASK-EBC-INF-08 (fixes de backend durante validación de importación masiva):
  * **`ClassificationModule.cs`**: `SingleOrDefaultAsync` → `FirstOrDefaultAsync` para la categoría "General". Causa: múltiples workers de Hangfire creaban duplicados de "General" en BD (el índice único `(Name, ParentId)` no los bloquea cuando `ParentId IS NULL`). El `SingleOrDefault` lanzaba al encontrar más de uno.
  * **`BancosDbContext.SeedDefaults`** + **migración `SeedGeneralCategory`**: La categoría "General" se siembra desde migración con ID fijo. Elimina la race condition de creación concurrente.
  * **`ImportJobs.cs` — catch blocks**: Se agregó `db.ChangeTracker.Clear() + db.Imports.Attach(import)` antes de guardar el fallo en ambos catch blocks. Causa: una `DbUpdateException` deja el contexto en estado inconsistente — el segundo `SaveChangesAsync` del catch también fallaba, dejando el import permanentemente en `status=1 Processing`.
  * **`ImportJobs.cs` — race condition LoanStatements**: Se agrega un `try/catch(DbUpdateException)` al insertar el LoanStatement. Si el registro ya existe (ganó un job concurrente), se limpia el context y se re-attachea `import` para que el `status=Completed` se pueda guardar en el SaveChanges posterior.
  * **`CoopealianzaLoanPdfParser.cs` — BalanceRegex**: `[\d., ]` → `[\d.,\s]`. Causa: PdfPig usa non-breaking space (U+00A0) como separador de miles en PDFs Bankingly. El espacio ASCII literal no lo capturaba, dejando el balance en solo el primer dígito (ej. `4` en lugar de `4372249.85`).
  * **`CoopealianzaLoanPdfParser.cs` — PaymentRegex**: Reescrita para PdfPig que concatena texto sin newlines. Usa ₡ como delimitador natural entre campos: `(?<date>\d{2}/\d{2}/\d{4})Pago(?<capital>₡[^₡]*)...`
  * **`CardStatementParser.cs`**: `ParseBacOnlinePdfConcatenated` para PDFs BAC online (texto concatenado sin saltos). `ParseBacDualAmountRows` para CSV BAC crédito con columnas Local/Dollars separadas.
  * **`ImportsModule.cs`**: Nuevo endpoint `POST /api/imports/{id}/retry` para re-encolar imports fallidos sin necesidad de volver a subir el archivo.
  * Ver detalle completo en [IMPORT-PARSER-TROUBLESHOOTING.md](../../06_decisions/IMPORT-PARSER-TROUBLESHOOTING.md). — EBC

* **2026-07-18** — TASK-EBC-BE-20: El parser de estados BAC distingue resúmenes de pago y snapshots sin tabla de movimientos; los deriva a revisión manual segura sin generar movimientos sintéticos. Tras corregir el esquema de progreso, los ocho trabajos afectados finalizaron sin errores de infraestructura. — EBC

* **2026-07-18** — TASK-EBC-BE-19: Se clasificaron los fallos de importación: los ocho jobs fallidos corresponden a estados de tarjeta sin movimientos detallados. Las validaciones de parsing ahora finalizan la importación como fallida, conservan el archivo y completan la invocación de Hangfire sin reintento. — EBC

* **2026-07-18** — TASK-EBC-BE-22: Progreso observable y sanitizado de importaciones implementado con persistencia independiente, Hangfire.Console, SignalR, snapshots REST y UI Angular. — EBC

* **2026-07-18** — TASK-EBC-BE-21: Se soportaron de forma explícita las variantes restantes de movimientos de cuenta: los CSV BCR omiten únicamente el pie de resumen estructural y continúan rechazando dobles direcciones ambiguas; las hojas reconocen fechas contables y de transacción. Los jobs 5, 9 y 11 finalizaron correctamente. — EBC

* **2026-07-18** — TASK-EBC-BE-18: Upload ahora vincula entryPath, entryIndex y template explícitamente desde multipart/form-data. La búsqueda de respaldo dejó de usar SingleOrDefault, eliminando el 500 incluso si un cliente antiguo omite entryIndex. — EBC

* **2026-07-18** — TASK-EBC-BE-17: Se eliminó la excepción masiva en la resolución de entradas de importación usando una selección tolerante por EntryIndex. La interfaz ahora resume éxitos y fallos al terminar todo el lote, mantiene solo los archivos fallidos y muestra los errores con estilo rojo. — EBC

* **2026-07-18** — TASK-EBC-BE-16: Se corrigió la confirmación de ZIP con entradas de ruta repetida: preview y carga usan EntryIndex estable, evitando SingleOrDefault ambiguo. Se añadió el acceso visible «Ver jobs y reintentos» y el proxy local para /hangfire. — EBC

* **2026-07-18** — TASK-EBC-BE-07: Se completó la pre-revisión automática por contenido sin auxiliar obligatorio, resolución por plantilla, aprendizaje estructural local e importación idempotente por archivo. — EBC

* **2026-07-18** — TASK-EBC-BE-08: Se implementó revisión segura de ZIP con entradas independientes, rutas relativas, exclusión de metadatos, límites anti ZIP-bomb y creación de un job por archivo confirmado. — EBC

* **2026-07-18** — TASK-EBC-BE-10: Se completaron extractores de estados de tarjeta para CSV, XLS/HTML y PDF, diferenciando compras, pagos, intereses y cargos y preservando USD y equivalente CRC. — EBC

* **2026-07-18** — TASK-EBC-BE-11: Se habilitaron movimientos de cuenta XLS binario y XLS basado en HTML con detección por contenido, encabezados normalizados, validación de dirección e idempotencia. — EBC

* **2026-07-18** — TASK-EBC-BE-13: Se completó la clasificación familiar: historial y reglas antes de Azure AI, creación/reutilización segura de categorías, fallback General pendiente, alta manual desde UI y temporales reintentables. — EBC

* **2026-07-18** — TASK-EBC-BE-14: Se agregó ClassificationSource.Ai como valor compatible al final del enum para auditar clasificaciones IA sin renumerar fuentes existentes. — EBC

* **2026-07-18** — TASK-EBC-BE-15: Se corrigió el parser dual para XLS binario y tablas HTML/XLS, reutilizando encabezados y validaciones y cubriéndolo con fixture anonimizado. — EBC

* **2026-07-18** — TASK-EBC-BE-12: Se analizaron confidencialmente todas las muestras bancarias disponibles y se separaron en siete plantillas estructurales. Se corrigió la firma del resumen CSV de tarjeta para admitir el esquema real sin columna Product y se agregó una plantilla independiente para movimientos de cuenta en XLS binario. La pre-revisión de archivos sueltos y de un ZIP completo clasificó 19 de 19 archivos, con cero pendientes y sin usar nombres o rutas. — EBC

* **2026-07-18** — TASK-EBC-BE-09: Implementada la revisión guiada de formatos y el aprendizaje de firmas estructurales seguras. Las firmas aprendidas se consultan antes de las reglas estáticas. — EBC

* **2026-07-18** — TASK-EBC-BE-06: Implementada y validada clasificación determinística de movimientos: coincidencia exacta aprobada, reglas por patrón, categoría General pendiente de revisión y endpoints mínimos de categorías, reglas y revisión. — EBC

* **2026-07-18** — TASK-EBC-BE-05: Implementado extractor PDF Coopealianza con validación de saldo y composición de pagos, persistencia idempotente de estados y pagos, migración SQL y pruebas con fixture PDF anonimizado. — EBC

* **2026-07-18** — TASK-EBC-BE-04: Implementado el lector BAC de financiamientos XLS binario con persistencia idempotente por auxiliar. — EBC

* **2026-07-18** — TASK-EBC-BE-03: Se agregaron endpoints mínimos de propietarios, cuentas y auxiliares; el upload BCR ahora agenda mediante Hangfire y el job es idempotente tras completar. Se eliminó la precisión decimal global para usar los valores predeterminados de EF y se añadió la migración correspondiente. — EBC

* **2026-07-18** — TASK-EBC-BE-02: Implementado detector de plantillas por firma de contenido para CSV, HTML/XLS, XLS BIFF y PDF; lector inicial BCR débito CSV con validación y persistencia idempotente mediante huella de movimiento. El job de Hangfire recibe ImportId y registra sus etapas. — EBC

* **2026-07-18** — TASK-EBC-BE-01: Se agregó carga segura de appsettings.Development.json y db.json para SQL, reutilizada por EF Core y Hangfire. — EBC

* **2026-07-18** — TASK-EZ-BE-01: Se creó la base .NET 10 con EF Core/MSSQL, Hangfire, importación temporal regenerable, esquema inicial y pruebas. — EBC
