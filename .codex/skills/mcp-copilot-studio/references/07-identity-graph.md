# MCP — Identidad del Usuario, Microsoft Graph y Diagnóstico

## 1. Qué Headers Envía Copilot Studio (Sin OAuth)

> ⚠️ **Datos verificados en producción (2026-05-22)** — La tabla a continuación refleja lo que
> Copilot Studio **realmente envía** en modo sin OAuth. Ver detalles completos en
> [08-no-auth-real-data.md](08-no-auth-real-data.md).

Cuando el conector se configura en modo **"Sin Security"** (sin OAuth), la plataforma inyecta headers de identidad del usuario — **pero no todos los que documenta Microsoft**:

| Header | Disponible en "Sin Security" | Notas |
|--------|-----|-------|
| `x-ms-client-object-id` | ✅ Siempre | OID del usuario **en el tenant de Power Platform** (puede diferir del tenant propio) |
| `x-ms-client-principal-id` | ✅ Siempre | Idéntico a `x-ms-client-object-id` |
| `x-ms-client-tenant-id` | ✅ Siempre | Tenant de Power Platform del usuario — ⚠️ **puede NO ser el mismo tenant de tu Azure AD** |
| `x-ms-client-session-id` | ✅ Solo en `initialize` | GUID de la sesión (útil para agrupar requests de una conversación) |
| `x-ms-client-app-id` | ✅ Siempre | App ID de Copilot Studio en Power Platform |
| `x-ms-entra-agent-name` | ✅ Siempre | Nombre del bot/agente configurado en CS |
| `x-ms-entra-agent-id` | ✅ Siempre | OID del agente en Power Platform |
| `x-ms-correlation-id` | ✅ Siempre | Mismo en todos los requests de un turno |
| `x-ms-client-principal-name` | ❌ **NO llega** | Documentado por MS como "siempre disponible" pero ausente en práctica |
| `Authorization: Bearer <jwt>` | ❌ **NO llega** | Solo con OAuth configurado en el conector |

> **Problema de tenant mismatch**: El OID que llega en `x-ms-client-object-id` pertenece al
> **tenant de Power Platform** (ej. `2f80d4e1-...`), que puede ser **distinto** al tenant donde
> vive tu Azure AD (ej. `a778774c-...`). Intentar resolver ese OID en tu propio Graph dará **404**.
> Ver sección 3.2 de [08-no-auth-real-data.md](08-no-auth-real-data.md) para el diagnóstico completo.

---

## 2. Patrón CallerInfo — Extraer Identidad en C#

El patrón recomendado es un `record` inmutable que acumula la identidad del caller desde múltiples fuentes:

```csharp
// McpModels.cs
public sealed record CallerInfo(
    string? Oid,
    string? Upn,
    string? DisplayName = null,
    string? Email       = null);
```

```csharp
// McpHandler.cs — en el handler principal
private static CallerInfo ExtractCaller(HttpRequest req, ClaimsPrincipal user)
{
    string? oid = null;
    string? upn = null;

    // 1. JWT Bearer (si el conector usa OAuth) — más confiable
    if (user.Identity?.IsAuthenticated == true)
    {
        oid = user.FindFirst("oid")?.Value;
        upn = user.FindFirst("preferred_username")?.Value
           ?? user.FindFirst("upn")?.Value;
    }

    // 2. Headers de CS (Copilot Studio — "Sin Security" o como complemento)
    if (oid is null && req.Headers.TryGetValue("x-ms-client-object-id", out var h_oid))
        oid = h_oid.ToString();

    if (upn is null && req.Headers.TryGetValue("x-ms-client-principal-name", out var h_upn))
        upn = h_upn.ToString();

    return new CallerInfo(Oid: oid, Upn: upn);
}
```

---

## 3. Enriquecimiento con Microsoft Graph

### 3.1 Cuándo llamar Graph

Llamar Graph **únicamente en `tools/call`**, nunca en `tools/list`:
- Reduce latencia en el descubrimiento de herramientas
- Solo paga el coste de la llamada HTTP cuando realmente se ejecuta una herramienta

