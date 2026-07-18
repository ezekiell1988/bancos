# Progreso actual

> **Última actualización:** 2026-07-18 CR (TASK-EBC-FE-02 completada)

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

* **2026-07-18** — TASK-EBC-FE-02 cerrada: Pantalla de importaciones renovada con arrastrar y soltar múltiples archivos, cola de carga y auxiliares disponibles. — EBC

* **2026-07-18** — TASK-EBC-DB-02 cerrada: Base reinicializada con una única migración inicial que incluye catálogo contable y auxiliares semilla no sensibles. — EBC

* **2026-07-18** — TASK-EBC-INF-06 cerrada: Script de arranque reubicado en .codex y referencias actualizadas. — EBC

* **2026-07-18** — TASK-EBC-INF-05 cerrada: La acción del entorno ahora nombra e invoca explícitamente el arranque integrado de API y web. — EBC

* **2026-07-18** — TASK-EBC-INF-04 cerrada: Entorno reutilizable de Bancos creado; reemplaza dos configuraciones ajenas y arranca el desarrollo HTTPS integrado. — EBC

* **2026-07-18** — TASK-EBC-FE-01 cerrada: Dashboard Angular standalone minimalista por features para importaciones y revisión, servido por la API, con proxy HTTPS y arranque integrado en modo watch. — EBC

* **2026-07-18** — TASK-EBC-BE-06 cerrada: Implementada y validada clasificación determinística de movimientos: coincidencia exacta aprobada, reglas por patrón, categoría General pendiente de revisión y endpoints mínimos de categorías, reglas y revisión. — EBC

* **2026-07-18** — TASK-EBC-BE-05 cerrada: Implementado extractor PDF Coopealianza con validación de saldo y composición de pagos, persistencia idempotente de estados y pagos, migración SQL y pruebas con fixture PDF anonimizado. — EBC

* **2026-07-18** — TASK-EBC-DOC-01 cerrada: ADR-02 y 00_context.md actualizados para que activos y pasivos USD generen diferencial cambiario. — EBC

* **2026-07-18** — TASK-EBC-BE-04 cerrada: Implementado el lector BAC de financiamientos XLS binario con persistencia idempotente por auxiliar. — EBC

* **2026-07-18** — TASK-EBC-BE-03 cerrada: Se agregaron endpoints mínimos de propietarios, cuentas y auxiliares; el upload BCR ahora agenda mediante Hangfire y el job es idempotente tras completar. Se eliminó la precisión decimal global para usar los valores predeterminados de EF y se añadió la migración correspondiente. — EBC

* **2026-07-18** — TASK-EBC-INF-02 cerrada: Aplicada y verificada la migración de importaciones; el workflow de despliegue quedó deshabilitado y documentado hasta definir el contenedor. Se preparó la publicación de los cambios del proyecto. — EBC

* **2026-07-18** — TASK-EBC-BE-02 cerrada: Implementado detector de plantillas por firma de contenido para CSV, HTML/XLS, XLS BIFF y PDF; lector inicial BCR débito CSV con validación y persistencia idempotente mediante huella de movimiento. El job de Hangfire recibe ImportId y registra sus etapas. — EBC

* **2026-07-18** — TASK-EBC-INF-01 cerrada: Se inicializó Hangfire localmente y se verificaron su esquema SQL y dashboard. — EBC

* **2026-07-18** — TASK-EBC-BE-01 cerrada: Se agregó carga segura de appsettings.Development.json y db.json para SQL, reutilizada por EF Core y Hangfire. — EBC

* **2026-07-18** — TASK-EBC-DB-01 cerrada: Se aplicó y verificó la migración inicial InitialCreate en dbbancos sin insertar datos de negocio. — EBC

* **2026-07-18** — TASK-EZ-BE-01 cerrada: Se creó la base .NET 10 con EF Core/MSSQL, Hangfire, importación temporal regenerable, esquema inicial y pruebas. — EBC
