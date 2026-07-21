# ISSUE-004 — BacCreditFinancingParser descarta moneda y el job no hace upsert

**Severidad:** high
**Estado:** abierto
**Componente:** Parsing / ImportJobs
**Detectado:** 2026-07-20 18:45 CR
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`

---

## Síntoma

El XLS de financiamientos BAC incluye la moneda en cada celda de monto (ej. "9.20 USD", "4,995.80 CRC"). El parser la descarta completamente. CreditFinancing no tiene columna CurrencyCode, por lo que registros USD quedan sin moneda correcta. Además el job nunca actualiza un financiamiento ya importado (solo verifica fingerprint y salta), impidiendo que re-importar corrija saldos o moneda.

## Causa raíz

1) MoneyParser.TryParse extrae solo el número y descarta el sufijo de moneda. 2) ParsedCreditFinancing y CreditFinancing no tienen CurrencyCode. 3) La fingerprint hardcodea "CRC". 4) El job usa fingerprint como única llave de deduplicación — si ya existe lo omite sin actualizar.

## Workaround

ninguno

## Fix propuesto

1) Agregar CurrencyCode a ParsedCreditFinancing y al parser (extraer moneda del sufijo de celda). 2) Agregar CurrencyCode a entidad CreditFinancing + migración (default CRC). 3) Cambiar el job a upsert por (AccountAuxiliaryId, FinancingDate, Concept): si existe actualiza Installments/InstallmentAmount/OutstandingBalance/CurrencyCode/ImportId; si no existe crea. 4) Permitir re-procesar un import ya Completed reseteando su status.

## Tareas vinculadas

* ninguna
