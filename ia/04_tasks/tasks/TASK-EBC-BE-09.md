# TASK-EBC-BE-09 — Aprendizaje de patrones de formato desde revisión

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 15:59 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Aprendizaje de patrones de formato desde revisión

## Contexto

El ZIP contiene archivos sin clasificación automática y la persona usuaria quiere clasificarlos uno por uno para reutilizar patrones sin almacenar contenido financiero.

## Objetivo

Guiar la clasificación y guardar firmas seguras de formato para detectar futuros archivos.

## Alcance permitido

* Mostrar archivos pendientes uno por uno.
* Guardar firmas estructurales seguras y tipo confirmado.
* Aplicar firmas aprendidas antes de reglas estáticas.

## Fuera de alcance

* Persistir bytes, saldos, movimientos, nombres sensibles o contenido de documentos.
* Cambiar reglas contables o publicar patrones.

## Criterios de aceptación

* [ ] La revisión presenta un archivo pendiente por vez.
* [ ] La confirmación guarda una firma segura reutilizable.
* [ ] Una firma equivalente clasifica futuros archivos automáticamente.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing`
* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Api/Data`
* `src/Bancos.Web/src/app/features/imports`

## Plan técnico

1. Definir firma segura de estructura.
2. Añadir persistencia y confirmación.
3. Priorizar firmas aprendidas.

## Pasos

1. Implementar modelo y detección.
2. Implementar UI guiada.
3. Validar con ejemplos no sensibles.

## Salida esperada

Clasificación guiada que aprende patrones locales seguros.

## Validación

* [ ] dotnet build y npm run build.
* [ ] Confirmar una clasificación y verificar detección posterior.

## Rollback

Revertir migración y patrones creados; no se almacenan archivos fuente.

## Dependencias

* TASK-EBC-BE-08

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 16:00 CR.

Análisis: 38 entradas permitidas; 9 listas, 24 requieren tipo, 5 sin extractor.

## Issues vinculados

* ninguno
