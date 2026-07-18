# Bancos — instrucciones de Copilot

MCP `iaWorkflow` opera `/ia` y es obligatorio para cualquier consulta, planificación, implementación, revisión, diagnóstico o cierre.

1. Llama `ia_validate` y luego `ia_get_context` con la intención adecuada.
2. Usa `work_task` antes de editar y `finish_task` al cerrar. Implementa solo tareas `Lista`; riesgo alto exige aprobación explícita.
3. Usa `ia_read_file`, `ia_read_task` e `ia_search` solo como lecturas enrutadas.
4. Para `dbbancos`, usa MCP `dbquery` de solo lectura. No leas, copies ni muestres secretos de `.local-secrets`.

No recorras `/ia` manualmente. Mantén datos financieros, identificadores bancarios, archivos fuente y secretos fuera de prompts, logs y documentación.
