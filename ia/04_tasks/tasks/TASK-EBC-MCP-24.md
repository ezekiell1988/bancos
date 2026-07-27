# TASK-EBC-MCP-24 — Carga y conciliación de adjuntos bancarios 2026-07-17

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 22:08 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Carga y conciliación de adjuntos bancarios 2026-07-17

## Contexto

Solicitud explícita del usuario para cargar los adjuntos listados con BancosMCP, revisar jobs y BD, aplicar cierres y verificar saldos contra los MD de documentación.

## Objetivo

Procesar los archivos bancarios provistos para el lote 2026-07-17, verificar su persistencia y aplicar cierres, conciliando contra la documentación de análisis.

## Alcance permitido

* src/input/20260717
* Persistencia generada por BancosMCP
* Jobs Hangfire de importación y cierre
* Documentación de análisis existente

## Fuera de alcance

* Cambios de código, plantillas, despliegues, operaciones manuales sobre saldos o transacciones

## Criterios de aceptación

* [ ] Cada archivo obtiene un job válido y finaliza correctamente
* [ ] Los registros esperados quedan persistidos
* [ ] El job de cierres concluye correctamente
* [ ] Los saldos conciliados coinciden con el análisis documental o se reporta una discrepancia

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Usar BancosMCP para importación y cierres
2. Usar dbQuery solo para verificaciones SELECT sanitizadas
3. No modificar datos manualmente

## Pasos

1. Encolar importación de los adjuntos indicados
2. Comprobar que los jobs concluyan correctamente
3. Validar persistencia mediante consultas de solo lectura
4. Encolar y comprobar el cálculo de cierres
5. Conciliar los resultados con los MD de análisis

## Salida esperada

Importaciones persistidas, cierres calculados y verificación de conciliación documentada en el cierre de la tarea.

## Validación

* [ ] Estado de jobs
* [ ] Consultas SELECT de persistencia
* [ ] Comparación con MD de documentación

## Rollback

No se realizan escrituras manuales; las importaciones y cierres son trazables por job. Cualquier incidencia se detiene y se reporta.

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

* Pendiente de revisión: Se encolaron 18 importaciones y una fue rechazada antes de encolarse. Trece jobs finalizaron correctamente; cinco fallaron por incompatibilidad de firma/formato. Se verificó persistencia parcial. No se calcularon cierres ni se concilió contra documentación para evitar saldos incompletos.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-26 22:09 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
