# AGENTS.md — Bancos

## Workflow obligatorio

`/ia` es fuente de verdad, pero MCP `iaWorkflow` es interfaz operativa obligatoria.

1. Para tarea, consulta, planificación, revisión, diagnóstico o cierre: usar `ia_validate` y `ia_get_context` con intención adecuada.
2. Para tarea concreta: usar `work_task` antes de editar y `finish_task` al cerrar.
3. Para lecturas puntuales: usar `ia_read_file`, `ia_read_task` o `ia_search` solo cuando contexto MCP lo requiera.
4. Consultas a `dbbancos`: usar MCP `dbquery` y herramientas de solo lectura. No leer ni imprimir `.local-secrets/db.json`.

No recorrer `/ia` manualmente ni reemplazar este flujo por skills. Los skills técnicos se usan únicamente cuando tarea aprobada requiera un patrón especializado.

## Seguridad

* No exponer credenciales, IBAN, números de cuenta, saldos, transacciones ni archivos fuente en respuestas, logs, prompts o `/ia`.
* Escrituras MCP requieren preview y `apply: true`; operaciones destructivas requieren confirmación explícita.
* Solo implementar tareas `Lista`. Riesgo alto requiere aprobación explícita.
* Antes de publicar en Azure, crear y aprobar tarea de autenticación/seguridad.

## Contexto técnico

* Monolito .NET 10/C# 14 Minimal API por features + Angular standalone + MSSQL.
* Jobs con Hangfire/Hangfire.Console. Nunca pasar bytes de archivos a argumentos de jobs.
* Configuración .NET en `appsettings`; secretos solo en archivos locales ignorados.
