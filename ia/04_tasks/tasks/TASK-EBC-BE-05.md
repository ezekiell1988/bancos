# TASK-EBC-BE-05 — Lector de préstamos Coopealianza en PDF

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Lector de préstamos Coopealianza en PDF

## Contexto

El detector reconoce coopealianza-loan-pdf-v1, pero aún no se extrae la tabla de pagos ni se valida su composición.

## Objetivo

Extraer saldo y detalle de pagos de préstamos Coopealianza desde PDF por firma de contenido.

## Alcance permitido

* Implementar extractor coopealianza-loan-pdf-v1.
* Validar capital, interés, mora, otros y total.
* Persistir saldo y pagos estructurados asociados al auxiliar.
* Crear fixtures anonimizados y pruebas.

## Fuera de alcance

* Subir PDFs reales.
* Clasificar pagos automáticamente.
* Procesar snapshots de tarjeta BAC.

## Criterios de aceptación

* [ ] Un PDF fixture detecta la plantilla correcta.
* [ ] Cada fila valida capital + interés + mora + otros = total cuando exista.
* [ ] El saldo y los pagos se persisten de forma idempotente.
* [ ] Errores dejan auditoría.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing`
* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Api/Data`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Definir modelo de préstamo y pagos.
2. Implementar extracción con PdfPig.
3. Validar y persistir.
4. Agregar pruebas.

## Pasos

1. Modelar préstamos.
2. Implementar parser.
3. Integrar job.
4. Validar.

## Salida esperada

Importación de préstamo PDF verificable.

## Validación

* [ ] dotnet test
* [ ] dotnet build
* [ ] Pruebas PDF de extracción y conciliación

## Rollback

Deshabilitar el extractor PDF conservando trazabilidad.

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

Trabajar directamente en main durante construcción.

## Issues vinculados

* ninguno
