---
name: llm-audit-dotnet
description: "Auditoría de código .NET en tiempo de ejecución para análisis LLM. Usar cuando se quiera revisar lo que ocurrió en el servidor o en el frontend: errores, decisiones, flujo de llamadas a Azure AI. Cubre: ILlmAuditService (escribe logs/llm-audit.md y opcionalmente BD), POST /diag/audit/client para logs de frontend, configuración LlmAudit en appsettings. Triggers: 'quiero ver los logs', 'analiza lo que pasó', 'revisar comportamiento del servidor', 'debug con IA', 'leer audit log', 'error al generar imagen'."
---

# dotnet-llm-audit — Auditoría de .NET para LLMs

Sistema liviano para que un LLM analice el comportamiento de una app .NET 10 en tiempo de ejecución, sin ruido de logs tradicionales.

## Arquitectura

```
Browser (frontend)
  │  POST /diag/audit/client ──────────────────────────────────────────────►┐
  │                                                                          │
MarketingOneOnOne.Api (runtime)                                              │
  └── ILlmAuditService  ──escribe──► logs/llm-audit.md  ◄──────────────────┘
                          └─► tbLlmAuditLog (BD, solo si PersistToDb=true)
```

> **Endpoint activo:** `POST /diag/audit/client` — solo para que el frontend envíe eventos.
> Solo existe en Development (`IsDevelopment()`).
> `LlmAudit.Enabled` es `false` en producción por defecto.

---

## Archivos del sistema (Marketing1on1)

| Archivo | Propósito |
|---------|-----------|
| `Options/LlmAuditOptions.cs` | `Enabled`, `LogPath`, `MaxFileSizeKb`, `PersistToDb` |
| `Services/LlmAuditService.cs` | Interface `ILlmAuditService` + implementación Singleton |
| `DTOs/LlmAuditDtos.cs` | `AuditClientDto` para el POST del frontend |
| `Extensions/ApplicationServicesExtensions.cs` | Registro DI — `AddSingleton<ILlmAuditService, LlmAuditService>()` |
| `Program.cs` | `app.MapPost("/diag/audit/client", ...)` — solo en Development |

---

## Configuración

**appsettings.json** (producción — desactivado):
```json
"LlmAudit": {
  "Enabled": false,
  "PersistToDb": false,
  "LogPath": "logs/llm-audit.md",
  "MaxFileSizeKb": 2048
}
```

**appsettings.Development.json** (dev — activado):
```json
"LlmAudit": {
  "Enabled": true,
  "LogPath": "logs/llm-audit.md"
}
```

---

## ILlmAuditService — API de escritura

```csharp
public interface ILlmAuditService
{
    void LogStartup(string component, IEnumerable<string> facts);
    void LogEvent(string category, string intent, string result, object? context = null);
    void LogDecision(string area, string decision, string rationale);
    void LogError(string category, string message, Exception? ex = null);
    void LogClientEvent(string category, string intent, string result, object? context = null);
    void Clear();
}
```

El servicio es **Singleton**. Inyectar via primary constructor o campo:

```csharp
public class MiServicio(ILlmAuditService audit)
{
    public void DoWork()
    {
        audit.LogEvent("MiServicio", "Iniciando proceso", "✅ comenzado", new { id = 42 });
    }
}
```

---

## Instrumentación en ArtLibraryService

Ya instrumentado en los puntos críticos:

| Método | Qué se loguea |
|--------|---------------|
| `GenerateArtworkAsync` | Inicio (size, apiSize, hasRef, model), error, completado |
| `AddVersionAsync` | Inicio (size, model), error |
| `CallAzureImageGenerateAsync` | status HTTP + preview del body (400 chars) |
| `CallAzureImageEditAsync` (con imagen de referencia) | status HTTP + preview del body + error |
| `CallAzureImageEditFromBytesAsync` (AddVersion) | status HTTP + preview del body + error |

---

## Logs del frontend — POST /diag/audit/client

### Contrato

```
POST /diag/audit/client
Content-Type: application/json

{
  "category":  "ArtLibrary",
  "intent":    "generateArtwork request",
  "result":    "size=1920x1080 hasRef=false",
  "context":   { "name": "Banner julio", "promptLength": 120 }
}
```

### Helper en art-library.service.ts

```typescript
private audit(category: string, intent: string, result: string, context?: Record<string, unknown>): void {
  fetch('/diag/audit/client', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ category, intent, result, context: context ?? null }),
  }).catch(() => {});
}
```

Llamadas activas:
- `generateArtwork` → request, response, error
- `addVersion` → request, response, error

---

## Protocolo de lectura de logs

### Ruta del archivo

El runtime escribe en `logs/llm-audit.md` **relativo a `AppContext.BaseDirectory`**.
En desarrollo con VS Code Launch, eso resuelve a:

```
src/MarketingOneOnOneApi/bin/Debug/net10.0/logs/llm-audit.md   ← RUTA REAL EN DEV
```

> Si se ejecuta `dotnet run` desde `src/MarketingOneOnOneApi/`, la ruta cambia a:
> `src/MarketingOneOnOneApi/logs/llm-audit.md`

Buscar el archivo cuando no se sabe dónde está:

```bash
find src/MarketingOneOnOneApi -name "llm-audit.md" 2>/dev/null
```

### Leer el archivo

```bash
cat src/MarketingOneOnOneApi/bin/Debug/net10.0/logs/llm-audit.md
```

### Filtrar solo errores

```bash
grep -E "^\#\#|\[ERROR\]|❌|Result:" src/MarketingOneOnOneApi/bin/Debug/net10.0/logs/llm-audit.md | head -60
```

### Filtrar solo Art Library

```bash
grep -E "\[ArtLibrary\]|GenerateArtwork|AddVersion|Azure" src/MarketingOneOnOneApi/bin/Debug/net10.0/logs/llm-audit.md
```

---

## Persistencia en BD (TASK-EBC-BE-04)

Ver skill `llm-audit-db` para la estructura de `tbLlmAuditLog` y el flag `PersistToDb`.

---

## Convenciones de categorías

| Categoría | Cuándo usarla |
|-----------|---------------|
| `STARTUP` | Inicialización de servicios |
| `ArtLibrary` | Generación/edición de imágenes |
| `DECISION` | Branching logic, feature flags |
| `ERROR` | Excepciones capturadas |
| `INTEGRATION` | Llamadas a APIs externas |
| `BROWSER·ArtLibrary` | Eventos del frontend |

---

## Seguridad

- `POST /diag/audit/client` solo se mapea en Development
- `LlmAudit.Enabled` está en `false` por defecto en producción
- El POST trunca category (50), intent (200) y result (500)
- El archivo `logs/llm-audit.md` está en `.gitignore`
- No loguear prompts completos, tokens ni PII
