# TASK-EBC-DOC-05 — Grupo de prueba de Telegram para Bancos

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** —
**Rama:** —
**createdAt:** 2026-10-25T20:05:41.126-06:00
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** DOC
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** doc, ia-workflow
**Requisitos:** REQ-007

---

## Título

Grupo de prueba de Telegram para Bancos

## Contexto

El dueño quiere usar el proyecto Bancos desde un grupo de Telegram que compartirá con su pareja. Antes de sumarla, se prueba con un grupo donde solo están él y el bot @MacEzequielBot. Hoy el bot solo contesta en grupos donde están únicamente las cuentas del dueño; con otra persona adentro ignora el grupo o la trata como usuario de consulta (solo /ia, sin anotar).

## Objetivo

Dejar funcionando un grupo de prueba (dueño + bot) vinculado al proyecto Bancos, anotar un documento desde ahí y dejar documentado qué falta en el bot para sumar a la pareja.

## Alcance permitido

* Configurar el grupo de prueba en Telegram (lo crea el dueño; el bot como administrador).
* Vincular el grupo o su tema al proyecto Bancos con /fijar.
* Probar anotar y consultar movimientos desde el grupo con el skill record-movements.
* Anotar en /ia lo que falta en telegram-agent-remote para la pareja.

## Fuera de alcance

* Cambiar el código de telegram-agent-remote (va en su propio proyecto).
* Sumar a la pareja al grupo.
* Usar los grupos de ClickEat.

## Criterios de aceptación

* El grupo de prueba existe, solo con el dueño y el bot, y el bot responde ahí con el proyecto Bancos.
* Un documento mandado al grupo queda anotado en datos/ con su respaldo en datos/multimedia/.
* Queda registrada en /ia la decisión de qué podrá hacer la pareja y qué cambio necesita el bot.

## Archivos afectados / probables

* datos/
* ia/

## Plan técnico

* El dueño crea el grupo en Telegram y agrega a @MacEzequielBot como administrador.
* Desde el grupo, /fijar bancos para vincular el proyecto (o un tema llamado Bancos).
* Mandar un documento de prueba y verificar con bancos_movimientos.
* Revisar en telegram-agent-remote el modelo de viewers/allowedUserIds y definir el rol de la pareja.

## Pasos

1. Crear el grupo y sumar el bot como administrador.
2. Vincular el proyecto Bancos.
3. Mandar un documento y confirmar que se anota.
4. Consultar el resumen del mes desde el grupo.
5. Registrar la decisión del rol de la pareja.

## Salida esperada

Grupo de prueba funcionando con Bancos y decisión registrada sobre el rol de la pareja.

## Validación

* bancos_movimientos muestra el movimiento anotado desde el grupo.
* El archivo de respaldo existe en datos/multimedia/AAAA-MM/.
* ia_validate sin errores.

## Reversión

Borrar el movimiento de prueba con bancos_borrar_movimiento y sacar al bot del grupo.

## Dependencias

* TASK-EBC-DOC-04

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"a7676652-2c80-4f22-858a-c8f6cc510726","taskId":"TASK-EBC-DOC-05","event":"created","occurredAt":"2026-10-25T20:05:41.126-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
```
