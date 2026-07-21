# Bancos.Mcp

Servidor MCP independiente para Copilot Studio. No tiene referencias a `Bancos.Api`, EF Core, SQL Server ni configuración de base de datos.

## Ejecutar localmente

```bash
dotnet run --project src/Bancos.Mcp --launch-profile https
```

El endpoint MCP acepta `POST /` (y rutas comodín) con JSON-RPC 2.0. El perfil local escucha en `https://localhost:7241`; el servidor redirige a HTTPS fuera del entorno de pruebas.

## Flujo compatible

* `initialize` negocia `2024-11-05` o `2025-06-18`.
* `tools/list` expone `health_status`, una tool estática sin acceso a datos.
* `tools/call` devuelve siempre `content: [{ type: "text", text: "..." }]`.
* `notifications/*` y respuestas enviadas por el cliente devuelven HTTP 202 sin cuerpo.
* La variante de Copilot Studio `[{"jsonrpc":"2.0"}]` se normaliza a `initialize`.

## Validación

```bash
dotnet build src/Bancos.Mcp
dotnet test tests/Bancos.Mcp.Tests
```

Para configurarlo posteriormente en Copilot Studio se deberá usar una URL HTTPS pública. La autenticación y el acceso a datos reales quedan fuera del alcance de esta fase.
