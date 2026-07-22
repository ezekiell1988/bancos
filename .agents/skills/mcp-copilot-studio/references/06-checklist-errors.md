# MCP — Checklist, Variables de Entorno y Errores Comunes

## 1. Variables de Entorno Comunes

```env
# Azure AD (para acceder a Microsoft Graph, si necesitas información del usuario)
TENANT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
CLIENT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
CLIENT_SECRET=tu_client_secret

# Azure Search (si usas búsqueda semántica)
AZURE_SEARCH_ENDPOINT=https://mi-search.search.windows.net
AZURE_SEARCH_API_KEY=tu_api_key
AZURE_SEARCH_INDEX_NAME=mi-indice

# Redis (para caché de tokens y resultados)
REDIS_CONNECTION_STRING=mi-redis.redis.cache.windows.net:6380,password=xxx,ssl=True
```

---

## 2. Checklist de Validación ante Copilot Studio

Antes de configurar el conector en Copilot Studio, verificar:

```
□ GET / → responde HTTP 200 con "MCP ready"

□ POST / con body {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18",...}}
    → responde con protocolVersion, capabilities, serverInfo

□ POST / con body [{"jsonrpc":"2.0"}] (array malformado)
    → NO falla, normaliza y responde con initialize válido

□ POST / con body {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
    → responde lista de tools con name, title, description, inputSchema

□ POST / con body {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"mi_tool","arguments":{...}}}
    → responde {content:[{type:"text",text:"..."}]}

□ POST / con body {"jsonrpc":"2.0","id":4} (sin method, solo id)
    → responde HTTP 202

□ (Azure Functions) host.json tiene routePrefix: ""

□ El endpoint NO tiene autenticación en sí mismo (Authorization: Anonymous)
    Copilot Studio maneja su propia auth vía headers

□ Content-Type: application/json en todas las respuestas JSON

□ La URL es HTTPS
```

---

## 3. Configurar en Copilot Studio

1. Ir a tu agente en Copilot Studio
2. `Tools` → `Add a tool` → `Model Context Protocol (MCP)`
3. En **Server URL** poner la URL base del endpoint:
   - Azure Functions: `https://mi-function.azurewebsites.net`
   - Node/Express: `https://mi-app.azurewebsites.net`
   - Next.js: `https://mi-app.vercel.app/api/mcp`
4. Las herramientas se descubren automáticamente vía `tools/list`
5. El agente usará las tools según las descripciones en sus prompts de sistema

> **IMPORTANTE**: La URL debe ser **HTTPS**. HTTP no es aceptado por Copilot Studio.

---

## 4. Errores Comunes y Soluciones

| Síntoma | Causa | Solución |
|---------|-------|----------|
| Copilot Studio no descubre las tools | `routePrefix` no está vacío (Azure Functions) | Agregar `"routePrefix": ""` en `host.json` |
| Error 400/500 al conectar | No se maneja el array `[{"jsonrpc":"2.0"}]` | Implementar `NormalizeBatch` |
| Tools aparecen pero no ejecutan | El resultado no tiene `content[].text` | Envolver el resultado en `{content:[{type:"text",text:"..."}]}` |
| "Method not found" en notifications | No se maneja `notifications/initialized` | Agregar case que retorne `Results.StatusCode(202)` |
| URL no conecta | URL es HTTP o tiene `/api/` prefix no esperado | Usar HTTPS y verificar `routePrefix: ""` |
| Tool no visible para usuario | `IsPrivate` es `true` y no hay email | Revisar headers de auth o hacer tool pública |
| "sesión expirada" / nombre y email "no disponible" después de ~1h | Scope no incluye `offline_access` → sin refresh token el JWT expira y no se renueva | Agregar `offline_access openid profile email` al Scope del conector en CS. Ver [09-oauth2-real-data.md](09-oauth2-real-data.md) sección 1 |
| JWT presente pero `is_authenticated=false` | Token expirado — el middleware JWT lo rechaza correctamente | Refrescar sesión en Copilot Studio (nuevo turno o reconectar el conector) |
| JWT válido (`authResult.Succeeded=true`) pero claims vacíos y tool lanza `jwtRejected` | **Azure Functions isolated**: `UseAuthentication()` no corre automáticamente. `httpContext.User` queda anónimo aunque el token sea válido. | Agregar manualmente después de `AuthenticateAsync`: `if (authResult.Succeeded && authResult.Principal is not null) httpContext.User = authResult.Principal;` (ver [05-implementation-functions.md](05-implementation-functions.md) sección JWT) |
| `AuthenticateAsync` retorna `null` o `NotFound` | El esquema JWT no está registrado en DI. `AddJwtBearer` no fue llamado en `Program.cs` o en el módulo de DI | Verificar que `services.AddAuthentication().AddJwtBearer(...)` esté registrado antes de `builder.Build()` |
| Error de versión | Cliente envía `2024-11-05` y servidor no lo soporta | Aceptar ambas versiones en `CompatibleVersions` |
| `JsonElement` vacío al leer params | `params` no existe en el body | Usar `TryGetProperty` antes de acceder |
| `IOptions` falla al arrancar | Configuración `Mcp:*` no está en `appsettings.json` | Agregar sección `"Mcp"` o marcar propiedades como no `[Required]` |
| `"Tool did not respond with success"` en CS | Error JSON-RPC -32603 devuelto por el servidor | Revisar el error real con `scripts/test-mcp-local.ps1` o directamente con `Invoke-RestMethod` |
| Tool privada lanza error de auth aunque CS envíe OID | Graph devuelve `(null, null)` porque OID es de otro tenant | Usar `caller.Email ?? caller.Upn` como email efectivo — ver [07-identity-graph.md](07-identity-graph.md) |
| Graph devuelve 404 para el OID del usuario | Tenant mismatch: OID del tenant de CS ≠ tenant del App Registration | Buscar usuario por UPN en vez de OID, o registrar la app en el tenant correcto |
| No sé qué headers envía Copilot Studio | Sin visibilidad de los headers reales | Agregar endpoint `GET /diag/whoami` — ver [07-identity-graph.md](07-identity-graph.md) §7 |
