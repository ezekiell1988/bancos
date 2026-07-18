# TASK-EBC-BE-10 — Extractor de estados de tarjeta por formatos confirmados

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 16:34 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Extractor de estados de tarjeta por formatos confirmados

## Contexto

Los estados de tarjeta ya se identifican por patrones locales, pero no existe extractor. El usuario confirmó separar compras, pagos, intereses y cargos, preservando importes originales y equivalentes CRC para USD.

## Objetivo

Extraer estados de tarjeta CSV, XLS y PDF confirmados y registrar sus movimientos con semántica contable.

## Alcance permitido

* Crear parsers de tarjeta para CSV, XLS y PDF confirmados.
* Persistir movimientos con tipo de operación, moneda original y equivalente CRC.
* Habilitar los formatos de tarjeta cuando su extractor pase validación.

## Fuera de alcance

* Cambiar categorías contables ajenas a tarjeta.
* Publicar o modificar datos financieros de origen.

## Criterios de aceptación

* [ ] Cada formato de tarjeta confirmado extrae fecha, descripción, importe y moneda.
* [ ] Compras, pagos, intereses y cargos se diferencian.
* [ ] USD conserva importe original y equivalente CRC.
* [ ] Los estados identificados dejan de aparecer como sin extractor.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing`
* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Api/Domain`
* `src/Bancos.Api/Data`

## Plan técnico

1. Inspeccionar estructuras locales y definir contrato por formato.
2. Implementar parsers y normalización de movimientos.
3. Aplicar reglas contables por tipo de operación.
4. Validar contra archivos locales sin registrar datos sensibles.

## Pasos

1. Definir campos estructurales.
2. Implementar extractores.
3. Integrar con ImportJobs.
4. Validar importación y duplicados.

## Salida esperada

Estados de tarjeta importables con movimientos separados por naturaleza contable.

## Validación

* [ ] dotnet build
* [ ] dotnet test
* [ ] Prueba local de previsualización e importación sin exponer contenido.

## Rollback

Deshabilitar las plantillas de tarjeta y revertir la migración o cambios de parser.

## Dependencias

* TASK-EBC-BE-09

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Se implementaron extractores de estados de tarjeta CSV, XLS/HTML y PDF con clasificación de compras, pagos, intereses y cargos; se habilitaron las plantillas BAC de tarjeta y se preservan importes USD junto con CRC (desde el documento o tipo de cambio diario).

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 16:34 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
