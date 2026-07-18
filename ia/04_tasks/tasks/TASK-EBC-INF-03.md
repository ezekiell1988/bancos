# TASK-EBC-INF-03 — Diseño de autenticación y estrategia de contenedor para despliegue

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** INF
**Prioridad:** media
**Riesgo:** alto
**Aprobación:** pendiente

---

## Título

Diseño de autenticación y estrategia de contenedor para despliegue

## Contexto

El despliegue y el workflow de GitHub están intencionalmente deshabilitados porque todavía no se ha decidido contenedor, registro, plataforma ni autenticación.

## Objetivo

Documentar y aprobar las decisiones de seguridad, autenticación y contenedor antes de habilitar despliegue.

## Alcance permitido

* Investigar alternativas de contenedor y despliegue.
* Crear ADR de plataforma y autenticación.
* Definir secretos, permisos mínimos y estrategia de CI/CD.
* Proponer la reactivación del workflow sin ejecutarla.

## Fuera de alcance

* Publicar una imagen.
* Crear recursos Azure.
* Habilitar despliegue automático.
* Exponer la API sin autenticación.

## Criterios de aceptación

* [ ] Existe ADR aprobado para contenedor/plataforma.
* [ ] Existe tarea o diseño aprobado de autenticación.
* [ ] Se documentan secretos, permisos y controles de CI/CD.
* [ ] El workflow continúa deshabilitado hasta aprobación explícita posterior.

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `ia/06_decisions`
* `ia/04_tasks`
* `.github/workflows`

## Plan técnico

1. Comparar alternativas.
2. Definir modelo de amenazas mínimo.
3. Crear ADRs y backlog de implementación.
4. Revisar workflow propuesto.

## Pasos

1. Investigar.
2. Documentar ADR.
3. Crear tareas derivadas.
4. Revisar.

## Salida esperada

Decisiones aprobables para habilitar despliegue futuro.

## Validación

* [ ] Revisión de ADR
* [ ] Validación /ia
* [ ] Revisión de seguridad

## Rollback

Mantener el workflow deshabilitado; no hay recursos externos que revertir.

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

Riesgo alto: requiere aprobación explícita antes de implementar cualquier publicación. Trabajar directamente en main durante construcción.

## Issues vinculados

* ninguno
