# MCP + Copilot Studio Con OAuth 2.0 — Datos Reales Capturados

> **Fuente:** Captura viva del slot `hangfire-dev` el 2026-05-22 via `diag.llmAuditEntry`.
> **Conector CS:** `crcbb_AgenteSynapseSQL` — con OAuth configurado (Authorization Code, Azure AD).
> **App Registration:** `5d46184e-d0d8-4add-96ec-b329e7fb57a4`, tenant ITQS `a778774c-ccbf-4b7d-a778-2c879d68ad71`.
> **Complemento de:** [08-no-auth-real-data.md](08-no-auth-real-data.md) (comparación sin OAuth).

---

## 1. Cómo Configurar OAuth en Copilot Studio

En el conector MCP dentro de Copilot Studio:

| Campo | Valor |
|-------|-------|
| **Authentication mode** | Manual |
| **Auth type** | OAuth 2.0 — Authorization Code |
| **Client ID** | `5d46184e-d0d8-4add-96ec-b329e7fb57a4` |
| **Client secret** | (ver Key Vault / App Registration ITQS) |
| **Authorization URL** | `https://login.microsoftonline.com/a778774c-ccbf-4b7d-a778-2c879d68ad71/oauth2/v2.0/authorize` |
| **Token URL** | `https://login.microsoftonline.com/a778774c-ccbf-4b7d-a778-2c879d68ad71/oauth2/v2.0/token` |
| **Refresh URL** | `https://login.microsoftonline.com/a778774c-ccbf-4b7d-a778-2c879d68ad71/oauth2/v2.0/token` ← igual que Token URL |
| **Scope** | `api://5d46184e-d0d8-4add-96ec-b329e7fb57a4/user_impersonation openid profile email offline_access` |
| **Grant type** | Authorization Code |
| **Redirect URL** | (la URL de consentimiento de APIM que provee CS) |

> ⚠️ **Scope crítico — `offline_access`**: Sin este scope Microsoft NO emite refresh token.
> El access token dura ~1h. Sin refresh token Copilot Studio no puede renovarlo silenciosamente
> → la sesión expira y todas las tools privadas fallan con "sesión expirada".
>
> **`openid profile email`**: Hacen que el JWT incluya los claims `name`, `preferred_username` y `email`
> → necesarios para que `obtener_usuario_oid` muestre nombre y correo (sin ellos devuelve "no disponible").
>
> **Refresh URL** = mismo endpoint que Token URL en Microsoft Identity Platform v2.
> La diferencia está en el `grant_type` del body: `authorization_code` (inicial) vs `refresh_token` (renovación).

---

## 2. Arquitectura de Red Con OAuth

```
Usuario en Copilot Studio
       │
       ▼  Authorization Code flow → AAD ITQS emite token v1
Microsoft Power Platform (tenant 2f80d4e1-...)
       │  Authorization: Bearer <jwt_v1>
       ▼  HTTPS POST /mcp/power-bi
Microsoft APIM Managed (msmanaged-na.azure-apim.net)
  └── /apim/crcbb-5Fmcp-20con-20oauth.../mcp/power-bi
       │  ← APIM SÍ reenvía el Bearer JWT al backend
       ▼
Tu servidor MCP (eVista.Hangfire hangfire-dev)
  └── JwtBearer middleware valida contra JWKS de AAD ITQS
```

> **Punto crítico**: APIM Managed de Microsoft **SÍ reenvía** el `Authorization: Bearer` al backend.
> Esto se confirmó viendo el header en el log raw del servidor. Ver sección 4.

---

## 3. Secuencia Completa de Requests por Turno

Igual que sin OAuth, CS re-inicializa el protocolo **en cada turno**. No hay conexión persistente.
Con OAuth, **todos los 4 requests llevan el Bearer JWT**.

```
[T+0ms]  POST /mcp/power-bi  initialize           → 200 + capabilities
[T+10ms] POST /mcp/power-bi  notifications/initialized → 202 (vacío)
[T+20ms] POST /mcp/power-bi  tools/list           → 200 + array de tools
[T+30ms] POST /mcp/power-bi  tools/call           → 200 + resultado
```

### Headers presentes en TODOS los requests (con OAuth)

