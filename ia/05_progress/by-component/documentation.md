> **Última actualización:** 2026-07-26 CR (TASK-EBC-DOC-06 completada)



## Completado

* **2026-07-26** — TASK-EBC-DOC-06: Se documentó la negociación de versión MCP por sesión y la exclusión de GET/DELETE del limitador global. La copia canónica de mcp-vscode fue sincronizada hacia Claude y Codex. — EBC

* **2026-07-26** — TASK-EBC-MCP-16: Revisión de bcr-debit-html-xls-v1 para bcr-debit-01-crc (MovimientosPorRangoFecha_Cta_CR07015202001294229652.xls, HTML envuelto en XLS). Parser bcr-debit-html confirmado. Archivo cubre 02/01/2026–17/07/2026 con 950 movimientos. Totales: débito ₡19,737,577.49 / crédito ₡17,455,629.67 / net −₡2,281,947.82. No contiene saldo inicial explícito; seed no requerido (balance inicial = ₡0). Saldo final documentado en docs/saldos-corte-jun2026.md como net del período. Detección bcr-debit-html funciona correctamente por términos 'banco de costa rica' + alternativas de movimientos. — EBC

* **2026-07-26** — TASK-EBC-MCP-14: Revisión de bank-account-movements-xls-v1 para bac-debit-01-crc (Transacciones del mes.xls, CDFV2 binario). Parser XLS con xlrd lee 147 filas; confirmado parser bank-account-movements-xls. Archivo contiene campo explícito Saldo Inicial=₡10,551.04 y Saldo en Libros=₡39.91. Totales: débito ₡8,021,947.13 / crédito ₡8,011,436.00 / net −₡10,511.13. Saldo inicial sembrado en EF migration (ENE-2026, fecha 2025-12-19, +₡10,551.04). Saldo final ₡39.91 documentado en docs/saldos-corte-jun2026.md. Idempotencia via sourceFingerprint confirmada en implementación existente. — EBC

* **2026-07-26** — TASK-EBC-MCP-15: Revisión e implementación completa para bcr-debit-csv-v1. Se detectó bug crítico: BN USD y BN CRC comparten el mismo formato CSV que BCR (oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion), causando que los archivos BN se procesaran en la cuenta BCR. Solución implementada: (1) Templates centinela 10 (bn-debit-csv-v1) y 11 (bn-debit-csv-crc-v1) con required terms imposibles. (2) Método TryResolveAlternativeByIbanPathAsync en AccountResolver que extrae el IBAN del folder y retorna el account+template alternativo. (3) Branch en ProcessImportFileTool para bcr-debit-csv que aplica el override IBAN si corresponde. (4) SeedPatterns() actualizado para retornar IsActive=false en sentinelas auto-detectados por código. Idempotencia verificada mediante sourceFingerprint. — EBC

* **2026-07-26** — TASK-EBC-MCP-18: Implementada tabla tbAccountPeriodClosings, entidad EF, job Hangfire CalculateAccountPeriodClosingsJob, endpoint POST /account-period-closings/calculate y MCP tool calculate_period_closings. Migración InitialCreate regenerada con seeds de saldo inicial correctos por cuenta (ABR-2026 para bac-credit-01/02; MAY-2026 para bac-credit-03-crc; sin seed para bac-credit-04-crc cuyo saldo anterior era 0). Corregido cruce de asignación bac-credit-03/04. 12 jobs importados y cierre calculado desde ABR-2026: 45 registros en tbAccountPeriodClosings. Diferencias vs CSV JUN-2026 documentadas en docs/saldos-corte-jun2026.md — explicadas por interés sin fecha (no importable) y txns del PDF pendientes de liquidación. — EBC

* **2026-07-24** — TASK-EBC-MCP-13: Parser bac-credit-online-pdf implementado y validado. Se corrigió extracción de texto PDF (GetWords() con reconstrucción por líneas Y), resolución de cuenta CRC por IBANs del folder, signos (banco invertido), ExchangeRate=1 para CRC, idPeriods, updatedAt en re-importación y extracción de place. Además se implementó bac-credit-csv-v1 con ruteo CRC/USD a las 8 cuentas. 11 jobs Succeeded: 244 transacciones con amounts, signos y place correctos. — EBC

* **2026-07-24** — TASK-EBC-MCP-12: Financiamientos BAC completamente implementado. Se creó ResolveFinancingPairByPathAsync que resuelve el par CRC/USD por IBANs del folder, se pobló identifierHash en la semilla para las 4 cuentas BAC de crédito, y se implementó ProcessCardFinancings en ImportFileJob con persistencia separada por moneda en tbCardFinancings. Jobs Succeeded para los 3 archivos procesados (bac-credit-01, 02 y 04). — EBC

* **2026-07-24** — TASK-EBC-MCP-18: Auditoría completada sin defectos. Parser MCP extrae campos de encabezado completos (monto original, tasa, plazo, fecha inicio) más historial de pagos y tabla de cuotas. Job hace upsert correcto de header y cuotas, calcula porciones corriente y largo plazo. Sin incidencias que abrir. — EBC

* **2026-07-22** — TASK-EBC-MCP-09: Implementado endpoint SSE en /mcp/sse para Claude Code. Descubierto que la spec MCP requiere camelCase en tools/list — PascalCase de .NET impedía el descubrimiento. Corregidos regexes del parser Coopealianza (espacios opcionales en texto PDF sin separadores). Verificado: 4 PDFs paginados → 36 cuotas en tbLoanPayments, LoanStatement con datos de header completos. Documentado en references/10-sse-claude-code.md. — EBC

* **2026-07-21** — TASK-EBC-DOC-05: Documentada en ia/README.md la convención para ubicar tools MCP por feature. — EBC

* **2026-07-20** — TASK-EBC-DOC-04: Se actualizó la documentación para distinguir Bancos.Api como monolito funcional y Bancos.Mcp como servidor MCP auxiliar independiente, con catálogo, migraciones y base de datos propios. — EBC

* **2026-07-20** — TASK-EBC-MCP-01: Se creó el servidor MCP independiente para Copilot Studio con transporte JSON-RPC, tool diagnóstica segura, HTTPS local, pruebas y documentación. — EBC

* **2026-07-18** — TASK-EBC-DOC-03: Se sincronizaron las skills canónicas desde .agents hacia .claude y .codex; el reporte final confirma 56 skills idénticas. — EBC

* **2026-07-18** — TASK-EBC-DOC-02: Se creó la skill angular-css-architecture con la convención de tokens globales, styleUrl por componente y validación responsive. — EBC

* **2026-07-18** — TASK-EBC-DOC-01: ADR-02 y 00_context.md actualizados para que activos y pasivos USD generen diferencial cambiario. — EBC
