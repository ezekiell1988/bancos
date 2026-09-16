---
name: mcp-vscode-dotnet
description: >
  Crear y mantener servidores MCP locales para VS Code usando .NET 10, C# 14,
  el SDK oficial MCP y stdio como transporte principal de desarrollo, cambios,
  pruebas y debugging. Permite agregar opcionalmente un host Streamable HTTP para
  publicar el mismo catálogo en Azure Container Apps y consumirlo desde Copilot Studio.
  Arquitectura por feature folders, autodescubrimiento por assembly, safe writes y
  técnicas para reducir tokens en catálogos, schemas y resultados.
  Triggers: mcp dotnet, mcp csharp, mcp stdio dotnet, vscode mcp dotnet, mcp http,
  streamable http, azure container apps, aca, mapmcp, mcp copilot studio, ahorrar tokens mcp,
  optimizar tools mcp, token budget mcp.
compatibility: Requiere .NET SDK 10 y acceso a NuGet/ACR; Azure solo para la publicación HTTP opcional.
metadata:
  version: "1.4.0"
---

# MCP .NET para VS Code — stdio primero, HTTP opcional

**WORKFLOW SKILL** para construir un MCP project-owned en .NET 10. El catálogo y la lógica
de tools son independientes del transporte:

- **Camino obligatorio y cotidiano:** `stdio`, iniciado por VS Code como child process.
- **Camino opcional y poco frecuente:** Streamable HTTP, agregado únicamente cuando se
  necesita publicar en Azure Container Apps, VS Code remoto o Copilot Studio.

Agregar, ajustar, probar y depurar tools se hace siempre contra `stdio`. El endpoint `/mcp`
y un puerto aparecen solamente en el host HTTP opcional; no existe un endpoint por tool.

La unidad de extensión es **1 tool = 1 feature folder**. El SDK descubre las clases
`[McpServerToolType]`; agregar una tool no modifica ninguno de los hosts.

## Usa este skill cuando

- Crear un MCP local en .NET 10/C# 14 para VS Code/GitHub Copilot mediante `stdio`.
- Agregar, cambiar, probar, auditar o depurar tools usando el host `stdio`.
- Compartir un único catálogo entre un host local `stdio` y, opcionalmente, un host HTTP.
- Diseñar o auditar el catálogo para reducir definiciones, schemas y resultados innecesarios.
- Publicar excepcionalmente el MCP en Azure Container Apps para un consumidor remoto.
- Preparar el host HTTP opcional para Copilot Studio.

## No uses este skill para

- MCP local Node.js/TypeScript por `stdio`: usar [mcp-vscode-node](../mcp-vscode-node/SKILL.md).
- Crear HTTP local como flujo normal de desarrollo; `stdio` es el default de este skill.
- Implementar JSON-RPC, framing o negociación de versiones a mano.
- Convertir automáticamente REST/OpenAPI en tools genéricas.

## Arquitectura obligatoria

```text
CompanyMcp.Tools     → catálogo, features y servicios compartidos
        ↑
CompanyMcp.Stdio     → host principal: desarrollo, cambios, pruebas y debugging
        ↑
CompanyMcp.Http      → host opcional: solo publicación remota/Container Apps
```

`CompanyMcp.Http` no se crea por anticipación. Se agrega cuando existe un requerimiento
real de publicación. Ambos hosts descubren exactamente el mismo assembly de tools.

## Flujo obligatorio

1. Crear o auditar la separación catálogo/hosts descrita en
   [references/01-arquitectura.md](./references/01-arquitectura.md).
2. Configurar `WithStdioServerTransport()` y reservar stdout para MCP según
   [references/02-transporte-seguridad.md](./references/02-transporte-seguridad.md).
3. Versionar `.vscode/mcp.json` con `type: "stdio"` siguiendo
   [references/04-vscode-clientes.md](./references/04-vscode-clientes.md).
4. Agregar o ajustar cada tool con
   [references/03-agregar-tool.md](./references/03-agregar-tool.md).
5. Compilar y ejecutar primero contract/smoke por `stdio` según
   [references/05-testing.md](./references/05-testing.md).
6. Aplicar el presupuesto de catálogo, schema y resultados de
   [references/09-eficiencia-tokens.md](./references/09-eficiencia-tokens.md).
7. Depurar siempre la ejecución `stdio` y completar
   [references/07-auditoria-errores.md](./references/07-auditoria-errores.md).
8. Solo si se solicita publicación, agregar el host HTTP, probar paridad del catálogo y
   aplicar [references/06-azure-container-apps.md](./references/06-azure-container-apps.md).
9. Solo si el consumidor es Copilot Studio, revisar
   [references/08-copilot-studio.md](./references/08-copilot-studio.md).

## Regla de oro — agregar una tool

```text
1. Copiar examples/tool-template.cs → CompanyMcp.Tools/Features/{Dominio}/{Nombre}Tool.cs
2. Cambiar Name, Title, Description, parámetros, hints y handler
3. Mantener la lógica de negocio en un servicio inyectado
4. Revisar que descripción, schema y resultado tengan solo lo necesario
5. dotnet build
6. Ejecutar el smoke stdio y verificar tools/list + tools/call
7. Depurar por stdio si algo falla
8. Probar HTTP solamente si el proyecto publica el host opcional
```