```
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGci...  ← 2259 chars aprox.
Content-Type: application/json; charset=utf-8
Accept: application/json,text/event-stream
User-Agent: CopilotStudio PowerFx/1.99.0-local
x-ms-client-object-id: 45c3d46f-b00a-42ca-8f7d-414cf18bd1bd  ← OID en tenant PP
x-ms-client-tenant-id: 2f80d4e1-da0e-4b6d-84da-30f67e280e4b  ← tenant Power Platform
x-ms-client-principal-id: 45c3d46f-...                        ← igual que object-id
x-ms-client-principal-name: ebaltodano@itqscr.com             ← ✅ LLEGA con OAuth
x-ms-organization-id: Default-2f80d4e1-...
x-ms-entra-agent-name: crcbb_AgenteSynapseSQL
x-ms-correlation-id: 4376dda7-...  ← mismo en toda la conversación
x-ms-client-request-id: 4e096d5a-...  ← único por request
```

> **Diferencia clave vs sin OAuth:** `Authorization: Bearer` presente + `x-ms-client-principal-name` ya llega.

---

## 4. Detalle Completo por Tipo de Request

### 4.1 `initialize`

**Propósito:** CS abre el protocolo MCP y negocia capacidades.

**Headers adicionales exclusivos de este request:**
```
x-ms-client-session-id: c569a964-8529-46ce-b81a-44796b7b2261  ← solo en initialize
```

**Body enviado por CS:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "mcs",
      "version": "1.0.0",
      "agentName": "AgenteSynapseSQL",
      "agentAuthenticationMode": "Integrated",
      "appId": "fcaf903a-3b2c-44e9-9d6e-8983d0255cd1",
      "cdsBotId": "71542850-4755-f111-bec7-000d3a4e4928",
      "channelId": "pva-studio",
      "lcat": "M365_COPILOT_USER"
    },
    "sessionContext": {}
  }
}
```

**Respuesta esperada (200 OK):**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": { "tools": {} },
    "serverInfo": { "name": "eVistaMCP", "version": "1.0.0" }
  }
}
```

**Notas:**
- El `id` que llega puede ser `"1"` (string) o un número — el servidor debe devolver el mismo valor.
- `agentAuthenticationMode: "Integrated"` aparece aunque el conector tenga OAuth configurado como "Manual".
- `channelId: "pva-studio"` indica que se está probando desde el canvas de Copilot Studio. En producción sería diferente.

---

### 4.2 `notifications/initialized`

**Propósito:** El cliente confirma que recibió el `initialize` satisfactoriamente. Es una notificación unidireccional — no espera respuesta JSON-RPC.

**Body enviado por CS:**
```json
{
  "jsonrpc": "2.0",
  "id": null,
  "method": "notifications/initialized"
}
```

> ⚠️ **Quirk de Copilot Studio:** Envía `"id": null` aunque las notificaciones MCP no deberían tener `id`.
> Algunos servidores fallan si intentan devolver un JSON-RPC con `id: null`.

**Respuesta esperada:**
```
HTTP 202 Accepted
(body vacío)
```

**Por qué 202 y no 200:** Las notificaciones MCP son fire-and-forget. Devolver un JSON-RPC response causaría que CS lo trate como error de protocolo.

---

### 4.3 `tools/list`

**Propósito:** CS descubre qué tools expone el servidor para presentarlas al LLM.

**Body enviado por CS:**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/list",
  "params": {}
}
```

**Respuesta esperada (200 OK):**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "tools": [
      {
        "name": "obtener_usuario_oid",
        "description": "Devuelve el OID, nombre y email del usuario autenticado.",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      }
    ]
  }
}
```

**Notas:**
- CS llama `tools/list` **en cada turno**, no lo cachea. Actualizar la lista de tools en el servidor se refleja inmediatamente en el siguiente turno.
- Si una tool no tiene `inputSchema`, CS puede fallar al mostrarla. Incluir siempre aunque `properties` esté vacío.
- El LLM de CS decide cuál tool llamar basándose únicamente en el `name` y `description` — optimizar esos campos.

---

### 4.4 `tools/call`

**Propósito:** El LLM de CS decide llamar una tool específica con argumentos.

