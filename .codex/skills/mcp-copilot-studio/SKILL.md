---
name: mcp-copilot-studio
description: Build an MCP (Model Context Protocol) server compatible with Microsoft Copilot Studio using .NET 10 / C# 14. Primary implementation uses ASP.NET Core Minimal APIs; secondary covers Azure Functions .NET 10 Isolated. Covers protocol structure, Copilot Studio quirks, tool definition patterns, auth headers, and deployment guidance.
version: 2.1.0
---

# MCP para Copilot Studio — Skill de Construcción (.NET 10 / C# 14)

**WORKFLOW SKILL** — Guía completa para construir un servidor MCP (Model Context Protocol) que funcione correctamente con **Microsoft Copilot Studio** usando **.NET 10 y C# 14**. Implementación primaria: ASP.NET Core Minimal APIs. Implementación secundaria: Azure Functions .NET 10 Isolated.

> Requiere el skill [dotnet-10-csharp-14](../dotnet-10-csharp-14/SKILL.md) para patrones de C# 14, Options, TypedResults y resiliencia HTTP.

## Usa Este Skill Cuando:

- ✅ El usuario quiere crear un servidor MCP en .NET para conectar con Copilot Studio
- ✅ El usuario necesita exponer herramientas (tools) a un agente de IA vía MCP
- ✅ Hay dudas sobre qué versión de protocolo usar, cómo manejar auth, o por qué Copilot Studio no conecta
- ✅ El usuario necesita entender el protocolo antes de implementarlo

## NO Uses Este Skill Para:

- ❌ MCP para Claude Desktop, Cursor u otros clientes MCP distintos a Copilot Studio
- ❌ Configuración de pipelines de CI/CD o infraestructura general de Azure
- ❌ Creación de agentes dentro de Copilot Studio (eso es lado cliente, no lado servidor)

---

## Índice de Referencia

| Archivo | Contenido |
|---------|-----------|
| [01-protocol.md](references/01-protocol.md) | Protocolo JSON-RPC 2.0, transport HTTP, flujo initialize → tools/list → tools/call, códigos de error |
| [02-quirks-auth.md](references/02-quirks-auth.md) | Quirks exclusivos de Copilot Studio (batch malformado, wildcard route, routePrefix) y headers de autenticación |
| [03-tool-design.md](references/03-tool-design.md) | Diseño de tools: interfaz, JSON Schema, resultado, control de acceso, optimización de tokens (TOON) |
| [04-implementation-aspnet.md](references/04-implementation-aspnet.md) | Implementación completa ASP.NET Core Minimal APIs: todos los archivos listos para copiar |
| [05-implementation-functions.md](references/05-implementation-functions.md) | Implementación Azure Functions .NET 10 Isolated + referencia otros lenguajes |
| [06-checklist-errors.md](references/06-checklist-errors.md) | Checklist de validación, configuración en Copilot Studio, variables de entorno, errores comunes |
| [07-identity-graph.md](references/07-identity-graph.md) | Identidad del usuario: patrón CallerInfo, enriquecimiento con Graph, permisos, validación con az CLI, endpoint /diag/whoami |
| [08-no-auth-real-data.md](references/08-no-auth-real-data.md) | **Datos reales capturados** de Copilot Studio sin OAuth: headers exactos, secuencia de requests, tenant mismatch del OID, qué sí/no llega, cómo resolver identidad |
| [09-oauth2-real-data.md](references/09-oauth2-real-data.md) | **Datos reales capturados Con OAuth 2.0:** headers completos con Bearer JWT, detalle body+response de cada request (initialize / notifications / tools/list / tools/call), claims JWT v1 vs v2, fix del issuer `sts.windows.net`, comparación con/sin OAuth, checklist |

---

## Puntos Críticos — Resumen Rápido

1. **`routePrefix: ""`** — Obligatorio en `host.json` para Azure Functions. Sin esto, Copilot Studio no conecta.
2. **`NormalizeBatch`** — Copilot Studio envía `[{"jsonrpc":"2.0"}]` (array malformado). El servidor debe normalizarlo sin fallar.
3. **`content[0].text`** — Todo resultado de tool debe tener la forma `{content:[{type:"text",text:"..."}]}`. Sin esta estructura las tools no funcionan.
4. **`notifications/*` → 202** — Las notificaciones del cliente (ej. `notifications/initialized`) deben responderse con HTTP 202, no con JSON-RPC.
5. **HTTPS obligatorio** — Copilot Studio rechaza URLs HTTP. El servidor debe estar en HTTPS.
6. **Headers de identidad sin OAuth** — Copilot Studio envía `x-ms-client-object-id` (OID) pero **NO** envía `x-ms-client-principal-name` (UPN) en la práctica. El OID pertenece al tenant de Power Platform, que puede diferir del tenant propio → Graph puede dar 404. Ver [08-no-auth-real-data.md](references/08-no-auth-real-data.md).
7. **Sin email sin OAuth** — Sin `x-ms-client-principal-name` y con OID de tenant ajeno, Graph no puede resolver el usuario. Para identificar usuarios confiablemente se requiere OAuth en el conector o un argumento `email` explícito en la tool.
8. **Token v1 con OAuth (Authorization Code)** — CS emite tokens v1 (`iss: sts.windows.net`), NO v2, aunque el authority del middleware sea `login.microsoftonline.com/v2.0`. Sin agregar `ValidIssuers` con el issuer v1, el middleware ASP.NET rechaza el token (`jwt.authenticated = false`). Ver [09-oauth2-real-data.md](references/09-oauth2-real-data.md).
9. **`MapInboundClaims = false` obligatorio** — Sin esta opción, ASP.NET mapea los claims a nombres XML largos. Con `false`, `oid`, `name`, `email`, `upn` conservan sus nombres cortos del JWT, simplificando `ctx.User.FindFirst("oid")`.
10. **`x-ms-client-object-id` NO cambia con OAuth** — Ese header sigue siendo el OID del usuario en el tenant de Power Platform. El OID real del tenant propio llega solo en el `oid` claim del JWT Bearer.
11. **JWT en Azure Functions isolated: `UseAuthentication()` NO corre solo** — `AddAuthentication/AddJwtBearer` registra los servicios en DI pero el middleware ASP.NET no se ejecuta automáticamente. El overload `ConfigureFunctionsWebApplication(Action<IFunctionsWorkerApplicationBuilder>)` no existe o no aplica en todas las versiones. **Solución:** llamar manualmente `await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme)` dentro del handler.
12. **`AuthenticateAsync` NO asigna `httpContext.User`** — Es el error más sutil: `authResult.Succeeded == true` y `authResult.Principal` tiene todos los claims, pero `httpContext.User` sigue siendo anónimo. Esto ocurre porque solo el middleware `UseAuthentication()` asigna `httpContext.User`. Al llamar `AuthenticateAsync` manualmente hay que hacer la asignación explícita: `if (authResult.Succeeded && authResult.Principal is not null) httpContext.User = authResult.Principal;`
