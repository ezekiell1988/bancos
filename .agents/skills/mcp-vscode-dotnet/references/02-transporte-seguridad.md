# 02 — stdio principal y seguridad HTTP opcional

## Transporte principal: stdio

Todo desarrollo local usa:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(CompanyMcpCatalog.Assembly);

await builder.Build().RunAsync();
```

Reglas obligatorias:

- stdout contiene exclusivamente mensajes MCP generados por el SDK.
- Logs, diagnósticos, banners y resultados del smoke van a stderr.
- No usar `Console.WriteLine`; usar `ILogger` o `Console.Error` en herramientas de prueba.
- VS Code inicia el proceso configurado en `command` y lo termina al detener el server.
- Cada cliente obtiene su propio proceso y su propia sesión.
- `stdio` no escucha un puerto y no tiene endpoint.

En `.vscode/mcp.json`, pasar únicamente variables de entorno necesarias. No colocar
secretos literales ni asumir que todo el entorno heredado debe estar disponible.

## Transporte opcional de publicación: Streamable HTTP

Esta sección solo aplica cuando se solicita publicar el MCP en Azure Container Apps, VS Code
remoto o Copilot Studio. No levantar HTTP para agregar, ajustar o depurar tools.

Usar Streamable HTTP en una ruta explícita:

```csharp
app.MapMcp("/mcp");
```

No habilitar SSE legacy ni WebSockets para un servidor nuevo. VS Code con `type: "http"`
prueba primero Streamable HTTP. Copilot Studio también requiere Streamable HTTP.

## HTTP stateless por defecto

Para tools request/response, configurar el transporte stateless:

```csharp
.WithHttpTransport(options =>
    options.SessionMode = HttpServerSessionMode.Stateless)
```

Ventajas:

- No hay sesiones en memoria ni afinidad por instancia.
- El servidor escala horizontalmente.
- Cada request recibe el `CancellationToken` del request HTTP.
- Reinicios y despliegues no pierden estado de negocio porque ese estado es externo.

Usar stateful únicamente si existe un requisito concreto de notificaciones no solicitadas,
sampling/elicitation iniciado por el servidor o aislamiento por sesión. Documentar entonces
afinidad, límites, expiración y comportamiento durante despliegues.

## Validación del host HTTP

Kestrel no debe aceptar cualquier `Host`. En desarrollo:

```json
{
  "AllowedHosts": "localhost;127.0.0.1;[::1]"
}
```

En Container Apps reemplazarlo mediante configuración por el hostname público exacto
(`*.azurecontainerapps.io` o el dominio custom asignado). Esto reduce ataques de DNS
rebinding y generación de URLs con hosts no confiables.

## TLS remoto

- Toda URL remota usa HTTPS con certificado válido y cadena confiable.
- Container Apps termina TLS automáticamente en el ingress externo; no exponer un ingress
  HTTP sin TLS hacia consumidores remotos.
- No desactivar validación TLS en VS Code ni en el smoke test.

## CORS HTTP

CORS solo aplica a clientes que ejecutan llamadas desde un navegador con otro origen. Un
cliente MCP server-to-server no necesita una política abierta.

- No agregar `AllowAnyOrigin()` por defecto.
- Si un cliente web lo exige, usar una policy nombrada con orígenes, métodos y headers
  explícitos.
- CORS no reemplaza `AllowedHosts`, autenticación ni autorización.

## Autenticación

### stdio local

No existe una frontera de red, pero las tools todavía deben aplicar autorización de dominio
y limitar filesystem/comandos. Las credenciales se proporcionan mediante `inputs`, secret
stores o variables explícitas; nunca se escriben en `mcp.json`.

### API key HTTP

Adecuada para un POC o integración técnica. Leerla desde secret store/configuración y
compararla en middleware con tiempo constante. En `mcp.json` usar `${input:...}`; nunca
versionar el valor.

### OAuth / Microsoft Entra ID para HTTP

Preferido para producción multiusuario. Configurar autenticación antes de autorización y
proteger el endpoint MCP:

```text
UseAuthentication → UseAuthorization → MapMcp(...).RequireAuthorization()
```

El SDK propaga el `ClaimsPrincipal` autenticado a la ejecución de la tool. Inyectar
`ClaimsPrincipal` en el método en lugar de decodificar JWT manualmente. Para filtrar tools
por `[Authorize]`, usar los filtros de autorización que ofrezca la versión instalada del
SDK.

## Autorización de tools

La autenticación del endpoint no sustituye reglas por tool:

- Lecturas: scope/rol mínimo.
- Escrituras: scope/rol + preview + `apply: true`.
- Destructivas: lo anterior + `confirm: true` y autorización específica.
- Acceso a downstream: managed identity, on-behalf-of u otra delegación explícita; no
  reenviar ciegamente el token destinado al MCP.

## Redacción y observabilidad

- Registrar nombre de tool, duración, resultado lógico y trace ID.
- No registrar body completo, argumentos sensibles, tokens ni connection strings.
- Devolver al modelo errores accionables pero sin stack trace ni payload crudo de la API.
- Aplicar rate limiting y límites de concurrencia en el endpoint remoto.

Fuentes primarias:

- <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md>
- <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/identity/identity.md>