**Body enviado por CS:**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "tools/call",
  "params": {
    "name": "obtener_usuario_oid",
    "arguments": {}
  }
}
```

**Respuesta exitosa esperada (200 OK):**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "oid:    a7499e62-87e8-4cf2-b21a-980b9e935b46\nnombre: Ezequiel Baltodano Cubillo\nemail:  ebaltodano@itqscr.com"
      }
    ]
  }
}
```

**Respuesta de error de tool (200 OK con isError):**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "result": {
    "isError": true,
    "content": [
      {
        "type": "text",
        "text": "No autorizado. Se requiere autenticación."
      }
    ]
  }
}
```

> ⚠️ **Crítico:** Errores de tool se devuelven con **HTTP 200** y `isError: true` en el resultado.
> Un HTTP 4xx/5xx hace que CS marque la tool como fallida y no muestra el mensaje al usuario.

---

## 5. El JWT Bearer — Análisis Completo

### 5.1 Cómo decodificar el payload (sin verificar firma)

```csharp
// Para diagnóstico — NO usar en validación de seguridad
private static object? DecodeJwtPayload(string? authHeader)
{
    if (authHeader is null || !authHeader.StartsWith("Bearer ")) return null;
    var token = authHeader["Bearer ".Length..];
    var parts = token.Split('.');
    if (parts.Length < 2) return null;
    var pad = parts[1].Length % 4 switch { 2 => "==", 3 => "=", _ => "" };
    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1] + pad));
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
    catch { return null; }
}
```

### 5.2 Claims del JWT v1 emitido por CS (Authorization Code)

CS con Authorization Code grant emite tokens **v1** (`iss: sts.windows.net`), no v2:

```json
{
  "iss": "https://sts.windows.net/a778774c-ccbf-4b7d-a778-2c879d68ad71/",
  "aud": "api://5d46184e-d0d8-4add-96ec-b329e7fb57a4",
  "tid": "a778774c-ccbf-4b7d-a778-2c879d68ad71",
  "oid": "a7499e62-87e8-4cf2-b21a-980b9e935b46",
  "name": "Ezequiel Baltodano Cubillo",
  "email": "ebaltodano@itqscr.com",
  "upn": "ebaltodano@itqscr.com",
  "appid": "5d46184e-d0d8-4add-96ec-b329e7fb57a4",
  "scp": "user_impersonation",
  "roles": ["ITQS_Global_RWX", "ITQS_Customer_RWX"],
  "exp": 1748000000
}
```

**Claims útiles para identidad:**

| Claim | Descripción | Notas |
|-------|-------------|-------|
| `oid` | OID del usuario **en el tenant ITQS** | ✅ Confiable. Usar para identidad. |
| `email` | Email del usuario | ✅ Disponible en v1 |
| `upn` | UPN (User Principal Name) | ✅ Igual al email en ITQS |
| `name` | Nombre para mostrar | ✅ Display name |
| `roles` | Roles del App Registration | ✅ Asignados en Enterprise App de AAD |
| `scp` | Scopes concedidos | `user_impersonation` por defecto |
| `appid` | Client ID de la App Registration | Igual al `aud` sin `api://` |
| `tid` | Tenant ID | Para verificar que viene del tenant correcto |

> **Diferencia con sin-OAuth:** Con OAuth, el `oid` en el JWT es el OID real en el tenant ITQS.
> Sin OAuth, `x-ms-client-object-id` tiene el OID del tenant de Power Platform (inutilizable para Graph).

### 5.3 Token v1 vs v2 — El Problema del Issuer

**CS emite tokens v1 aunque el authority del middleware sea v2.** Esto causa que el middleware ASP.NET rechace el token con `jwt.authenticated = false`.

| | Token v1 (CS Authorization Code) | Token v2 |
|---|---|---|
| `iss` | `https://sts.windows.net/{tenantId}/` | `https://login.microsoftonline.com/{tenantId}/v2.0` |
| `aud` | `api://{clientId}` | `api://{clientId}` |
| Claims de usuario | `email`, `upn`, `name` directos | `preferred_username`, `name` |
| Emitido por CS cuando | Authorization Code grant | Client Credentials / algunos flows v2 |

### 5.4 Fix del Middleware — Aceptar v1 y v2

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority        = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        o.Audience         = $"api://{clientId}";
        o.MapInboundClaims = false; // conserva oid, upn, name, email en nombre original

        // CRÍTICO: CS Authorization Code emite v1 (sts.windows.net).
        // Sin ValidIssuers, el middleware rechaza el token aunque sea válido.
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = new[]
            {
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/"  // ← v1 issuer, obligatorio para CS
            },
            ValidAudience = $"api://{clientId}"
        };
    });
