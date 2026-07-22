# MCP + Copilot Studio Sin OAuth — Datos Reales Capturados

> **Fuente:** Captura viva del slot `hangfire-dev` el 2026-05-22 via `diag.llmAuditEntry`.
> **Herramienta probada:** `obtener_usuario_oid` ("Quien soy yo?")
> **Conector CS:** `crcbb_AgenteSynapseSQL` — sin autenticación OAuth configurada.

---

## 1. Arquitectura de Red Real

```
Usuario en Copilot Studio
       │
       ▼
Microsoft Power Platform (tenant 2f80d4e1-...)
       │
       ▼  HTTPS POST /mcp/power-bi
Microsoft APIM Managed (msmanaged-na.azure-apim.net)
  └── /apim/crcbb-5Fmcp-20sin-20security-5F.../1e0b.../mcp/power-bi
       │
       ▼  headers x-ms-* inyectados por la plataforma
Tu servidor MCP (eVista.Hangfire hangfire-dev)
```

> **Clave**: la request pasa por el **APIM Managed de Microsoft** antes de llegar al servidor.
> El nombre del conector en el APIM (`mcp-sin-security`) confirma que es un conector sin OAuth.

---

## 2. Headers Completos — Lista Verificada

Capturados en condiciones reales. Todos los requests de la misma conversación comparten `x-ms-correlation-id`.

### Headers estándar HTTP
| Header | Valor ejemplo | Notas |
|--------|---------------|-------|
| `Accept` | `application/json,text/event-stream` | CS acepta SSE |
| `Content-Type` | `application/json; charset=utf-8` | Siempre JSON |
| `User-Agent` | `CopilotStudio PowerFx/1.99.0-local` | Versión de CS |
| `Host` | `evistaweb-hangfire-dev.azurewebsites.net` | Tu App Service |
| `Max-Forwards` | `10` | Injected by APIM |

### Headers de identidad del usuario
| Header | Valor ejemplo | ¿Llega sin OAuth? |
|--------|---------------|-------------------|
| `x-ms-client-object-id` | `45c3d46f-b00a-42ca-8f7d-414cf18bd1bd` | ✅ Siempre |
| `x-ms-client-principal-id` | `45c3d46f-...` (mismo que object-id) | ✅ Siempre |
| `x-ms-client-tenant-id` | `2f80d4e1-da0e-4b6d-84da-30f67e280e4b` | ✅ Siempre |
| `x-ms-client-principal-name` | — | ❌ **NO se envía** sin OAuth |
| `x-ms-organization-id` | `Default-2f80d4e1-da0e-4b6d-84da-30f67e280e4b` | ✅ Siempre |
| `x-ms-client-environment-id` | `/providers/Microsoft.PowerApps/environments/Default-...` | ✅ Siempre |
| `x-ms-client-session-id` | `c569a964-8529-46ce-b81a-44796b7b2261` | ✅ Solo en `initialize` |
| `x-ms-client-app-id` | `96ff4394-9197-43aa-b393-6a41652e21f8` | ✅ Siempre |

### Headers del agente de Copilot Studio
| Header | Valor ejemplo | Significado |
|--------|---------------|-------------|
| `x-ms-entra-agent-name` | `crcbb_AgenteSynapseSQL` | Nombre del bot/agente |
| `x-ms-entra-agent-id` | `fcaf903a-3b2c-44e9-9d6e-8983d0255cd1` | OID del agente en Power Platform |

### Headers de tracing / correlación
| Header | Valor ejemplo | Significado |
|--------|---------------|-------------|
| `x-ms-correlation-id` | `4376dda7-9ba2-44e9-9e00-2aad77cdd009` | **Mismo en toda la conversación** |
| `x-ms-client-request-id` | `4e096d5a-...` (único por request) | ID de cada request individual |
| `x-ms-coreframework-caller-activity-id` | `fc38cc5a-...` | Actividad en Power Platform |
| `x-ms-activity-vector` | `00.00.01.03...` | Vector de la cadena de actividades |
| `traceparent` | `00-5e9edde8c350a3fd20a5d55d0802f1e3-0bfbc023222fbdba-00` | W3C trace context |
| `Request-Id` | `|5e9edde8c350a3fd20a5d55d0802f1e3.0bfbc023222fbdba.` | .NET correlation ID |

