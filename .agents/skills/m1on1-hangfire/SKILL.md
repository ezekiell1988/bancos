---
name: m1on1-hangfire
description: >
  Como usar Hangfire en Marketing1on1: configuracion (queues, workers, SQL Server storage,
  dashboard con auth JWT), patrones de job existentes (job simple vs Planner->Chunk->Finalizer),
  como registrar y encolar un job nuevo, el antipatron de pasar payloads grandes (byte[]) en los
  argumentos del job (ISSUE-011), y los 3 mecanismos de progreso disponibles (Redis+SignalR,
  polling manual, y Hangfire.Console para la pestaña "Console" del dashboard).
  Usar SIEMPRE que se cree, modifique o depure un job Hangfire, se toque
  Extensions/HangfireExtensions.cs u Options/HangfireOptions.cs, o se investigue un job en
  /hangfire/jobs/details/{id}.
applyTo:
  - "src/MarketingOneOnOneApi/Jobs/**"
  - "src/MarketingOneOnOneApi/Extensions/HangfireExtensions.cs"
  - "src/MarketingOneOnOneApi/Options/HangfireOptions.cs"
---

# Hangfire en Marketing1on1

## 1. Donde vive la configuracion

| Archivo | Responsabilidad |
|---|---|
| `Extensions/HangfireExtensions.cs` | `AddHangfireConfiguration` (storage, queues, workers, DI de jobs, `.UseConsole()`), `ConfigureHangfireRecurringJobs`, `UseHangfireDashboardConfiguration` (auth JWT/cookie) |
| `Options/HangfireOptions.cs` | `DashboardPath` (`/hangfire`), `WorkerCount` (default 5), `Queues` (`critical`, `default`, `auth`, `integration`, `low`), `AutomaticRetryAttempts` (default 3) |

Storage: SQL Server, schema `Hangfire`, en la BD remota compartida (`172.191.128.24` en producción/dev). Todos los jobs de todos los tenants comparten el mismo worker pool y las mismas 5 queues — no hay aislamiento por cliente.

Dashboard: `http://localhost:8000/hangfire` en desarrollo (sin auth). En producción exige JWT o cookie de sesion (`HangfireAuthorizationFilter`), salvo que `HANGFIRE_DASHBOARD_OPEN_ACCESS=true`.

Consultar jobs/fallos directo desde SQL sin abrir el dashboard (MCP `dbQuery`):
- `db_hangfire_jobs` — resumen de estados + jobs mas recientes.
- `db_hangfire_job_console` — dado el id numerico de un job (el de `/hangfire/jobs/details/{id}`), devuelve el job, su historial de estados, el detalle de excepcion si fallo, y **todas las lineas de la pestaña Console** (offset en segundos desde el inicio). Resuelve internamente el mapeo `Hangfire.Hash` (`console:refs:{hexId}` → `jobId`) necesario para leer `Hangfire.[Set]` (`console:{hexId}`), que es donde Hangfire.Console persiste las lineas — no es obvio a simple vista, ver `.mcp/db-query/src/db.mjs` (`hangfireJobConsole`) si hay que tocarlo.

---

## 2. Patrones de job en el proyecto

### A. Job simple (una sola llamada)

Ejemplo: `VideoGenerationJob`, `SendLoginPinEmailJob`. Un endpoint hace `Enqueue<IMiJob>(job => job.HacerAlgoAsync(...))` y el job corre de punta a punta.

### B. Planner -> Chunk[] -> Finalizer (procesos largos/paralelizables)

Ejemplo: `CampaignMasterJob`/`CampaignChunkJob`/`CampaignFinalizerJob`, `IntegrationOnePlannerJob`/`IntegrationOneChunkJob`/`IntegrationOneFinalizerJob` (ver skill `m1on1-integration-1`/`m1on1-integration-2` para el detalle completo de ese flujo).

- **Planner**: calcula el total de trabajo, parte en N chunks, inicializa estado en Redis, encola los N `ChunkJob` (normalmente con `_backgroundJobClient.Create<IChunkJob>(..., new EnqueuedState("integration"))` para fijar la queue explicita).
- **ChunkJob**: procesa un lote, incrementa contadores atomicos en Redis, si es el ultimo chunk encola el Finalizer.
- **Finalizer**: calcula el status final (`completed` / `completed_with_errors` / `failed`).

Usar este patron cuando el trabajo es grande y se puede partir en unidades independientes que corren en paralelo. Para un solo llamado a una API externa lenta (como sora-2), el patron A alcanza.

---

## 3. Registrar un job nuevo

1. Definir la interfaz en `Jobs/{Area}/I{Nombre}Job.cs` (o junto a la clase si es simple, como `VideoGenerationJob.cs`). El metodo publico debe ser serializable en sus parametros — ver sección 5.
2. Implementar la clase con DI normal por constructor (`AppDbContext`, servicios, `ILogger<T>`, etc.).
3. Registrar en `HangfireExtensions.cs` → `AddHangfireConfiguration`:
   ```csharp
   services.AddScoped<IMiNuevoJob, MiNuevoJob>();
   ```
4. Encolar desde un endpoint o desde otro job:
   ```csharp
   backgroundJobClient.Enqueue<IMiNuevoJob>(job => job.HacerAlgoAsync(arg1, arg2, null));
   //                                                                          ^^^^ PerformContext, ver sección 6
   ```
   Para fijar una queue especifica (en vez de la que infiere el atributo por defecto), usar `Create` + `EnqueuedState("nombre-queue")` en vez de `Enqueue` — la queue debe estar en `HangfireOptions.Queues` o el job nunca se recoge.

