# CLAUDE.md — Bancos

## MCP primero

Usa MCP `iaWorkflow` para todo trabajo del proyecto:

* Llama `ia_validate` y `ia_get_context` antes de planificar, implementar, revisar, depurar o cerrar.
* Usa `work_task` antes de editar y `finish_task` al finalizar.
* Usa lecturas MCP puntuales; no recorras `/ia` manualmente.
* Para SQL en `dbbancos`, usa `dbquery` de solo lectura; nunca reveles contenido de `.local-secrets/db.json`.

## Reglas

* Trabaja únicamente tareas `Lista`; tareas de riesgo alto requieren aprobación explícita.
* No expongas datos financieros, identificadores bancarios, credenciales ni archivos de importación.
* Escritos de workflow: preview primero, `apply: true` solo tras aprobación.
* Stack: .NET 10/C# 14 Minimal APIs por features, Angular standalone, MSSQL y Hangfire.Console.