### Headers inyectados por APIM / Azure App Service
| Header | Valor ejemplo | Origen |
|--------|---------------|--------|
| `X-MS-APIM-Referrer` | `https://msmanaged-na.azure-apim.net/apim/crcbb-5F.../` | APIM gateway |
| `X-MS-APIM-Callback` | `https://msmanaged-na.consent.azure-apim.net` | APIM consent endpoint |
| `X-Forwarded-For` | `::ffff:20.172.189.239,52.146.72.240, 20.88.153.195:11898` | Cadena de proxies |
| `X-Forwarded-Proto` | `https` | App Service |
| `CLIENT-IP` | `20.88.153.195:11898` | IP real del gateway MS |
| `DISGUISED-HOST` | `evistaweb-hangfire-dev.azurewebsites.net` | App Service |
| `X-SITE-DEPLOYMENT-ID` | `evistaweb__165d` | App Service deployment slot |

### Header que NO llega
| Header | Estado |
|--------|--------|
| `Authorization: Bearer ...` | ❌ **AUSENTE** — sin OAuth en el conector, no hay token |
| `x-ms-client-principal-name` | ❌ **AUSENTE** — la documentación de MS dice que llega, pero en la práctica no viene |

---

## 3. Secuencia Completa de Requests por Conversación

Cada vez que el usuario envía un mensaje al bot, Copilot Studio ejecuta esta secuencia completa:

```
[T+0ms]  POST /mcp/power-bi  {"jsonrpc":"2.0","id":"1","method":"initialize","params":{...}}
[T+10ms] POST /mcp/power-bi  {"jsonrpc":"2.0","id":null,"method":"notifications/initialized"}
[T+20ms] POST /mcp/power-bi  {"jsonrpc":"2.0","id":"2","method":"tools/list","params":{}}
[T+30ms] POST /mcp/power-bi  {"jsonrpc":"2.0","id":"3","method":"tools/call","params":{"name":"...","arguments":{...}}}
```

> CS re-inicializa el protocolo **en cada turno** del usuario. No hay conexión persistente.

### Body real de `initialize`
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "initialize",
  "params": {
    "capabilities": {},
    "clientInfo": {
      "agentAuthenticationMode": "Integrated",
      "agentName": "AgenteSynapseSQL",
      "appId": "fcaf903a-3b2c-44e9-9d6e-8983d0255cd1",
      "cdsBotId": "71542850-4755-f111-bec7-000d3a4e4928",
      "channelId": "pva-studio",
      "lcat": "M365_COPILOT_USER",
      "name": "mcs",
      "version": "1.0.0"
    },
    "protocolVersion": "2024-11-05",
    "sessionContext": {}
  }
}
```

**Campos útiles del `clientInfo`:**
- `agentName` → nombre del bot en Copilot Studio
- `appId` → OID del agente/bot en Power Platform  
- `cdsBotId` → ID CDS del bot
- `channelId` → `pva-studio` cuando se prueba desde el canvas de CS
- `lcat` → `M365_COPILOT_USER` indica licencia del usuario

---

## 4. El Problema del Tenant Mismatch (OID Inutilizable para Graph)

### Estructura de tenants

```
Tenant ITQS (Azure AD)         Tenant Power Platform (Microsoft)
a778774c-ccbf-4b7d-...         2f80d4e1-da0e-4b6d-84da-30f67e280e4b
       │                                    │
  Usuarios ITQS                   OID del usuario en CS
  (EvistaPro, EvistaDev)          45c3d46f-b00a-42ca-8f7d-414cf18bd1bd
       │                                    │
  Graph API ITQS                  NO accesible desde tenant ITQS