---

## 4. Antipatron: no pasar payloads grandes en los argumentos del job

Hangfire.SqlServer serializa los argumentos de cada job a JSON y los persiste en la tabla `Hangfire.Job` dentro de una transaccion con `sp_getapplock`. Un payload de varios MB (por ejemplo `byte[]` de una imagen) puede hacer expirar ese lock → `Hangfire.BackgroundJobClientException` con `Win32Exception: Unknown error: 258` (WAIT_TIMEOUT). Esto paso en producción con `VideoGenerationJob` (ver `ia/07_issues/archive/2026-07.md`, ISSUE-011).

**Regla:** los argumentos de un job deben ser IDs/strings/metadatos pequeños, nunca blobs binarios.

Patron correcto cuando el job necesita un archivo (imagen, video, etc.):
1. El endpoint persiste el archivo **antes** de encolar (en este proyecto, subiéndolo a Art Library vía `IArtLibraryService.UploadArtworkAsync` si es una imagen nueva, o resolviendo un ID ya existente).
2. Solo el `idMultimedia` (o equivalente) viaja en los argumentos del job.
3. El job descarga el archivo por su cuenta al arrancar, con `IContaboBackgroundStorageService.DownloadBytesAsync(idClientCustomer, path)` — nunca con `IContaboStorageService` normal, porque ese resuelve credenciales por `HttpContext` del request actual, que no existe dentro de un job Hangfire (resolvería mal el bucket del tenant).

Ver `Jobs/VideoGeneration/VideoGenerationJob.cs` (`LoadImageBytesWithContentTypeAsync`) y `Features/Video/VideoGenerationEndpoints.cs` (`ResolveImageIdMultimediaAsync`) como ejemplo de referencia.

---

## 5. Progreso: 3 mecanismos disponibles, cuando usar cada uno

| Mecanismo | Cuando usarlo | Ejemplo |
|---|---|---|
| **Redis + SignalR** (push en vivo al frontend) | El usuario espera el resultado en pantalla mientras el job corre (generación de video/imagen, campañas) | `VideoGenerationRedisHelper` + `VideoProgressHub`, `CampaignMasterJob` + `CampaignProgressHub` |
| **Polling manual sin SignalR** | Procesos administrativos largos donde el usuario refresca un boton "Ver avance" en vez de quedarse mirando (integraciones SQL→ES) | `IntegrationOneRedisHelper` + `GET /status/{jobId}` — ver skill `m1on1-integration-1` |
| **Hangfire.Console** (pestaña "Console" del dashboard, `/hangfire/jobs/details/{id}`) | Depuración/soporte: ver qué está haciendo un job en tiempo real desde el dashboard, sin depender de Redis/SignalR ni de logs externos | `VideoGenerationJob` — ver [references/console-progress.md](./references/console-progress.md) |

Los tres no son excluyentes — `VideoGenerationJob` usa Redis+SignalR para el frontend **y** Hangfire.Console para poder ver el mismo avance desde el dashboard sin abrir el navegador de la app. Al agregar progreso a un job nuevo, evaluar si aplica alguno o varios de estos 3 patrones.

**Ver [references/console-progress.md](./references/console-progress.md) para el procedimiento completo de agregar Hangfire.Console a un job.**

---

## 6. Recurring jobs

Se registran en `ConfigureHangfireRecurringJobs` (llamado desde `Program.cs` al arrancar), no desde el dashboard ni desde codigo disperso:

```csharp
recurringJobManager.AddOrUpdate<IMiJob>(
    "nombre-unico-del-recurring-job",
    job => job.MetodoAsync(),
    Cron.Daily(3, 0), // o Cron.Hourly(), Cron.Monthly(1, 0, 0), etc.
    new RecurringJobOptions { TimeZone = timeZone }
);
```

`timeZone` se resuelve una vez al inicio de la funcion desde `IDomainSettingsService.GetCurrentTimeZone()` con fallback a `Central America Standard Time` y despues a UTC — reusar esa variable, no hardcodear otra zona horaria.

---

## 7. Checklist rapido al crear/tocar un job

- [ ] Interfaz + implementacion registradas en `AddHangfireConfiguration` (`services.AddScoped<IXxxJob, XxxJob>()`)
- [ ] Argumentos del job son IDs/strings pequeños, **no** `byte[]`/blobs (sección 4)
- [ ] Si el job necesita un archivo: se persiste antes de encolar, el job lo descarga con `IContaboBackgroundStorageService` (no `IContaboStorageService`, que depende de `HttpContext`)
- [ ] Si el job es de tipo Planner/Chunk/Finalizer: contadores en Redis, deduplicacion por chunkId, no volver a introducir SignalR si el flujo original no lo usaba (ver runbooks de `m1on1-integration-1`)
- [ ] Si aplica progreso visible: elegido el/los mecanismos correctos (sección 5) — considerar agregar `PerformContext? context = null` + `context?.WriteLine(...)` aunque sea solo para depuración
- [ ] Queue usada existe en `HangfireOptions.Queues` (`appsettings.json` sección `Hangfire`)
- [ ] `dotnet build` y `dotnet test` en verde