```

> **Por qué `MapInboundClaims = false`:** Sin esta opción, ASP.NET Core mapea `name` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` (nombre largo). Con `false`, los claims conservan sus nombres cortos (`oid`, `upn`, `name`, `email`) tal como vienen en el JWT.

---

## 6. Extraer Identidad del JWT en C#

```csharp
/// <summary>
/// Extrae CallerInfo del JWT validado (ctx.User) o de headers CS como fallback.
/// Con OAuth y MapInboundClaims=false, los claims v1 conservan sus nombres originales.
/// </summary>
private static CallerInfo ExtractCaller(HttpRequest req, HttpContext ctx)
{
    // 1. JWT validado por el middleware — fuente más confiable
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        var oid   = ctx.User.FindFirst("oid")?.Value;
        var name  = ctx.User.FindFirst("name")?.Value;
        // v1 usa "email" y "upn"; v2 usa "preferred_username"
        var email = ctx.User.FindFirst("email")?.Value
                 ?? ctx.User.FindFirst("preferred_username")?.Value
                 ?? ctx.User.FindFirst("upn")?.Value;
        var upn   = ctx.User.FindFirst("upn")?.Value
                 ?? ctx.User.FindFirst("preferred_username")?.Value;

        return new CallerInfo(oid, upn, name, email);
    }

    // 2. Fallback — headers CS (sin OAuth, identidad no confiable)
    var hOid = req.Headers.TryGetValue("x-ms-client-object-id", out var hOidVal)
        ? hOidVal.ToString() : null;
    var hUpn = req.Headers.TryGetValue("x-ms-client-principal-name", out var hUpnVal)
        ? hUpnVal.ToString() : null;  // En la práctica no llega sin OAuth

    return new CallerInfo(hOid, hUpn);
}
```

---

## 7. Comparación Completa: Con OAuth vs Sin OAuth

| Aspecto | Sin OAuth | Con OAuth (Authorization Code) |
|---------|-----------|--------------------------------|
| `Authorization` header | ❌ Ausente | ✅ `Bearer <jwt>` en los 4 requests |
| `x-ms-client-principal-name` | ❌ No llega | ✅ Llega (`ebaltodano@itqscr.com`) |
| `x-ms-client-object-id` | ✅ OID de tenant PP | ✅ OID de tenant PP (NO cambía) |
| OID real del usuario | ❌ No disponible | ✅ `oid` claim del JWT (tenant ITQS) |
| Email del usuario | ❌ No disponible | ✅ `email`/`upn` claim del JWT |
| Nombre del usuario | ❌ No disponible | ✅ `name` claim del JWT |
| Roles del usuario | ❌ No disponible | ✅ `roles` claim del JWT |
| Graph API funciona con OID | ❌ OID de otro tenant → 404 | ✅ OID correcto → funciona |
| Token version | n/a | v1 (`sts.windows.net`) |
| `jwt.authenticated` en ASP.NET | `false` | `true` (con fix de ValidIssuers) |
| Seguridad de identidad | ❌ Fácil de falsificar | ✅ Firma JWKS verificada por ASP.NET |

---

## 8. Checklist para un Nuevo MCP con OAuth

- [ ] App Registration en Azure AD con `api://{clientId}/user_impersonation` scope expuesto
- [ ] Middleware `AddJwtBearer` con `ValidIssuers` que incluya el issuer **v1** (`sts.windows.net`) y v2
- [ ] `MapInboundClaims = false` para conservar nombres cortos de claims
- [ ] Endpoint del conector configurado en Copilot Studio como **Manual + Authorization Code**
- [ ] Scope: `api://{clientId}/user_impersonation` (completo con `api://`)
- [ ] `ExtractCaller` lee `oid`, `name`, `email`/`upn` del `ctx.User` (JWT validado) primero
- [ ] Responder `notifications/*` con **HTTP 202** sin body
- [ ] Errores de tool devueltos como **HTTP 200 + `isError: true`** en el resultado JSON-RPC
- [ ] Log diagnóstico: decodificar payload JWT (base64) sin verificar firma — útil para debugging inicial
