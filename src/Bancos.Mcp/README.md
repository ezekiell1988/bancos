---
title: Bancos.Mcp
description: Configuración y diagnóstico del servidor MCP local de Bancos
---

## Bancos.Mcp

Servidor MCP auxiliar independiente para Copilot Studio. No referencia ni hospeda
`Bancos.Api`, pero usa EF Core y SQL Server para su catálogo local de plantillas de
importación.

El catálogo se administra mediante `McpCatalogDbContext`, sus migraciones propias y
una base de datos distinta a la de `Bancos.Api`. Los dos proyectos no comparten
tablas, historial `__EFMigrationsHistory` ni configuración de conexión.

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
	conectado y exponer `health_status` y `detect_import_template`.
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

La primera respuesta debe listar las herramientas disponibles. La segunda debe devolver
`result.content[0]` con `type` igual a `text`.

## Detectar plantilla local

`detect_import_template` recibe `relativePath`, una ruta relativa a
`FileTemplateDetection:InputDirectory`. La configuración base usa `src/input` y
limita cada archivo a 10 MiB. Para XLS/XLSX también acota filas, celdas y caracteres
extraídos; los límites se configuran en `FileTemplateDetection`. Acepta exclusivamente
`pdf`, `csv`, `xls` y `xlsx`.

```bash
curl --silent --show-error --fail --insecure \
	--header 'Content-Type: application/json' \
	--data '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"detect_import_template","arguments":{"relativePath":"subcarpeta/archivo.csv"}}}' \
	https://localhost:7241/
```

La respuesta exitosa contiene solo `idImportTemplates`. La herramienta rechaza rutas
absolutas, traversal, enlaces simbólicos, extensiones no admitidas, archivos que
superan el límite y contenido que no coincide con la extensión. No persiste el
archivo, no consulta SQL Server y no incluye contenido financiero en la respuesta ni
en los errores. La eliminación del archivo de entrada sigue siendo responsabilidad del
usuario.

El endpoint MCP aplica un límite por ventana de solicitudes y una política de
concurrencia para sus llamadas POST. Los valores locales se ajustan en
`McpToolRateLimit`; una solicitud rechazada recibe HTTP 429 sin contenido del archivo.

## Flujo compatible

* `initialize` negocia `2024-11-05` o `2025-06-18`.
* `tools/list` expone `health_status` y `detect_import_template`.
* `tools/call` devuelve siempre `content: [{ type: "text", text: "..." }]`.
* `notifications/*` y respuestas enviadas por el cliente devuelven HTTP 202 sin cuerpo.
* La variante de Copilot Studio `[{"jsonrpc":"2.0"}]` se normaliza a `initialize`.

## Validación

```bash
dotnet build src/Bancos.Mcp
dotnet test tests/Bancos.Mcp.Tests
```

Para configurarlo posteriormente en Copilot Studio se deberá usar una URL HTTPS pública. La autenticación y el acceso a datos reales quedan fuera del alcance de esta fase.