No se edita `Program.cs` de ningún host ni se registra la tool manualmente.
`WithToolsFromAssembly(CompanyMcpCatalog.Assembly)` descubre la nueva clase. Si la feature
introduce un servicio de dominio, registrar ese servicio en la composición compartida.

## Referencias

| Archivo | Contenido |
|---|---|
| [01-arquitectura.md](./references/01-arquitectura.md) | Catálogo compartido, host stdio principal, host HTTP opcional y regla puerto/endpoint |
| [02-transporte-seguridad.md](./references/02-transporte-seguridad.md) | stdio, stdout/stderr, lifecycle local y seguridad HTTP opcional |
| [03-agregar-tool.md](./references/03-agregar-tool.md) | Atributos, schema, DI, errores, resultados y safe writes |
| [04-vscode-clientes.md](./references/04-vscode-clientes.md) | `mcp.json` stdio local, debugging y configuración HTTP remota opcional |
| [05-testing.md](./references/05-testing.md) | Tests y smoke stdio obligatorios; paridad HTTP opcional |
| [06-azure-container-apps.md](./references/06-azure-container-apps.md) | Rama opcional de publicación en Azure Container Apps y CI/CD con OIDC |
| [07-auditoria-errores.md](./references/07-auditoria-errores.md) | Checklist stdio-first y diagnóstico rápido |
| [08-copilot-studio.md](./references/08-copilot-studio.md) | Compatibilidad opcional con Copilot Studio |
| [09-eficiencia-tokens.md](./references/09-eficiencia-tokens.md) | Estrategia de catálogos, schemas, resultados y selección de tools para ahorrar tokens |

## Ejemplos listos para copiar

Catálogo compartido:

- [McpTools.csproj](./examples/McpTools/McpTools.csproj)
- [CompanyMcpCatalog.cs](./examples/McpTools/CompanyMcpCatalog.cs)
- [GetServerTimeTool.cs](./examples/McpTools/Features/System/GetServerTimeTool.cs)
- [CreateTicketTool.cs](./examples/McpTools/Features/Tickets/CreateTicketTool.cs)
- [tool-template.cs](./examples/tool-template.cs)

Flujo principal `stdio`:

- [McpStdioServer.csproj](./examples/McpStdioServer/McpStdioServer.csproj)
- [stdio Program.cs](./examples/McpStdioServer/Program.cs)
- [McpStdioSmoke.csproj](./examples/McpStdioSmoke/McpStdioSmoke.csproj)
- [stdio smoke Program.cs](./examples/McpStdioSmoke/Program.cs)
- [vscode-mcp.local.json](./examples/config/vscode-mcp.local.json)

Publicación HTTP opcional:

- [McpHttpServer.csproj](./examples/McpHttpServer/McpHttpServer.csproj)
- [HTTP Program.cs](./examples/McpHttpServer/Program.cs)
- [McpModule.cs](./examples/McpHttpServer/McpModule.cs)
- [appsettings.json](./examples/McpHttpServer/appsettings.json)
- [McpHttpSmoke.csproj](./examples/McpHttpSmoke/McpHttpSmoke.csproj)
- [HTTP smoke Program.cs](./examples/McpHttpSmoke/Program.cs)
- [vscode-mcp.remote-oauth.json](./examples/config/vscode-mcp.remote-oauth.json)
- [vscode-mcp.remote-header.json](./examples/config/vscode-mcp.remote-header.json)
- [container-apps.yml](./examples/ci/container-apps.yml)
- [Dockerfile](./examples/McpHttpServer/Dockerfile)

## Puntos críticos

1. `stdio` es obligatorio para el ciclo de desarrollo; no necesita puerto ni endpoint.
2. VS Code inicia y termina el proceso .NET definido por `command` y `args`.
3. stdout contiene exclusivamente mensajes MCP; logs y diagnósticos van a stderr.
4. Cambios, contract tests, smoke tests y debugging se validan primero por `stdio`.
5. Un MCP es un catálogo con muchas tools; no crear un endpoint ni un puerto por tool.
6. HTTP es 100% opcional y solo aparece cuando se publica el mismo catálogo remotamente.
7. Si existe HTTP, usar Streamable HTTP stateless, una ruta estable `/mcp` y HTTPS remoto.
8. Parameters y DTOs generan schema, pero la validación de negocio sigue siendo explícita.
9. Write tools: preview por defecto; mutar solo con `apply: true`; delete exige además
   `confirm: true`.
10. No filtrar stack traces, tokens, connection strings ni payloads crudos.
11. Tras cambiar tools, reiniciar el server stdio y usar `MCP: Reset Cached Tools` si VS Code
    conserva el catálogo anterior.
12. El costo depende de las tools habilitadas, sus schemas y sus resultados; dividir un
    catálogo en varios MCP no ahorra tokens si todos permanecen habilitados.
13. Preferir un MCP por dominio coherente y separar dominios solo cuando puedan habilitarse
    selectivamente o tengan permisos, propietarios o ciclos de despliegue distintos.
