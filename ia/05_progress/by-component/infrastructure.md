> **Última actualización:** 2026-07-20 CR (TASK-EBC-MCP-02 completada)



## Completado

* **2026-07-20** — TASK-EBC-MCP-02: Se registró Bancos.Mcp como servidor MCP HTTP local de VS Code y se documentó el arranque HTTPS y el diagnóstico. El endpoint confirmó tools/list y tools/call para health_status. — EBC

* **2026-07-20** — TASK-EBC-INF-09: Configurado VS Code con launch de dotnet watch para la API en el puerto 8000, Angular ng serve HTTPS en el 4200 y compound Full Stack. — EBC

* **2026-07-20** — TASK-EBC-INF-08: SQL Server 2022 en Docker configurado y validado. Los 19 archivos de src/input.zip se importaron correctamente. Bugs corregidos: SingleOrDefaultAsync→FirstOrDefaultAsync en ClassificationModule, seed de categoría General, ChangeTracker.Clear()+Attach en catch blocks para fallo correcto, BalanceRegex con \\s para non-breaking spaces de PdfPig, race condition en LoanStatements con re-attach del Import. Nuevo endpoint POST /api/imports/{id}/retry. — EBC

* **2026-07-20** — TASK-EBC-INF-07: SQL Server local levantado en Docker (puerto 1433), migraciones EF aplicadas, db.json apunta a localhost. appsettings.Development.json limpiado de credenciales hardcodeadas. Procedimiento documentado en ia/06_decisions/DEV-ENV-local-sqlserver.md. — EBC

* **2026-07-18** — TASK-EBC-INF-06: Script de arranque reubicado en .codex y referencias actualizadas. — EBC

* **2026-07-18** — TASK-EBC-INF-05: La acción del entorno ahora nombra e invoca explícitamente el arranque integrado de API y web. — EBC

* **2026-07-18** — TASK-EBC-INF-04: Entorno reutilizable de Bancos creado; reemplaza dos configuraciones ajenas y arranca el desarrollo HTTPS integrado. — EBC

* **2026-07-18** — TASK-EBC-INF-02: Aplicada y verificada la migración de importaciones; el workflow de despliegue quedó deshabilitado y documentado hasta definir el contenedor. Se preparó la publicación de los cambios del proyecto. — EBC

* **2026-07-18** — TASK-EBC-INF-01: Se inicializó Hangfire localmente y se verificaron su esquema SQL y dashboard. — EBC
