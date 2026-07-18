# TASK-EBC-BE-11 — Extractor de movimientos de cuenta XLS y HTML

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 16:40 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Extractor de movimientos de cuenta XLS y HTML

## Contexto

El ZIP contiene movimientos de cuenta correctamente identificados en variantes XLS/HTML que aparecen sin extractor. El parser actual solo acepta el CSV BCR.

## Objetivo

Extraer de forma segura movimientos de cuenta desde las variantes XLS y HTML confirmadas.

## Alcance permitido

* Crear parsers específicos XLS y HTML.
* Extraer fecha, referencia, descripción, débito, crédito, moneda y saldos cuando existan.
* Habilitar las plantillas de movimientos cuando validen.

## Fuera de alcance

* Cambiar reglas de tarjeta o préstamos.
* Interpretar metadatos de sistema como documentos.

## Criterios de aceptación

* [ ] Cada variante confirmada extrae movimientos con fecha, referencia, descripción y dirección.
* [ ] El balance se valida cuando el formato entrega saldo inicial/final.
* [ ] Las variantes dejan de mostrarse como sin extractor.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing`
* `src/Bancos.Api/Features/Imports/ImportJobs.cs`
* `src/Bancos.Api/Features/Imports/ImportsModule.cs`

## Plan técnico

1. Excluir metadatos de macOS.
2. Inspeccionar encabezados estructurales de XLS/HTML.
3. Implementar parser por variante.
4. Integrar y validar conciliación.

## Pasos

1. Definir contratos de parser.
2. Implementar lecturas XLS/HTML.
3. Habilitar plantillas.
4. Validar contra ZIP local.

## Salida esperada

Movimientos de cuenta XLS/HTML importables y reconciliados.

## Validación

* [ ] dotnet build
* [ ] dotnet test
* [ ] Previsualización local sin datos expuestos.

## Rollback

Deshabilitar las plantillas XLS/HTML y revertir parsers.

## Dependencias

* TASK-EBC-BE-08
* TASK-EBC-BE-09

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 16:40 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
