# TASK-EBC-MCP-27 — Cierres y conciliación de saldos del lote 2026-07-17

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 22:25 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Cierres y conciliación de saldos del lote 2026-07-17

## Contexto

Los cinco imports originalmente fallidos fueron reprocesados con éxito. El usuario solicita validar BD, aplicar cierres y contrastar resultados con docs/saldos-corte-jun2026.md.

## Objetivo

Verificar la persistencia final del lote importado, calcular cierres por período y conciliar los saldos de junio contra la documentación de corte.

## Alcance permitido

* Consultas SELECT sanitizadas a dbbancosmcp
* Job BancosMCP de cálculo de cierres
* docs/saldos-corte-jun2026.md
* Trazabilidad de Hangfire

## Fuera de alcance

* Modificar archivos de entrada
* Modificar saldos manualmente
* Borrar jobs históricos
* Cambios de código o despliegue

## Criterios de aceptación

* [ ] Los imports de reemplazo están exitosos
* [ ] El job de cierres termina correctamente
* [ ] Existen cierres para JUN-2026
* [ ] La conciliación con la documentación da coincidencia o reporta discrepancias

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `docs/saldos-corte-jun2026.md`

## Plan técnico

1. Usar calculate_period_closings
2. Usar dbQuery exclusivamente con SELECT agregados o de comparación booleana
3. Procesar documentación local sin emitir valores financieros

## Pasos

1. Verificar estados exitosos y persistencia de entidades
2. Calcular cierres desde ENE-2026
3. Comprobar el job de cierres y los registros resultantes
4. Comparar los cierres JUN-2026 contra la documentación

## Salida esperada

Cierres calculados y resultado de conciliación sin divulgar datos financieros.

## Validación

* [ ] Estado Hangfire
* [ ] Conteos sanitizados de persistencia
* [ ] Comparación de presencia y valor con documentación

## Rollback

El cálculo es regenerable por job; no se harán ajustes manuales.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Persistencia validada y cierres calculados desde ENE-2026. El job de cierres concluyó exitosamente y generó seis registros para JUN-2026. La comparación automatizada, sin divulgar importes, detectó que los seis saldos calculados no coinciden con los valores documentados en la sección de conciliación de docs/saldos-corte-jun2026.md.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-26 22:25 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
