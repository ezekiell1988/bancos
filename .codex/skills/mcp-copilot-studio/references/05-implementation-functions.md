# MCP — Implementación Azure Functions .NET 10 Isolated

## Estructura de Archivos

```
mcp-functions/
├── mcp-functions.csproj
├── Program.cs               ← Bootstrap (reutiliza AddMcpServer del módulo compartido)
├── host.json                ← routePrefix: "" (OBLIGATORIO)
├── local.settings.json
└── Functions/
    └── McpFunction.cs       ← Trigger HTTP
```

---

## `mcp-functions.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker"                    Version="2.*" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http"   Version="3.*" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk"               Version="2.*" />
  </ItemGroup>
</Project>
```

---

## `Program.cs`

```csharp
using McpServer;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddProblemDetails();
builder.Services.AddMcpServer();      // mismo módulo que en Minimal APIs

builder.Build().Run();
```

---

## `Functions/McpFunction.cs`

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using McpServer;
using McpServer.Options;
using McpServer.Tools;
using Microsoft.Extensions.Options;

namespace McpServer.Functions;

public sealed class McpFunction(ToolRegistry registry, IOptions<McpOptions> opts)
{
    [Function("McpEndpoint")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{*route}")]
        HttpRequestData req,
        CancellationToken ct)
    {
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await ok.WriteStringAsync("MCP ready", ct);
            return ok;
        }

        // Leer body y delegar al núcleo JSON-RPC compartido
        var bodyStr = await new StreamReader(req.Body).ReadToEndAsync(ct);
        using var doc = JsonDocument.Parse(bodyStr);
        var body      = McpHandler.NormalizeBatch(doc.RootElement).Clone();

        var id     = body.TryGetProperty("id",     out var idEl)  ? (JsonElement?)idEl  : null;
        var method = body.TryGetProperty("method", out var methEl) ? methEl.GetString() : null;
        body.TryGetProperty("params", out var paramsEl);

        if (method is null && id.HasValue)
            return req.CreateResponse(System.Net.HttpStatusCode.Accepted);

        var userEmail = ExtractEmail(req);
        var (statusCode, jsonBody) = await McpInternals.RouteAsync(
            method, id, paramsEl, registry, opts.Value, userEmail, ct);

        var response = req.CreateResponse((System.Net.HttpStatusCode)statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        if (jsonBody is not null)
            await response.WriteStringAsync(jsonBody, ct);
        return response;
    }

    private static string? ExtractEmail(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-NAME", out var vals))
            return vals.FirstOrDefault();
        return null;
    }
}
```

---

## JWT en Azure Functions Isolated — Patrón Correcto

> ⚠️ **Trampa crítica**: En Azure Functions isolated worker, `AddAuthentication/AddJwtBearer` registra los servicios en DI pero **el middleware `UseAuthentication()` no corre automáticamente** al igual que en ASP.NET Core clásico. El overload `ConfigureFunctionsWebApplication(Action<>)` con pipeline de middleware no está disponible en todas las versiones del SDK.
>
> Consecuencia: aunque el JWT sea perfectamente válido, `httpContext.User` queda anónimo y todos los checks de acceso fallan.

### Solución: validar y asignar manualmente dentro del handler

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// Al inicio de HandleAsync / del método que procesa el request:
var authResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

// ⚠️ PASO OBLIGATORIO: AuthenticateAsync NO asigna httpContext.User.
// Solo el middleware UseAuthentication() lo hace automáticamente.
// Sin esta asignación, httpContext.User.Claims está vacío aunque authResult.Succeeded == true.
if (authResult.Succeeded && authResult.Principal is not null)
    httpContext.User = authResult.Principal;

// Ahora sí se pueden leer claims correctamente:
var email = httpContext.User.FindFirst("upn")
         ?? httpContext.User.FindFirst("preferred_username")
         ?? httpContext.User.FindFirst("email");
```

### Por qué ocurre

```
ASP.NET Core clásico:
  UseAuthentication() middleware → corre por cada request → AuthenticateAsync + asigna User

Azure Functions isolated:
  ConfigureFunctionsWebApplication() → configura el pipeline pero UseAuthentication()
  no se invoca de forma garantizada en el path real del request HTTP
  → httpContext.User nunca se asigna → todos los claims están vacíos
```

### Cómo detectar el problema

Agregar puntos de debug después de `AuthenticateAsync`:

```csharp
audit.LogEvent("MCP:auth", "AuthenticateAsync result",
    $"Succeeded={authResult.Succeeded} | Failure={authResult.Failure?.Message}");

audit.LogEvent("MCP:claims", "Claims tras asignación",
    string.Join(", ", httpContext.User.Claims.Select(c => $"{c.Type}={c.Value}")));
```

Si `Succeeded=true` pero `Claims` está vacío → falta el `httpContext.User = authResult.Principal`.

---

## `host.json` (OBLIGATORIO)

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[4.*, 5.0.0)"
  },
  "extensions": {
    "http": { "routePrefix": "" }
  }
}
```

> ⚠️ Sin `"routePrefix": ""`, Azure Functions agrega `/api/` y las URLs no coinciden con lo configurado en Copilot Studio.

---

## Otros Lenguajes (referencia breve)

Si el proyecto no es .NET, el protocolo JSON-RPC de las Partes 1-5 es idéntico; solo cambia el código de infraestructura HTTP:

| Lenguaje | Approach recomendado |
|----------|----------------------|
| Python   | Azure Functions v2 + `azure-functions` 1.24+ |
| Node.js  | Express con `app.all("*", handler)` |
| Next.js  | App Router `app/api/mcp/[...route]/route.ts` |
