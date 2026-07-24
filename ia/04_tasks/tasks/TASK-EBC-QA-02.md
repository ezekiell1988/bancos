# TASK-EBC-QA-02 — Fix 4 test failures in Bancos.Api.Tests: detection text, Coopealianza regex, AccountKind enum converter

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-24 10:24 CR
**Fecha cierre:** —
**Área:** QA
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Fix 4 test failures in Bancos.Api.Tests: detection text, Coopealianza regex, AccountKind enum converter

## Contexto

Al correr los tests de Bancos.Api.Tests tras cambios recientes quedan 4 en rojo: detector de BAC credit PDF, parser Coopealianza en Api, re-proceso de préstamo, e integración BCR por enum sin converter.

## Objetivo

Dejar todos los tests de Bancos.Api.Tests en verde sin alterar comportamiento de producción.

## Alcance permitido

* tests/Bancos.Api.Tests/ImportTemplateDetectorTests.cs
* src/Bancos.Api/Features/Parsing/CoopealianzaLoanPdfParser.cs
* src/Bancos.Api/Domain/Entities.cs

## Fuera de alcance

* Cambios al MCP
* Migraciones
* Frontend

## Criterios de aceptación

* [ ] dotnet test Bancos.Api.Tests pasa al 100%

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Actualizar texto de prueba en Recognizes_pdf_and_html_signatures para incluir 'Fecha de pago de contado'
2. Relajar BalanceRegex del Api CoopealianzaLoanPdfParser (: y ₡ opcionales)
3. Agregar [JsonConverter(typeof(JsonStringEnumConverter<AccountKind>))] al enum AccountKind

## Pasos

1. Editar ImportTemplateDetectorTests.cs test text
2. Editar CoopealianzaLoanPdfParser.cs BalanceRegex
3. Editar Entities.cs enum AccountKind
4. dotnet test

## Salida esperada

47 tests passing, 0 failing

## Validación

* [ ] dotnet test Bancos.Api.Tests

## Rollback

git revert

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

* Aprobada por usuario el 2026-07-24 10:24 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