```csharp
// HandleToolsCallAsync — solo aquí se llama Graph
if (caller.Oid is not null)
{
    logger.LogInformation("[MCP:tools/call] Enriqueciendo con Graph para oid={Oid}", caller.Oid);
    var (displayName, email) = await graphSvc.GetUserInfoAsync(caller.Oid, ct);
    caller = caller with { DisplayName = displayName, Email = email };

    logger.LogInformation("[MCP:tools/call] Graph → email={Email} name={Name}",
        caller.Email ?? "(null — Graph no devolvió email)",
        caller.DisplayName ?? "(null)");
}
else
{
    logger.LogWarning("[MCP:tools/call] Sin OID — Graph no se llama");
}

// Email efectivo: Graph primero, UPN como fallback
var effectiveEmail = caller.Email ?? caller.Upn;
```

### 3.2 Por qué el fallback `Email ?? Upn` es esencial

Graph puede devolver `(null, null)` en escenarios reales:
- El OID del usuario pertenece a un tenant **diferente** al configurado en el App Registration
- El App Registration no tiene los permisos necesarios o les falta el admin consent
- Timeout de red o error temporal de Graph

El UPN que envía Copilot Studio en `x-ms-client-principal-name` **siempre es confiable**: es el login del usuario en M365 y típicamente coincide con su email de trabajo. Usarlo como fallback garantiza que el flujo funciona aunque Graph falle.

```csharp
// Auth check — requiere OID Y email efectivo (no solo el de Graph)
var effectiveEmail = caller.Email ?? caller.Upn;

if (tool.IsPrivate && (caller.Oid is null || effectiveEmail is null))
{
    var reason = caller.Oid is null
        ? "Sin OID — CS no envió x-ms-client-object-id"
        : $"Sin email — Graph falló y CS no envió x-ms-client-principal-name (oid={caller.Oid})";
    return JsonRpcError(id, -32603,
        "No se pudo identificar tu correo. " +
        "Asegúrate de que Copilot Studio envíe los headers de identidad " +
        "(x-ms-client-object-id, x-ms-client-principal-name).");
}
```

---

## 4. Implementación GraphUserService

```csharp
// IGraphUserService.cs
public interface IGraphUserService
{
    Task<(string? DisplayName, string? Email)> GetUserInfoAsync(string oid, CancellationToken ct = default);
}
```

```csharp
// GraphUserService.cs
using System.Net.Http.Headers;

public sealed class GraphUserService : IGraphUserService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration     _cfg;
    private readonly ILogger            _logger;

    // Cache de token (válido ~1h, se renueva cuando expira)
    private static string? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GraphUserService(IHttpClientFactory http, IConfiguration cfg,
        ILogger<GraphUserService> logger)
    {
        _http   = http;
        _cfg    = cfg;
        _logger = logger;
    }

    public async Task<(string? DisplayName, string? Email)> GetUserInfoAsync(
        string oid, CancellationToken ct = default)
    {
        try
        {
            var token  = await GetAppTokenAsync(ct);
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url      = $"https://graph.microsoft.com/v1.0/users/{oid}" +
                           "?$select=mail,userPrincipalName,displayName";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Graph] GET users/{Oid} → {Status}", oid, response.StatusCode);
                return (null, null);
            }

            using var doc  = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root        = doc.RootElement;
            var displayName = root.TryGetProperty("displayName",       out var dn)  ? dn.GetString()  : null;
            var mail        = root.TryGetProperty("mail",              out var m)   ? m.GetString()   : null;
            var upn         = root.TryGetProperty("userPrincipalName", out var upn_) ? upn_.GetString() : null;

            // mail puede ser null si el user no tiene buzón Exchange — usar UPN como fallback
            var email = mail ?? upn;

            _logger.LogInformation("[Graph] Usuario={Name} Email={Email}", displayName, email);
            return (displayName, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Graph] Error al buscar usuario oid={Oid}", oid);
            return (null, null);  // Nunca propagar — el caller usará UPN como fallback
        }
    }

    private async Task<string> GetAppTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            // Double-check dentro del lock
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            var tenantId     = _cfg["Graph:TenantId"]     ?? throw new InvalidOperationException("Graph:TenantId no configurado");
            var clientId     = _cfg["Graph:ClientId"]     ?? throw new InvalidOperationException("Graph:ClientId no configurado");
            var clientSecret = _cfg["Graph:ClientSecret"] ?? throw new InvalidOperationException("Graph:ClientSecret no configurado");

            var client = _http.CreateClient();
            var body   = new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["scope"]         = "https://graph.microsoft.com/.default"
            };

            var tokenResp = await client.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(body), ct);

            tokenResp.EnsureSuccessStatusCode();

            using var doc   = await JsonDocument.ParseAsync(
                await tokenResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn   = doc.RootElement.GetProperty("expires_in").GetInt32();

            _cachedToken = accessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 1 min de margen

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
```

