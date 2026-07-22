# Progreso en vivo en el dashboard con Hangfire.Console

Agregado el 2026-07-08 junto al fix de ISSUE-011, sobre `VideoGenerationJob`. Antes de esto,
ningun job del proyecto escribia nada visible en la pestaña **Console** de
`/hangfire/jobs/details/{id}` — el progreso solo existia en `ILogger` (va a stdout/Serilog, no al
dashboard), Redis (requiere consultar el estado por otro medio) o SignalR (requiere el frontend
conectado). `Hangfire.Console` cierra ese hueco: cualquiera con acceso al dashboard ve el avance
del job en tiempo real, útil para depurar sin acceso a logs del servidor ni al frontend de la app.

## 1. Paquete y configuracion global

Paquete NuGet `Hangfire.Console` (agregado con `dotnet add package Hangfire.Console`, version
1.4.3 al momento de escribir esto — compatible con `Hangfire.Core 1.8.18`).

Habilitado una sola vez en `Extensions/HangfireExtensions.cs`, dentro de la cadena de
`AddHangfire(config => config...)`:

```csharp
using Hangfire.Console;

services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions { /* ... */ })
    .UseFilter(new AutomaticRetryAttribute { Attempts = retryAttempts })
    // Habilita la pestaña "Console" en /hangfire/jobs/details/{id} — los jobs que
    // reciben PerformContext pueden escribir progreso en vivo, visible en el dashboard.
    .UseConsole()
);
```

Con esto habilitado, **cualquier job** del proyecto puede empezar a escribir en la consola del
dashboard siguiendo los 3 pasos de abajo — no hace falta tocar la configuracion de nuevo.

## 2. Como instrumentar un job especifico

### 2.1 Agregar `PerformContext?` a la interfaz y a la implementacion

`PerformContext` lo inyecta Hangfire en runtime — **no se serializa ni se pasa manualmente** al
encolar. Es el mismo patron que ya usaban `IIntegrationOneChunkJob`/`IIntegrationTwoChunkJob`
para leer `RetryCount` (`Jobs/IntegrationOne/IntegrationOneChunkJob.cs`,
`Jobs/IntegrationTwo/IntegrationTwoChunkJob.cs`), solo que ahora tambien se usa para escribir.

```csharp
using Hangfire.Server; // PerformContext vive aca

public interface IMiJob
{
    Task HacerAlgoAsync(string jobId, MiJobArgs args, PerformContext? context = null);
}

public class MiJob : IMiJob
{
    public async Task HacerAlgoAsync(string jobId, MiJobArgs args, PerformContext? context = null)
    {
        context?.WriteLine($"▶ Iniciado — jobId={jobId}");
        // ... trabajo real ...
        context?.WriteLine("✅ Completado");
    }
}
```

**Siempre usar `context?.WriteLine(...)` con `?.`** — fuera del runtime de Hangfire (tests
unitarios, llamadas directas) `context` es `null` y no debe explotar.

### 2.2 Pasar `null` explicito al encolar

Al encolar con `Enqueue`/`Create` usando una expression tree, pasar `null` explicito como ultimo
argumento (Hangfire lo reemplaza por el `PerformContext` real al ejecutar):

```csharp
backgroundJobClient.Enqueue<IMiJob>(job => job.HacerAlgoAsync(jobId, args, null));
```

Mismo patron que `IntegrationOnePlannerJob.cs`/`IntegrationTwoPlannerJob.cs` ya usaban al encolar
sus `ChunkJob` (`job.ProcessChunkAsync(..., null)`).

### 2.3 Que loguear

Regla practica: cada `context?.WriteLine(...)` deberia corresponder a un cambio de estado que ya
le importa al usuario/soporte — no hace falta loguear cada iteracion de un loop interno.
Ejemplo real de `VideoGenerationJob.GenerateAsync` (`Jobs/VideoGeneration/VideoGenerationJob.cs`):

```csharp
context?.WriteLine($"▶ VideoGeneration iniciado — videoJobId={videoJobId} idClientCustomer={args.IdClientCustomer} mode={args.Mode} durationSeconds={args.DurationSeconds} size={args.Size}");
// ...
context?.WriteLine($"Plan de clips: {clipDurations.Count} clip(s) de {string.Join("/", clipDurations)}s cada uno.");
// ...
context?.WriteLine($"Generando clip {i + 1}/{clipDurations.Count} ({clipDurations[i]}s) vía sora-2...");
// dentro del callback onProgress del servicio externo:
context?.WriteLine($"  clip {i + 1}/{clipDurations.Count}: {status}");
// ...
context?.WriteLine($"Subiendo video final a Contabo S3 ({finalBytes.Length} bytes)...");
// ...
context?.WriteLine($"✅ Completado — resultCode={multimedia.CodeMultimedia}");
// en el catch:
context?.WriteLine($"❌ Falló: {ex.Message}");
```

Patron: un `WriteLine` al entrar a cada etapa (inicio, plan/calculo, cada paso de un loop
significativo, subida/persistencia, resultado final), mas uno en el `catch` con el mensaje de
error. No duplica `_audit.LogEvent`/`ILogger` — son complementarios: `_audit`/Redis/SignalR
alimentan la auditoria y el frontend, `Hangfire.Console` es solo para mirar el job desde el
dashboard mientras corre o despues de que termino.

## 3. Como se ve

En `http://localhost:8000/hangfire/jobs/details/{id}` aparece una pestaña **Console** adicional
a **Job details**/**State history**, con las lineas escritas por `WriteLine` en orden, cada una
con timestamp relativo al inicio del job. Si el job ya no existe en el server local (reinicios,
retencion vencida) tambien desaparece el console log — no es persistencia a largo plazo, es para
observar el job mientras esta activo o poco despues de terminar.

## 4. Extender esto a otros jobs

Al momento de escribir esto, solo `VideoGenerationJob` esta instrumentado (fue el caso reportado
en ISSUE-011). Los jobs de Campaign*/Integration* (que ya tienen Redis+SignalR o polling manual)
pueden ganar tambien Hangfire.Console con los mismos 3 pasos de la sección 2 sin conflicto — es
un mecanismo adicional, no un reemplazo. Priorizar instrumentar jobs largos/con fallas dificiles
de reproducir, donde poder ver el avance en vivo desde el dashboard ahorra tiempo de soporte.
