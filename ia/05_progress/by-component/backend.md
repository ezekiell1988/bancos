> **Última actualización:** 2026-07-18 CR (TASK-EBC-BE-03 completada)



## Completado

* **2026-07-18** — TASK-EBC-BE-03: Se agregaron endpoints mínimos de propietarios, cuentas y auxiliares; el upload BCR ahora agenda mediante Hangfire y el job es idempotente tras completar. Se eliminó la precisión decimal global para usar los valores predeterminados de EF y se añadió la migración correspondiente. — EBC

* **2026-07-18** — TASK-EBC-BE-02: Implementado detector de plantillas por firma de contenido para CSV, HTML/XLS, XLS BIFF y PDF; lector inicial BCR débito CSV con validación y persistencia idempotente mediante huella de movimiento. El job de Hangfire recibe ImportId y registra sus etapas. — EBC

* **2026-07-18** — TASK-EBC-BE-01: Se agregó carga segura de appsettings.Development.json y db.json para SQL, reutilizada por EF Core y Hangfire. — EBC

* **2026-07-18** — TASK-EZ-BE-01: Se creó la base .NET 10 con EF Core/MSSQL, Hangfire, importación temporal regenerable, esquema inicial y pruebas. — EBC
