---
description: Investigar y resolver un issue registrado en /ia/07_issues
---

## Leer antes de empezar

- `ia/00_context.md`
- `ia/02_architecture.md`
- `ia/07_issues/current.md`
- El archivo del issue: `ia/07_issues/open/{ISSUE-ID}.md`
- `ia/06_decisions.md` (si el issue puede estar relacionado con una decisión arquitectónica)

## Objetivo

Diagnosticar la causa raíz del issue y proponer o implementar un fix.

## Reglas

- No inventar causas raíz — buscar evidencia en código, logs o tests.
- Si el fix implica una decisión arquitectónica nueva, registrar un ADR en `06_decisions.md`.
- Al resolver: consolidar el resumen en `07_issues/archive/{YYYY-MM}.md` y eliminar el archivo individual.
- Actualizar `07_issues/current.md` para reflejar el cierre.

## Salida esperada

- Fix implementado o workaround documentado.
- Issue cerrado en `/ia` si se resolvió.
