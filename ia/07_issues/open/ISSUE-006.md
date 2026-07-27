# ISSUE-006 — Importación BN de tarjeta no procesa el estado de cuenta PDF

**Severidad:** high
**Estado:** abierto
**Componente:** MCP / FileProcessing
**Detectado:** 2026-07-26 23:56 CR
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`

---

## Síntoma

El PDF de muestra no satisface la firma de bn-card-statement-pdf-v1; si se corrigiera la detección, la resolución de cuenta para las dos cuentas vinculadas usa un extractor XLS sobre el PDF. El flujo tampoco crea vínculos CardStatementLine para los movimientos extraídos.

## Causa raíz

La plantilla y el parser codifican encabezados y formatos de montos distintos a la muestra; AccountResolver reutiliza BacCreditFinancingXlsParser para identificar una cuenta de PDF y ProcessBnCardStatement persiste corte y movimientos sin asociarlos.

## Workaround

No encolar este formato; mantener el documento fuera de cargas hasta validar un parser corregido.

## Fix propuesto

Alinear firmas y regex con una fixture anonimizada del formato BN, implementar extracción de identificador apta para PDF o un selector explícito de cuenta, y persistir CardStatementLine de forma idempotente tras guardar las transacciones.

## Validación requerida

1. Cargar el PDF de muestra únicamente en el entorno local de prueba.
2. Extraer y revisar el saldo inicial por cada moneda presente.
3. Comprobar por moneda que `saldo inicial + suma de movimientos = saldo final` reportado en el archivo, con una tolerancia decimal documentada.

La evidencia de esta validación debe ser agregada y no incluir montos, identificadores ni contenido financiero del documento.

## Tareas vinculadas

* TASK-EBC-MCP-17
