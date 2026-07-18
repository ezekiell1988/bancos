# TASK-EBC-QA-01 — Revisión final colaborativa de funcionalidad

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 15:07 CR
**Fecha cierre:** —
**Área:** QA
**Prioridad:** media
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Revisión final colaborativa de funcionalidad

## Contexto

Se requiere una revisión final de la funcionalidad del proyecto con participación directa del usuario.

## Objetivo

Validar junto al usuario los flujos implementados y registrar hallazgos para cierre o corrección.

## Alcance permitido

* Ejecutar pruebas manuales en la interfaz local.
* Validar importación, estado, revisión y datos semilla.
* Registrar hallazgos y crear tareas/issues derivados.

## Fuera de alcance

* Despliegue público.
* Cambios no identificados durante la revisión.

## Criterios de aceptación

* [ ] Usuario y agente recorren los escenarios acordados.
* [ ] Cada hallazgo queda registrado y priorizado.
* [ ] Se emite decisión de cierre o lista de pendientes.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `ia/07_issues`
* `ia/04_tasks`
* `src/Bancos.Web`
* `src/Bancos.Api`

## Plan técnico

1. Preparar checklist de pruebas.
2. Guiar revisión en localhost.
3. Documentar resultados y acciones.

## Pasos

1. Aprobar la tarea.
2. Revisar flujo de importación con el usuario.
3. Revisar estados y pendientes.
4. Registrar resultados.

## Salida esperada

Checklist final validado y trazabilidad de cualquier pendiente.

## Validación

* [ ] Prueba manual colaborativa en https://localhost:4200
* [ ] Validación de endpoints locales relevantes

## Rollback

No aplica; la tarea es de revisión.

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

* Pendiente de revisión: Se levantó el entorno local Bancos desde .codex/environments/bancos.toml y se revisó colaborativamente la interfaz de Importaciones en https://localhost:4200/imports. El diagnóstico visual mostró que el formulario de importación estaba limitado a 640 px frente a un hero de 840 px; se registró ISSUE-002, se implementó y verificó su corrección en TASK-EBC-FE-03. También se revisó la arquitectura CSS contra documentación oficial de Angular y MDN: se identificó mezcla de tokens, estilos globales, layout y pantalla en un único styles.css; se registró ISSUE-003, se implementó en TASK-EBC-FE-04 una arquitectura con tokens globales, estilos compartidos y estilos encapsulados por componente. Se creó el skill angular-css-architecture (TASK-EBC-DOC-02) y se sincronizaron 56 skills entre .agents, .claude y .codex (TASK-EBC-DOC-03).

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 15:09 CR.

Requiere participación del usuario para confirmar UX y comportamiento.

## Issues vinculados

* ninguno
