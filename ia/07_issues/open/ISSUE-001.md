# ISSUE-001 — Faltan datos semilla para catálogo y auxiliares derivados de plantillas

**Severidad:** medium
**Estado:** abierto
**Componente:** database
**Detectado:** 2026-07-18 14:49 CR
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`

---

## Síntoma

La UI de importaciones no puede seleccionar auxiliares porque AccountAuxiliaries está vacío.

## Causa raíz

TASK-EBC-BE-02 implementó detección de firmas y persistencia limitada; TASK-EBC-BE-03 expuso endpoints, pero no se creó un seed/migración de catálogo y auxiliares derivados de las plantillas documentadas.

## Workaround

Crear propietarios, cuentas y auxiliares manualmente mediante endpoints existentes.

## Fix propuesto

Crear tarea aprobada para consolidar plantillas documentadas, definir catálogo contable base y auxiliares no sensibles, generar migración EF idempotente y verificar con consultas dbquery.

## Tareas vinculadas

* TASK-EBC-BE-02
* TASK-EBC-BE-03
* TASK-EBC-FE-02
