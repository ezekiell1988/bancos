# TASK-EBC-DOC-04 — Skill del proyecto para anotar documentos

**schemaVersion:** 2
**Estado:** En progreso
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** aprobación explícita
**Rama:** —
**createdAt:** 2026-10-25T19:57:36.605-06:00
**approvedAt:** 2026-10-25T20:00:20.362-06:00
**startedAt:** 2026-10-25T20:00:21.733-06:00
**finishedAt:** —
**Área:** DOC
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** doc, ia-workflow
**Requisitos:** REQ-001, REQ-002, REQ-003, REQ-004

---

## Título

Skill del proyecto para anotar documentos

## Contexto

El agente necesita un procedimiento corto y estable para leer fotos, PDF y Excel de BCR, BAC y facturas y anotarlos con el MCP bancos sin preguntar de más.

## Objetivo

Crear con el skill create-skill un skill del proyecto en `.agents/skills/` (visible también para Claude Code) que guíe cómo procesar un documento y anotarlo con el MCP bancos.

## Alcance permitido

* .agents/skills/
* .claude/skills/ (enlace)

## Fuera de alcance

* Cambiar los tools del MCP bancos (Fase 2)
* Copiar datos reales al skill

## Criterios de aceptación

* SKILL.md con name igual a la carpeta y description con triggers.
* Explica cuenta, clase, tipo, fecha, moneda, respaldo, duplicados y qué responderle al dueño.
* Remite a los tools del MCP bancos.
* Probado anotando un documento real del dueño.

## Archivos afectados / probables

* No definidos.

## Plan técnico

* Scaffolding con create-skill (scripts/new-skill.mjs).
* SKILL.md corto con el flujo y reglas de decisión.
* Enlace .claude/skills/<nombre> -> .agents/skills/<nombre>.

## Pasos

1. Crear el skill con scripts/new-skill.mjs de create-skill.
2. Redactar el procedimiento y las reglas de decisión.
3. Probarlo con un documento real del dueño.

## Salida esperada

Skill del proyecto que carga solo al recibir documentos bancarios o facturas.

## Validación

* El skill aparece en la lista de skills de una sesión nueva en el proyecto.
* Un documento real queda anotado con el MCP bancos.

## Reversión

Borrar la carpeta del skill y su enlace.

## Dependencias

* TASK-EBC-MCP-01

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"8b68d756-b483-4d5a-ad04-76d7dac9d960","taskId":"TASK-EBC-DOC-04","event":"created","occurredAt":"2026-10-25T19:57:36.605-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"37e40d52-b575-477d-badf-17aa11d3fbdb","taskId":"TASK-EBC-DOC-04","event":"approved","occurredAt":"2026-10-25T20:00:20.362-06:00","actor":"MCP iaWorkflow","reason":"aprobación explícita"}
{"schemaVersion":2,"eventId":"239e10f7-e60f-4ab2-a096-b3521ca334c0","taskId":"TASK-EBC-DOC-04","event":"started","occurredAt":"2026-10-25T20:00:21.733-06:00","actor":"MCP iaWorkflow","reason":"Inicio de implementación autorizado."}
```