### Registro DI

```csharp
// PowerBiModule.cs / startup
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGraphUserService, GraphUserService>();
```

### Configuración (appsettings.json / App Settings de Azure)

```json
{
  "Graph": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId":  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "tu_client_secret"
  }
}
```

> En Azure App Service usar la notación doble guión bajo: `Graph__TenantId`, `Graph__ClientId`, `Graph__ClientSecret`.

---

## 5. Permisos de Microsoft Graph Necesarios

El App Registration que usa el servidor MCP necesita permisos **de aplicación** (no delegados), porque actúa como daemon (client credentials flow):

| Permiso | Tipo | Para qué |
|---------|------|----------|
| `User.Read.All` | Application | Leer perfil de cualquier usuario por OID |
| `User.ReadWrite.All` | Application | Leer y modificar usuarios (si lo requieres) |
| `Directory.Read.All` | Application | Leer directorio completo (grupos, roles) |

> `User.Read.All` es suficiente para `GET /v1.0/users/{oid}?$select=mail,displayName,userPrincipalName`.

**Todos los permisos de aplicación requieren Admin Consent** del tenant.

---

## 6. Validar Permisos Graph con `az` CLI

### 6.1 Ver permisos registrados en el App Registration

```powershell
# Reemplaza CLIENT_ID con el appId del App Registration
az ad app permission list --id "CLIENT_ID" --output table
```

### 6.2 Ver grants de admin consent (service principal)

```powershell
# Obtener el objectId del service principal del app
$sp = az ad sp show --id "CLIENT_ID" | ConvertFrom-Json
$spId = $sp.id

# Ver los app role assignments (admin consent aplicado)
az ad app permission list-grants --all --filter "clientId eq '$spId'" --output table
```

### 6.3 Verificar directamente con Graph REST API

```powershell
# Token de acceso para Graph (usando az CLI)
$token = az account get-access-token --resource "https://graph.microsoft.com" --query accessToken -o tsv

# Ver permisos del service principal
Invoke-RestMethod `
  -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/$spId/appRoleAssignments" `
  -Headers @{ Authorization = "Bearer $token" } |
  ConvertTo-Json -Depth 3
```

### 6.4 Probar una llamada Graph directamente

```powershell
# Token con client credentials (como lo hace el servidor)
$tenantId     = "TU_TENANT_ID"
$clientId     = "TU_CLIENT_ID"
$clientSecret = "TU_CLIENT_SECRET"

$body = @{
    grant_type    = "client_credentials"
    client_id     = $clientId
    client_secret = $clientSecret
    scope         = "https://graph.microsoft.com/.default"
}

$tokenResp = Invoke-RestMethod `
    -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
    -Method POST -Body $body
$token = $tokenResp.access_token

# Buscar usuario por OID
$oid = "OID_DEL_USUARIO_A_BUSCAR"
Invoke-RestMethod `
    -Uri "https://graph.microsoft.com/v1.0/users/$oid?`$select=mail,userPrincipalName,displayName" `
    -Headers @{ Authorization = "Bearer $token" }
```

**Errores comunes en este paso:**

| Error | Causa | Solución |
|-------|-------|----------|
| `403 Forbidden` | Falta admin consent o permiso incorrecto | Ir a Azure Portal → App Registration → API Permissions → Grant admin consent |
| `404 Not Found` | El OID no existe en ese tenant | El OID viene de un tenant diferente; verificar `x-ms-client-tenant-id` |
| `401 Unauthorized` | Client secret inválido o vencido | Regenerar el secreto en Azure Portal |
| `AADSTS700016` | ClientId no existe en el tenant | El App Registration no está en el tenant correcto |

---

## 7. Endpoint `/diag/whoami` para Diagnóstico

Agrega un endpoint `GET /diag/whoami` para ver exactamente qué headers envía Copilot Studio.
Útil para depurar el OID, UPN y verificar que el conector está enviando los headers esperados.

