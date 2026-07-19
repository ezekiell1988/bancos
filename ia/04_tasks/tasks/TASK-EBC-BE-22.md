# TASK-EBC-BE-22 — Mostrar progreso en tiempo real de jobs de importación

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 19:33 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Mostrar progreso en tiempo real de jobs de importación

## Contexto

Los jobs de importación pueden permanecer varios minutos clasificando movimientos sin producir señales visibles entre los hitos inicial y final. Hangfire.Console ya está instalado y el proxy Angular ya permite WebSocket en /hubs, pero la API no registra SignalR y el frontend no mantiene progreso por importación. El seguimiento debe usar únicamente etapas, conteos y porcentajes sanitizados.

## Objetivo

Implementar progreso observable, persistente y en tiempo real para cada importación, visible tanto en Hangfire como en Angular, sin exponer datos financieros ni alterar la atomicidad o idempotencia del job.

## Alcance permitido

* Modelo y migración para progreso de importación separado de las transacciones financieras
* IImportProgressReporter con actualización limitada por porcentaje o intervalo
* Barra y eventos sanitizados en Hangfire.Console
* Hub SignalR fuertemente tipado y grupos por ImportId
* Snapshot REST de progreso y reconexión/polling de respaldo
* Barra de progreso accesible y etapa actual en la pantalla Angular de importaciones
* Pruebas automatizadas de reporter, hub, endpoints y presentación de progreso

## Fuera de alcance

* Autenticación, autorización, Azure SignalR o backplane para múltiples instancias
* Cambiar reglas financieras, parsers, clasificación o persistencia de movimientos
* Optimizar o agrupar llamadas a Azure AI
* Registrar nombres, descripciones, importes, cuentas, saldos o contenido de archivos

## Criterios de aceptación

* [ ] Cada importación expone estado, etapa, cantidad procesada, total, porcentaje de 0 a 100 y fecha de actualización usando ImportId como correlación.
* [ ] Hangfire.Console muestra una barra de progreso y solo registra cambios de etapa o hitos limitados; no escribe una línea por movimiento.
* [ ] Las actualizaciones se limitan a un cambio mínimo de 1% o un intervalo configurable de al menos 1 segundo, además de estados terminales.
* [ ] El progreso se persiste fuera de la transacción financiera principal y permanece consultable después de recargar la página o reconectar SignalR.
* [ ] SignalR publica únicamente DTO sanitizado al grupo de la importación; el frontend obtiene primero el snapshot REST y luego aplica eventos en tiempo real.
* [ ] La UI muestra etapa, X de N y una barra accesible; maneja Queued, Processing, Completed y Failed sin quedar indefinidamente en un estado engañoso.
* [ ] Un reintento reinicia el intento de progreso de forma explícita y el porcentaje no retrocede dentro del mismo intento.
* [ ] Los jobs continúan recibiendo solo ImportId y conservan su comportamiento idempotente y atómico.
* [ ] No se exponen datos financieros, nombres de archivos ni identificadores bancarios en logs, eventos o respuestas de progreso.
* [ ] Compilación, pruebas .NET, build Angular y verificación local con un job largo finalizan correctamente.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Program.cs`
* `src/Bancos.Api/Domain/Entities.cs`
* `src/Bancos.Api/Data/BancosDbContext.cs`
* `src/Bancos.Api/Features/Imports/ImportJobs.cs`
* `src/Bancos.Api/Features/Imports/ImportsModule.cs`
* `src/Bancos.Api/Features/Imports/ImportProgressHub.cs`
* `src/Bancos.Api/Features/Imports/ImportProgressReporter.cs`
* `src/Bancos.Api/Migrations`
* `src/Bancos.Web/src/app/features/imports/data-access`
* `src/Bancos.Web/src/app/features/imports/pages/imports-page.ts`
* `src/Bancos.Web/src/app/features/imports/pages/imports-page.css`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Definir etapas y DTO sanitizado de progreso con Attempt, Current, Total, Percent, Status y UpdatedUtc.
2. Persistir el último snapshot en una entidad o tabla ImportProgress independiente para que los reportes no confirmen parcialmente movimientos financieros.
3. Crear IImportProgressReporter que coordine persistencia, Hangfire.Console e IHubContext y coalesca actualizaciones por porcentaje y tiempo.
4. Instrumentar detección, lectura, clasificación, persistencia y estados terminales; calcular el porcentaje de clasificación sobre el total conocido.
5. Registrar SignalR con AddSignalR y MapHub bajo /hubs; suscribir clientes a grupos basados en ImportId.
6. Exponer snapshot REST junto con la importación o mediante endpoint dedicado.
7. Agregar cliente SignalR Angular con reconexión automática y polling limitado como respaldo.
8. Mostrar barra accesible, etapa y conteo por importación, conservando el enlace al dashboard Hangfire.
9. Cubrir throttling, monotonicidad, reintentos, estados terminales, reconexión y ausencia de datos sensibles con pruebas.

## Pasos

1. Diseñar contrato de progreso y persistencia independiente.
2. Implementar reporter y progreso de Hangfire.Console.
3. Agregar Hub SignalR, grupos y snapshot REST.
4. Instrumentar ImportJobs sin cambiar su lógica financiera.
5. Implementar cliente Angular y barra de progreso.
6. Validar pruebas, build y un job largo real no sensible.

## Salida esperada

Cada importación muestra avance confiable en Hangfire y en la aplicación Angular, con recuperación tras recarga o desconexión y sin saturar logs ni exponer información financiera.

## Validación

* [ ] dotnet build
* [ ] dotnet test
* [ ] npm run build
* [ ] Pruebas de throttling y monotonicidad del progreso
* [ ] Prueba de reconexión SignalR y snapshot REST
* [ ] Verificación local de Hangfire.Console y barra Angular durante un job largo
* [ ] ia_validate

## Rollback

Retirar Hub, reporter y UI de progreso; revertir la migración o tabla ImportProgress. Los jobs vuelven a mostrar solo estados globales y continúan ejecutándose con ImportId.

## Dependencias

* TASK-EBC-FE-06
* TASK-EBC-BE-21

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 19:35 CR.

Diseño recomendado: SignalR es la vía rápida de notificación; el snapshot persistido es la fuente de verdad. Usar ImportId, no el identificador interno de Hangfire, como clave pública de correlación.

## Issues vinculados

* ninguno
