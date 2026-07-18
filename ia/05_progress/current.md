# Progreso actual

> **Última actualización:** 2026-07-18 CR (TASK-EBC-INF-02 completada)

## En curso

* Descubrimiento de requisitos financieros.
* Firmas de siete plantillas documentadas; faltan validación de XLS binario y semántica de CSV de crédito durante implementación.

## Completado en sesión actual

* Creado `/ia`: contexto, requisitos, arquitectura, plan, tareas, progreso, ADRs, issues, retrospectiva, templates, prompts y skills de workflow.
* Inspeccionados formatos de primera carga de forma anonimizada y documentados detectores/validaciones.
* Configurados MCP `iaWorkflow` y `dbquery`; smoke tests completos y configuración de Codex/VS Code/Claude actualizada.

## Próximo

* Completar preguntas de requisitos.
* Auditar estructura `/ia`.
* Abrir una sesión nueva de Codex para cargar MCP nativos.
* Aprobar y ejecutar `TASK-EZ-BE-01` mediante `iaWorkflow`.

## Completado en sesiones recientes

* **2026-07-18** — TASK-EBC-INF-02 cerrada: Aplicada y verificada la migración de importaciones; el workflow de despliegue quedó deshabilitado y documentado hasta definir el contenedor. Se preparó la publicación de los cambios del proyecto. — EBC

* **2026-07-18** — TASK-EBC-BE-02 cerrada: Implementado detector de plantillas por firma de contenido para CSV, HTML/XLS, XLS BIFF y PDF; lector inicial BCR débito CSV con validación y persistencia idempotente mediante huella de movimiento. El job de Hangfire recibe ImportId y registra sus etapas. — EBC

* **2026-07-18** — TASK-EBC-INF-01 cerrada: Se inicializó Hangfire localmente y se verificaron su esquema SQL y dashboard. — EBC

* **2026-07-18** — TASK-EBC-BE-01 cerrada: Se agregó carga segura de appsettings.Development.json y db.json para SQL, reutilizada por EF Core y Hangfire. — EBC

* **2026-07-18** — TASK-EBC-DB-01 cerrada: Se aplicó y verificó la migración inicial InitialCreate en dbbancos sin insertar datos de negocio. — EBC

* **2026-07-18** — TASK-EZ-BE-01 cerrada: Se creó la base .NET 10 con EF Core/MSSQL, Hangfire, importación temporal regenerable, esquema inicial y pruebas. — EBC
