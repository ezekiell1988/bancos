> **Última actualización:** 2026-07-27 CR (TASK-EBC-BE-29 completada)



## Completado

* **2026-07-27** — TASK-EBC-BE-29: Implementado job diario BCCR de tipo de cambio USD/CRC: consulta el indicador 318 con fallback de hasta tres días, realiza upsert idempotente para BN y BAC y queda programado a las 08:00 en hora de Costa Rica. Las pruebas específicas pasan; los fallos ajenos de la suite completa se registraron en ISSUE-008. — EBC

* **2026-07-27** — TASK-EBC-MCP-33: validado flujo .NET → Azure AI → No clasificado → revisión manual. Se añadió timeout configurable de 10 s para Azure AI; un timeout queda auditado como No clasificado sin abortar el lote. Pruebas de clasificación: 14 aprobadas. — EBC

* **2026-07-27** — TASK-EBC-MCP-32: Revisados los 6 criterios de aceptación contra el código: classify_pending_transactions ejecuta lote regla→IA→No clasificado; list_unclassified_transactions expone explicación; confirm_transaction_classification registra manual y crea/actualiza regla determinista (probado con test dedicado); TryClassifyWithAiAsync cae a No clasificado ante fallo/baja confianza sin excepción no controlada; todas las respuestas incluyen Explanation con el origen (rule/ai/manual/unclassified) sin datos bancarios sensibles. Los 9 tests de ClassificationServiceTests pasan; el único fallo al filtrar McpProtocolTests es el problema preexistente de Hangfire JobStorage en el arranque del WebApplicationFactory, no relacionado con esta tarea. — EBC

* **2026-07-27** — ISSUE-007 resuelto: Se incorporó extracción normalizada con checksum, comparación simultánea de huellas bancaria y de tarjeta, resolución obligatoria de una cuenta CRC y una USD, y unicidad de catálogo por identidad más moneda. La migración y el reproceso MCP fueron verificados con conciliación e idempotencia correctas.

* **2026-07-27** — TASK-EBC-BE-28: Validación de identidad compartida implementada y verificada operativamente. El estado BN se resuelve contra cuentas lógicas CRC y USD separadas, exige coincidencia de identidad bancaria y tarjeta, y conserva conciliación e idempotencia. — EBC

* **2026-07-24** — **Implementación completa y validada en producción local (2026-07-24)**

- Implementado `ResolveFinancingPairByPathAsync` en `AccountResolver`: extrae IBANs del nombre de carpeta vía regex, hashea con SHA-256 y los busca en `tbBankAccounts.identifierHash` para retornar par CRC+USD.
- Extendido `ProcessImportFileTool` para detectar `bac-credit-financing-xls` y usar resolución por path en lugar de por contenido.
- Extendido `ImportFileJob.ExecuteAsync` con parámetro `Guid? usdBankAccountId`; `ProcessCreditFinancings` agrupa por moneda y persiste en la cuenta correcta.
- Corregido `status = "active"` (minúsculas) para respetar check constraint de `tbCardFinancings`.
- Migración única `InitialCreate` regenerada con `identifierHash` en seed para pares:
  - `bac-credit-01`: CR69...1047 (CRC) / CR17...8556 (USD)
  - `bac-credit-02`: CR48...1545 (CRC) / CR18...4214 (USD)
- Importación real de 2 archivos validada: 8 registros en `tbCardFinancings`, ambos jobs Hangfire en `Succeeded`, enrutamiento CRC/USD correcto.
- Próximo paso: agregar `bac-credit-03` (CR64...9651 CRC / CR13...8803 USD) y procesar su archivo. — EBC

* **2026-07-24** — ISSUE-005 resuelto: McpProtocolTests pasan en su totalidad (20/20). El problema de IBackgroundJobClient se resolvió porque ProcessImportFileTool obtiene el cliente en ExecuteAsync (lazy), no en el constructor, por lo que startup sin Hangfire funciona correctamente.

* **2026-07-24** — ISSUE-004 resuelto: Corregidos los dos defectos residuales en ImportJobs.cs: CreateFingerprint(ParsedCreditFinancing) usa financing.CurrencyCode y el guard de Completed fue eliminado para permitir re-procesamiento.

* **2026-07-24** — TASK-EBC-BE-27: Corregidos dos defectos residuales del ISSUE-004: CreateFingerprint(ParsedCreditFinancing) ahora usa financing.CurrencyCode en lugar del literal CRC; eliminado el early return que bloqueaba re-procesar imports con estado Completed. — EBC

* **2026-07-24** — TASK-EBC-MCP-20: Resolver automático de cuenta implementado en AccountResolver y BacCreditFinancingXlsParser. ProcessImportFileTool mantiene contrato files-only. Build limpio y 20/20 tests correctos. — EBC

* **2026-07-22** — TASK-EBC-MCP-19: Actualizado Bancos.Mcp para Streamable HTTP de ChatGPT: endpoint /mcp con sesiones, validación de versión y origen, cierre DELETE, outputSchema y structuredContent. Se difirió la autenticación de producción porque el proyecto continúa local y el despliegue queda fuera de alcance. — EBC

* **2026-07-22** — TASK-EBC-BE-26: Se agregó número y estado de cuota, con upsert por préstamo y número de cuota. La migración inicial se regeneró y la carga de PDFs confirmó actualizaciones sin duplicados. — EBC

* **2026-07-22** — TASK-EBC-BE-25: Migración inicial consolidada, arranque local aplica migraciones y el procesamiento de préstamos calcula porción inmediata, corriente y largo plazo desde el calendario persistido. Se evitó la pérdida de cuotas al serializar jobs de importación. — EBC

* **2026-07-21** — TASK-EBC-MCP-08: Reubicados los componentes propios de TemplateDetection dentro de su feature, conservando las abstractions transversales en Tools. — EBC

* **2026-07-21** — TASK-EBC-MCP-07: Centralizado el catálogo de plantillas, modularizado el servidor MCP y protegido el procesamiento local de archivos. — EBC

* **2026-07-20** — TASK-EBC-MCP-03: Se incorporó `detect_import_template` en Bancos.Mcp para identificar plantillas de importación mediante una ruta relativa confinada. La herramienta procesa PDF, CSV, XLS y XLSX, devuelve únicamente `idImportTemplates`, no persiste archivos ni consulta SQL Server. El catálogo MCP y la convención de esquema se consolidaron durante la tarea. — EBC

* **2026-07-20** — TASK-EBC-BE-24: Parser BN implementado. BnCardStatementPdfParser.cs maneja texto concatenado de PdfPig. Detecta automáticamente bn-card-statement-pdf-v1. Persiste CardStatement (corte ₡210,829 + $6.49 contado, pago mínimo ₡5,000 + $6.49, vence 03/08/2026), 19 Transactions (2 pagos + 17 compras) y 3 CreditFinancings activos (BN Marchamos 12M ₡32,334 y ₡46,324; Compras 6M ₡26,142). — EBC

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
