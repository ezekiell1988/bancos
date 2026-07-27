> **Última actualización:** 2026-07-27 CR (ISSUE-006 resuelto)



## Completado

* **2026-07-27** — ISSUE-006 resuelto: Se unificó la extracción PDF reconstruida, se alinearon las regex con el formato BN, se resolvió el par CRC/USD, se enrutaron movimientos por moneda y se crearon vínculos idempotentes. La importación local terminó correctamente y la conciliación almacenada resultó válida en ambas monedas.

* **2026-07-26** — TASK-EBC-MCP-17: Auditoría técnica completada sin cambios de código ni datos. La muestra PDF no coincide con la firma y los patrones requeridos; el flujo de cuenta falla para múltiples vínculos y no persiste asociaciones entre corte y movimientos. ISSUE-006 abierto. — EBC

* **2026-07-26** — TASK-EBC-MCP-17: auditoría de plantilla/parser BN completada sin cambios de código ni datos. ISSUE-006 abierto: detección no reconoce la muestra, resolución de cuenta usa extractor XLS para PDF con múltiples cuentas y faltan vínculos corte-movimientos. La suite no ejecuta por errores preexistentes xUnit1001/xUnit1002 en McpProtocolTests. — EBC

* **2026-07-26** — TASK-EBC-MCP-11: Se auditó la plantilla Resumen de tarjeta BAC. La detección resolvió la plantilla CSV esperada, el flujo asigna las cuentas CRC/USD y el job persiste movimientos con idempotencia por huella. — C

* **2026-07-26** — TASK-EBC-MCP-10: Se auditó la plantilla de estado consolidado BAC: catálogo, detección, parser, job e idempotencia están conectados correctamente. El lote revisado no incluyó una muestra de esta plantilla, por lo que se documenta esa limitación de validación empírica. — C

* **2026-07-26** — TASK-EBC-MCP-23: Se excluyeron los sondeos GET y DELETE de /mcp del rate limit global, manteniendo la política de concurrencia para las operaciones POST MCP. — C

* **2026-07-26** — TASK-EBC-MCP-22: Se verificó la compatibilidad MCP HTTP con protocolo 2025-03-26 mediante una sesión real: initialize, tools/list y health_status respondieron correctamente. — C

* **2026-07-26** — TASK-EBC-MCP-30: Se normalizaron como magnitudes positivas los valores de columnas separadas de débito y crédito en XLS/HTML, preservando la semántica de signo para columnas de monto único. — C

* **2026-07-26** — TASK-EBC-MCP-29: Se resolvieron las variantes CSV de débito por coincidencia única del identificador de carpeta, limitada a parsers CSV; la recarga limpia separó correctamente las cuentas y excluyó XLS. — C

* **2026-07-26** — TASK-EBC-MCP-28: Se agregó resolución única de cuenta por IBAN de carpeta para movimientos XLS, preservando la plantilla detectada. El archivo pendiente se encoló y su job terminó exitosamente. — C

* **2026-07-26** — TASK-EBC-MCP-26: Corregido y validado el reproceso de cinco imports. El parser Coopealianza ahora usa la misma extracción PDF que la detección; los cuatro jobs de préstamo concluyeron correctamente. El despacho de CSV BCR conserva la plantilla detectada y resuelve la cuenta desde esa plantilla, evitando sustituirla por XLS. Tras reiniciar BancosMCP, el CSV se encoló con el parser correcto y concluyó correctamente. — EBC

* **2026-07-26** — TASK-EBC-MCP-25: Diagnóstico completado sin mutaciones. Los cuatro PDFs Coopealianza son PDFs válidos y pasan detección de plantilla, pero el parser usa una extracción reconstruida distinta de la extracción de detección, provocando que la firma requerida no llegue intacta al parser. El CSV fallido es texto plano, pero su job histórico fue encolado con parser de XLS; el detector actual lo clasifica como BCR CSV. Hay una discrepancia de clasificación/despacho entre el job persistido y el detector actual. — EBC

* **2026-07-24** — TASK-EBC-QA-02: 46/46 tests pasan. Se corrigieron 4 fallas pre-existentes: (1) texto de detección BacCreditOnlinePdfV1 actualizado a "fecha de pago de contado"; (2) JsonConverter en AccountKind e ImportStatus/ClosingStatus/ClassificationSource/ClassificationStatus/TransactionOperationType para correcta serialización en tests; (3) fixture CoopealianzaLoanPdfFixture actualizado con encoding Latin-1 y mapeo /colonmonetary para que PdfPig extraiga ₡ correctamente; (4) endpoint Upload corregido: bool force → bool? force, accountAuxiliaryId de [FromForm] a [FromQuery], configuración de StorageOptions en BancosApiFactory con path temporal, y early-return en ProcessAsync cuando el archivo fue eliminado y el import ya está Completed. — EBC

* **2026-07-18** — TASK-EBC-QA-01: Se completó la revisión colaborativa del flujo solicitado: archivos sueltos o ZIP, preclasificación por contenido, job por archivo, parsers bancarios, clasificación reglas→IA→General y revisión/categorías manuales. — EBC

* **2026-07-18** — TASK-EBC-QA-01 en revisión: entorno local Bancos levantado (frontend https://localhost:4200, API https://localhost:5001 y Hangfire). Hallazgo de UI registrado y resuelto: ISSUE-002 / TASK-EBC-FE-03 eliminó el límite de 640 px del formulario de Importaciones; build Angular exitoso y verificación sin desbordamiento a 904 px (hero y formulario 840 px) y 390 px (formulario 350 px). Revisión de arquitectura CSS registrada y resuelta: ISSUE-003 / TASK-EBC-FE-04 separó tokens globales, estilos compartidos, layout App y estilos encapsulados de Importaciones; build exitoso y pantalla Revisión conservada a 904 px. Documentación: skill angular-css-architecture creado en TASK-EBC-DOC-02 y sincronización de skills completada en TASK-EBC-DOC-03 (56 idénticos, 0 diferencias). Pendiente: importar un archivo de prueba no sensible, recorrer resultados en Revisión y confirmar catálogo/datos semilla; ISSUE-001 continúa pendiente. — EBC
