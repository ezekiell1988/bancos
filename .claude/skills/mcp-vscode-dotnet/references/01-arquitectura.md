# 01 — Arquitectura stdio-first con publicación HTTP opcional

## Decisión base

Usar .NET 10 y el SDK oficial. El proyecto principal es un host de consola con
`ModelContextProtocol` y `WithStdioServerTransport()`. El SDK se responsabiliza de JSON-RPC,
framing, negociación de versión, `initialize`, `tools/list`, `tools/call` y cancelación.

Streamable HTTP no forma parte del ciclo normal de desarrollo. Solo si existe un requisito
de publicación se agrega un host ASP.NET Core con `ModelContextProtocol.AspNetCore`.

No copiar el `protocol.mjs` del skill `mcp-vscode`, no escribir un loop de stdin/stdout, no
construir un controller JSON-RPC y no mantener versiones MCP manualmente. Esas piezas
pertenecen al SDK.

## Estructura recomendada

```text
src/CompanyMcp.Tools/
  CompanyMcp.Tools.csproj
  CompanyMcpCatalog.cs
  Features/
    WorkItems/
      ListWorkItemsTool.cs
      CreateWorkItemTool.cs
      IWorkItemService.cs
      WorkItemService.cs
    Documentation/
      SearchDocumentationTool.cs
      IDocumentSearch.cs
      DocumentSearch.cs

src/CompanyMcp.Stdio/
  CompanyMcp.Stdio.csproj
  Program.cs

# Crear únicamente cuando se solicite publicación remota:
src/CompanyMcp.Http/
  CompanyMcp.Http.csproj
  Program.cs
  McpModule.cs
  appsettings.json

tests/
  CompanyMcp.UnitTests/
  CompanyMcp.StdioContractTests/
  CompanyMcp.HttpParityTests/  # opcional
```

## Equivalencia con `mcp-vscode`

| Skill stdio/Node | Este skill stdio/.NET |
|---|---|
| `1 tool = 1 archivo` | `1 tool = 1 feature folder`; la clase pública sigue siendo una por tool |
| registry que lee `tools/*.mjs` | `WithToolsFromAssembly()` |
| VS Code ejecuta `node server.mjs` | VS Code ejecuta `dotnet run --project CompanyMcp.Stdio` |
| `server.mjs` no se modifica | los hosts no se modifican al agregar una tool |
| schema escrito a mano | schema generado desde firma, nullability y `[Description]` |
| `ToolError` | `McpException` o `CallToolResult { IsError = true }` |
| smoke por stdio | smoke con `StdioClientTransport` |

## Fronteras de responsabilidad

### Tool MCP

- Traduce argumentos MCP a una operación de dominio.
- Declara nombre, título, descripción y hints.
- Hace validación explícita de argumentos y autorización específica.
- Mapea el resultado a una respuesta breve y estable.

### Servicio de dominio

- Contiene la lógica reutilizable.
- No conoce el transporte, ASP.NET Core ni VS Code.
- Usa interfaces para APIs, persistencia y filesystem.
- Recibe `CancellationToken`.

### Host stdio principal

- Registra MCP, DI y `WithStdioServerTransport()`.
- Es iniciado y terminado por VS Code.
- Reserva stdout exclusivamente para mensajes MCP y envía logs a stderr.
- Es el único host usado al agregar, ajustar, probar o depurar tools.
- No escucha puertos ni expone endpoints.

### Host HTTP opcional

- Se crea solo para publicar el catálogo remotamente.
- Registra autenticación, health checks, middleware y Streamable HTTP.
- Mapea una única ruta estable, normalmente `/mcp`.
- No contiene lógica de tools.

## Registro

El catálogo vive en `CompanyMcp.Tools`. Ambos hosts deben indicar explícitamente ese
assembly, porque el assembly de entrada no contiene las tools:

```csharp
services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(CompanyMcpCatalog.Assembly);
```

Agregar una clase `[McpServerToolType]` basta para que aparezca en `tools/list`. Si se
necesita un catálogo dinámico por tenant o usuario, documentar la razón y usar las APIs de
colección/filtros del SDK; no mutar una colección singleton global durante un request.

Las dependencias nuevas de dominio todavía deben registrarse en DI, normalmente desde el
módulo de la feature o mediante una convención de assembly scanning. Eso no es registro de
la tool: el catálogo MCP continúa autodescubierto.

El host HTTP opcional cambia solo el transporte:

```csharp
services.AddMcpServer()
    .WithHttpTransport(options =>
        options.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly(CompanyMcpCatalog.Assembly);
```

Agregar una clase `[McpServerToolType]` al catálogo compartido basta para que aparezca en
`tools/list` de ambos hosts. Las dependencias nuevas de dominio se registran desde la
composición compartida; no se duplican en cada host.

## Puerto, endpoint y tool

```text
stdio
  → proceso hijo por cliente
  → stdin/stdout
  → sin puerto
  → sin endpoint

HTTP opcional
  → un proceso ASP.NET Core escucha un puerto local/interno
  → una ruta estable /mcp representa el catálogo
  → muchas tools viven dentro de ese mismo endpoint
```

No crear un puerto ni un endpoint por tool. Si existen dos servidores MCP realmente
independientes como procesos locales, necesitan puertos diferentes. Si comparten proceso,
se pueden enrutar por paths distintos, pero esa separación solo se justifica por catálogos,
autenticación o ciclos de despliegue diferentes.

## Camino local y publicación opcional

```text
VS Code
  → command: dotnet run --project src/CompanyMcp.Stdio
  → desarrollo, cambios, tests y debugging por stdio

Azure Container Apps (solo si se solicita)
  → publica src/CompanyMcp.Http como imagen de contenedor
  → VS Code/Copilot Studio: https://company-mcp.<env-id>.<region>.azurecontainerapps.io/mcp
```

Hay dos hosts delgados, pero una sola implementación y un solo catálogo. `CompanyMcp.Http`
es 100% opcional: no crearlo ni mantenerlo si el MCP solo se usa localmente.

Fuentes primarias:

- <https://github.com/modelcontextprotocol/csharp-sdk>
- <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md>
