---
title: Bancos.Mcp
description: Configuración y diagnóstico del servidor MCP local de Bancos
---

## Bancos.Mcp

Servidor MCP independiente para Copilot Studio. No tiene referencias a `Bancos.Api`, EF Core, SQL Server ni configuración de base de datos.

## Ejecutar localmente

```bash
dotnet run --project src/Bancos.Mcp --launch-profile https
```

El endpoint MCP acepta `POST /` (y rutas comodín) con JSON-RPC 2.0. El perfil local escucha en `https://localhost:7241`; el servidor redirige a HTTPS fuera del entorno de pruebas.

## Uso desde VS Code

El workspace registra `bancosMcp` en `.vscode/mcp.json` como servidor HTTP local
con URL `https://localhost:7241`. La entrada no contiene cabeceras, tokens ni
secretos.

1. Confía el certificado de desarrollo si macOS no reconoce `https://localhost:7241`:

	```bash
	dotnet dev-certs https --trust
	```

2. Inicia el servidor con el perfil HTTPS:

	```bash
	dotnet run --project src/Bancos.Mcp --launch-profile https
	```

3. Recarga la ventana de VS Code después de incorporar el archivo o de modificar la
	configuración MCP. En la vista de servidores MCP, `bancosMcp` debe aparecer
	conectado y exponer `health_status`.
4. Desde el chat en modo agente, invoca `health_status` sin argumentos. La respuesta
	correcta tiene `content` con tipo `text` y estado `Estado: disponible`.

## Diagnóstico local

Para separar un problema de conexión de uno del cliente, consulta el endpoint HTTPS
directamente. `--insecure` solo acepta el certificado de desarrollo durante esta
verificación local.

```bash
curl --silent --show-error --fail --insecure \
  --header 'Content-Type: application/json' \
  --data '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' \
  https://localhost:7241/

curl --silent --show-error --fail --insecure \
  --header 'Content-Type: application/json' \
  --data '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"health_status","arguments":{}}}' \
  https://localhost:7241/
```

La primera respuesta debe listar `health_status`. La segunda debe devolver
`result.content[0]` con `type` igual a `text`. Este servidor no tiene referencias a
`Bancos.Api`, SQL Server, OAuth, archivos de entrada ni datos financieros.

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