```

### Por qué falla el lookup en Graph

1. CS envía `x-ms-client-object-id: 45c3d46f...`
2. Ese OID existe en el **tenant de Power Platform** (`2f80d4e1`), NO en el tenant ITQS (`a778774c`)
3. `GraphUserService.GetUserInfoAsync(oid)` llama `GET /v1.0/users/{oid}` con un token del tenant ITQS
4. Microsoft Graph retorna **404** porque el OID no existe en ese tenant
5. El fallback a `GetUserByUpnAsync` también falla porque `x-ms-client-principal-name` **no llega**
6. Resultado: `caller.DisplayName = null`, `caller.Email = null` → tools privadas denegadas

### La trampa de la documentación oficial de Microsoft

La documentación de CS dice que `x-ms-client-principal-name` se envía siempre en modo "Sin Security".
**Datos reales contradicen esto.** El header no está presente en ninguno de los 4 requests capturados.

---

## 5. Identidad Disponible Sin OAuth — Qué SÍ se puede usar

Sin JWT Bearer, la identidad confiable está limitada a:

| Dato | Header | Confiable | Utilidad |
|------|--------|-----------|----------|
| OID (Power Platform) | `x-ms-client-object-id` | ✅ Plataforma | Identificar usuario en sesión (no en AD propio) |
| Tenant (PP) | `x-ms-client-tenant-id` | ✅ Plataforma | Saber de qué tenant de PP viene |
| Nombre del agente | `x-ms-entra-agent-name` | ✅ Plataforma | Auditoría |
| Session ID | `x-ms-client-session-id` | ✅ Plataforma | Agrupar requests de una conversación |
| Correlation ID | `x-ms-correlation-id` | ✅ Plataforma | Tracing end-to-end |
| Email del usuario | — | ❌ **No disponible** sin OAuth | Requiere JWT o argumento explícito |

---

## 6. Cómo Resolver la Identidad Sin OAuth

### Opción A — OAuth en el conector (recomendada)
Configurar el conector MCP en Copilot Studio con autenticación OAuth (Azure AD / Entra ID).
- CS enviará un `Authorization: Bearer <jwt>` con claims reales del usuario ITQS
- El OID en el JWT pertenecerá al tenant ITQS → Graph funciona
- `x-ms-client-principal-name` también llega cuando hay OAuth

### Opción B — Argumento `email` en la tool (workaround)
Agregar un parámetro `email` explícito a la tool y pedirle al LLM que lo pase.
- El agente puede inferir el email del contexto de la sesión de M365
- No requiere configurar OAuth
- Menos seguro (el email puede ser manipulado)

### Opción C — Usar el OID de PP como identificador (limitado)
Mapear el OID de Power Platform (`45c3d46f`) a un usuario ITQS en una tabla de correspondencia.
- Mantenimiento manual
- Solo funciona si se conoce de antemano qué usuarios usarán el bot

---

## 7. Snippet — Extraer Identidad Real sin OAuth en C#

```csharp
private static CallerInfo ExtractCaller(HttpRequest req, HttpContext ctx)
{
    string? oid    = null;
    string? upn    = null;
    string? tenant = null;

    // 1. JWT Bearer (OAuth configurado) — fuente más confiable
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        oid    = ctx.User.FindFirst("oid")?.Value;
        upn    = ctx.User.FindFirst("preferred_username")?.Value
              ?? ctx.User.FindFirst("upn")?.Value;
        tenant = ctx.User.FindFirst("tid")?.Value;
    }

    // 2. Headers de CS — disponibles incluso sin OAuth
    // NOTA: x-ms-client-principal-name NO viene en la práctica sin OAuth.
    // Solo confiar en x-ms-client-object-id como identificador.
    if (oid is null)
        req.Headers.TryGetValue("x-ms-client-object-id", out var h);
    oid ??= req.Headers.TryGetValue("x-ms-client-object-id", out var hOid)
        ? hOid.ToString() : null;

    // x-ms-client-principal-name: documentado por MS pero ausente en práctica sin OAuth.
    // Intentar de todas formas como fallback por si en el futuro llega.
    if (upn is null && req.Headers.TryGetValue("x-ms-client-principal-name", out var hUpn)
        && !string.IsNullOrWhiteSpace(hUpn))
        upn = hUpn.ToString();

    // x-ms-client-tenant-id: tenant del usuario en Power Platform.
    // ADVERTENCIA: este tenant puede NO ser el mismo que el tenant de tu Azure AD.
    // El OID en este tenant no es resolvible con Graph del tenant propio.
    if (tenant is null)
        req.Headers.TryGetValue("x-ms-client-tenant-id", out var hTenant);

    return new CallerInfo(Oid: oid, Upn: upn);
}
```

---

## 8. Referencia Cruzada

| Tema | Ver |
|------|-----|
| Cómo configurar OAuth en el conector MCP de CS | [02-quirks-auth.md](02-quirks-auth.md) |
| Implementación GraphUserService con fallback UPN | [07-identity-graph.md](07-identity-graph.md) |
| Checklist de errores si la identidad falla | [06-checklist-errors.md](06-checklist-errors.md) |
