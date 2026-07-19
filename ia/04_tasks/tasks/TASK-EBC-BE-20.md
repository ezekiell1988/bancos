# TASK-EBC-BE-20 — Soportar estados de tarjeta BAC sin filas de movimientos

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 18:56 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Soportar estados de tarjeta BAC sin filas de movimientos

## Contexto

Jobs afectados: 4, 6, 7, 12, 13, 14, 17 y 18. Los PDF fallan por no detectar filas con fecha e importe; los CSV/resúmenes no reconocen su tabla. No incluir contenido de estados ni datos financieros en la tarea.

## Objetivo

Resolver el procesamiento de los estados de tarjeta BAC que fallan al no reconocer movimientos extraídos del PDF o del resumen CSV.

## Alcance permitido

* CardStatementParser y detector de plantillas BAC
* Modelo/persistencia de resumen de tarjeta si fuera imprescindible
* Pruebas anonimizadas de parsing y consulta read-only de estados Hangfire

## Fuera de alcance

* Cambiar archivos fuente del usuario
* Enviar datos a proveedores de IA
* Modificar credenciales o despliegue

## Criterios de aceptación

* [ ] Los jobs 4, 6, 7, 12, 13, 14, 17 y 18 tienen una causa reproducible documentada sin datos sensibles.
* [ ] El lector soporta los formatos confirmados o los deriva a revisión manual segura.
* [ ] No se crean movimientos sintéticos a partir de saldos sin semántica contable.
* [ ] Pruebas automatizadas pasan y los jobs se reencolan manualmente una vez.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing/CardStatementParser.cs`
* `src/Bancos.Api/Features/Imports/ImportJobs.cs`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Caracterizar de forma estructural los extractos sin exponer texto.
2. Separar movimientos transaccionales de resúmenes/saldos.
3. Implementar parser o entidad de resumen según semántica comprobada.
4. Validar con fixtures anonimizadas y Hangfire.

## Pasos

1. Analizar estructura.
2. Implementar soporte.
3. Reencolar una vez y verificar estados.

## Salida esperada

Los formatos BAC afectados se procesan con datos contables explícitos o quedan en revisión manual con diagnóstico estructurado, sin inventar movimientos.

## Validación

* [ ] dotnet test
* [ ] Consulta dbquery de estados por job
* [ ] Verificación del dashboard Hangfire

## Rollback

Revertir únicamente parser/modelo de estados de tarjeta; los archivos fallidos permanecen disponibles.

## Dependencias

* TASK-EBC-BE-19

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

Sin notas adicionales.

## Issues vinculados

* ninguno