```csharp
// PowerBiModule.cs — dentro de MapMcp()
app.MapGet("/diag/whoami", (HttpRequest req, HttpContext ctx) =>
{
    var msHeaders = req.Headers
        .Where(h => h.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase))
        .ToDictionary(h => h.Key, h => h.Value.ToString());

    return Results.Ok(new
    {
        oid_header = req.Headers.TryGetValue("x-ms-client-object-id", out var o)
            ? o.ToString() : "(ausente)",
        upn_header = req.Headers.TryGetValue("x-ms-client-principal-name", out var u)
            ? u.ToString() : "(ausente)",
        all_x_ms_headers  = msHeaders,
        jwt_authenticated = ctx.User.Identity?.IsAuthenticated,
        jwt_oid           = ctx.User.FindFirst("oid")?.Value ?? "(sin JWT)",
        jwt_upn           = ctx.User.FindFirst("preferred_username")?.Value ?? "(sin JWT)"
    });
}).AllowAnonymous();
```

### Cómo usarlo

```powershell
# Desde tu máquina — verás los headers que TÚ envías (útil para pruebas locales)
Invoke-RestMethod "https://mi-servidor.azurewebsites.net/diag/whoami"

# Para ver lo que Copilot Studio envía:
# 1. Agregar la URL /diag/whoami como acción en el agente de CS con el método GET
# 2. O revisar los logs del servidor al momento que CS llama /mcp/...
```

### Salida esperada cuando llama Copilot Studio

```json
{
  "oid_header": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "upn_header": "usuario@empresa.com",
  "all_x_ms_headers": {
    "x-ms-client-object-id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "x-ms-client-principal-name": "usuario@empresa.com",
    "x-ms-client-tenant-id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  },
  "jwt_authenticated": false,
  "jwt_oid": "(sin JWT)",
  "jwt_upn": "(sin JWT)"
}
```

> `jwt_authenticated = false` es normal con el conector "Sin Security" — no hay OAuth Bearer token. Los campos `oid_header` y `upn_header` son los que importan.

---

## 8. Flujo Completo: Tools/Call con Identidad

```
Copilot Studio                      Servidor MCP                    Microsoft Graph
     │                                   │                                │
     │  POST /mcp                        │                                │
     │  x-ms-client-object-id: <oid>    │                                │
     │  x-ms-client-principal-name: upn  │                                │
     │  body: tools/call obtener_X       │                                │
     │──────────────────────────────────>│                                │
     │                                   │ ExtractCaller()                │
     │                                   │ caller.Oid = <oid>             │
     │                                   │ caller.Upn = upn               │
     │                                   │                                │
     │                                   │  GET /v1.0/users/<oid>        │
     │                                   │──────────────────────────────>│
     │                                   │  { mail, displayName }         │
     │                                   │<──────────────────────────────│
     │                                   │                                │
     │                                   │ caller.Email = mail ?? upn     │
     │                                   │ (UPN como fallback si Graph ❌) │
     │                                   │                                │
     │                                   │ Auth check: Oid ✅ Email ✅    │
     │                                   │ Ejecutar tool                  │
     │  result: { content: [...] }       │                                │
     │<──────────────────────────────────│                                │
```

---

## 9. Tenant Mismatch — Caso Frecuente

**Síntoma**: Graph devuelve 404 para el OID del usuario aunque el App Registration y la configuración estén correctos.

**Causa**: El OID del usuario viene del tenant de Copilot Studio (donde está el bot), pero el App Registration en el servidor MCP está en un tenant diferente.

**Diagnóstico**:
```powershell
# Ver el tenant del OID que envía CS usando /diag/whoami
# Luego verificar si ese OID existe en tu tenant:
$token = <token obtenido con client credentials>
Invoke-RestMethod "https://graph.microsoft.com/v1.0/users/<OID_DE_CS>" `
    -Headers @{ Authorization = "Bearer $token" }
# Si devuelve 404 → el usuario no está en tu tenant
```

**Soluciones**:
1. **Registrar la aplicación en el tenant correcto** (donde están los usuarios de CS)
2. **Confiar en el UPN** del header `x-ms-client-principal-name` directamente sin llamar Graph
3. **Guest users**: si los usuarios son invitados en tu tenant, pueden aparecer con un OID diferente; buscarlos por UPN: `GET /v1.0/users/{upn}` o `GET /v1.0/users?$filter=userPrincipalName eq '{upn}'`
