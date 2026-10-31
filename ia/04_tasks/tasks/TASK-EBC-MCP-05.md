# TASK-EBC-MCP-05 — Pruebas del proyecto corribles con npm test y desde /pruebas

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** —
**Rama:** —
**createdAt:** 2026-10-30T11:09:32.280-06:00
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** mcp, ia-workflow
**Requisitos:** —

---

## Título

Pruebas del proyecto corribles con npm test y desde /pruebas

## Contexto

El bot de Telegram (telegram-agent-remote) tiene /pruebas: corre "npm run test -- --watch=false" en la carpeta de un proyecto de su lista blanca (config.tests.projects) y devuelve el resultado sin secretos. Este proyecto tiene .mcp/bancos/tests/smoke.mjs, pero .mcp no tiene package.json. Sin un script "test" el proyecto no puede entrar en la lista.

## Objetivo

Que "npm run test -- --watch=false" en .mcp corra todas las pruebas sin red, sin costo y sin tocar datos reales, termine solo y devuelva código distinto de 0 si algo falla; y dejar el proyecto en la lista de /pruebas.

## Alcance permitido

* .mcp/package.json nuevo con script "test" (sin dependencias nuevas salvo que el smoke las necesite).
* .mcp/bancos/tests/: ajustes para que trabaje sobre datos temporales.
* README del proyecto: cómo correr las pruebas.
* config.json del bot (no versionado): entrada en tests.projects.

## Fuera de alcance

* Pruebas que gastan dinero, usan red real o cambian datos reales.
* Cambiar el comportamiento del producto o de las tools.
* Cambios en el código del bot de Telegram.

## Criterios de aceptación

* "npm run test -- --watch=false" en .mcp pasa en esta Mac y termina en menos de 10 minutos.
* Si una prueba falla, el comando sale con código distinto de 0.
* Ninguna prueba incluida usa red real, cobra ni escribe fuera de un directorio temporal.
* /pruebas bancos desde Telegram devuelve el resultado.

## Archivos afectados / probables

* .mcp/package.json
* .mcp/bancos/tests/smoke.mjs

## Plan técnico

* Crear .mcp/package.json con "type": "module" y script "test" que corre bancos/tests/smoke.mjs.
* Confirmar que el smoke usa una copia temporal de datos/ y nunca los movimientos reales.

## Pasos

1. Correr a mano las pruebas actuales de .mcp y anotar cuáles pasan, cuáles fallan y cuáles usan red o costo.
2. Crear .mcp/package.json con el script "test".
3. Agregar { "name": "bancos", "path": "~/Documents/ezekl/Bancos/.mcp", "runner": "npm" } a tests.projects del config.json del bot y reiniciarlo.
4. Probar /pruebas bancos desde Telegram.

## Salida esperada

Script "test" en .mcp/package.json que agrupa las pruebas sin red, y el proyecto bancos en la lista de /pruebas.

## Validación

* cd ~/Documents/ezekl/Bancos/.mcp && npm run test -- --watch=false
* /pruebas bancos en Telegram

## Reversión

Quitar el script "test" y la entrada de tests.projects; las pruebas existentes no cambian.

## Dependencias

* Ninguna.

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"3ff82a8d-f44f-428e-83a3-bd84136afdb4","taskId":"TASK-EBC-MCP-05","event":"created","occurredAt":"2026-10-30T11:09:32.280-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
```
