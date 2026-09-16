# 05 — Tests stdio obligatorios y HTTP opcional

El MCP se valida primero y siempre por `stdio`. No considerar lista una tool que solo
compila o que únicamente fue probada mediante el host HTTP opcional.

## 1. Unit tests de dominio

Probar servicios y validadores sin ASP.NET ni MCP:

- Casos exitosos y errores de negocio.
- Cancelación.
- Límites/paginación.
- Safe write: preview no muta.
- Destructivas: falta de `confirm` rechazada.
- Paths y redacción de secretos.

## 2. Tests directos de la tool

Invocar el método C# con fakes de servicios y verificar el record/texto resultante. Esto
detecta mapping y validación sin depender del transporte.

## 3. Contract tests del catálogo

Usar `StdioClientTransport` para iniciar el servidor como child process y consultar
`tools/list` mediante el cliente oficial. Verificar:

- Conjunto exacto de nombres esperados.
- Nombres `snake_case` únicos.
- Descripciones no vacías y diferenciables.
- Argumentos requeridos/opcionales correctos.
- Hints `ReadOnly`, `Destructive`, `Idempotent` y `OpenWorld` coherentes.
- Output schema presente cuando se usa structured content.

Mantener un snapshot revisable del catálogo si una modificación accidental de schema sería
riesgosa. Actualizarlo conscientemente, nunca automáticamente en CI.

## 4. Smoke stdio end-to-end — obligatorio

Desde la raíz del skill/proyecto de ejemplo:

```bash
dotnet build examples/McpStdioServer/McpStdioServer.csproj
dotnet run --project examples/McpStdioSmoke -- \
  examples/McpStdioServer/McpStdioServer.csproj
```

El smoke usa `StdioClientTransport` del SDK oficial. Inicia el mismo proceso que iniciará
VS Code, ejecuta el handshake, lista tools y llama tools representativas. Toda salida del
runner va a stderr para conservar stdout como canal MCP. Plantillas:

- [../examples/McpStdioSmoke/McpStdioSmoke.csproj](../examples/McpStdioSmoke/McpStdioSmoke.csproj)
- [../examples/McpStdioSmoke/Program.cs](../examples/McpStdioSmoke/Program.cs)

## 5. Paridad HTTP — solo si se publica

Si existe `CompanyMcp.Http`, ejecutar además:

```bash
dotnet run --project examples/McpHttpServer --urls http://127.0.0.1:3001
dotnet run --project examples/McpHttpSmoke -- http://127.0.0.1:3001/mcp
```

El smoke HTTP no reemplaza el smoke stdio. Debe observar el mismo conjunto de nombres y
schemas porque ambos hosts cargan `CompanyMcpCatalog.Assembly`. Plantillas opcionales:

- [../examples/McpHttpSmoke/McpHttpSmoke.csproj](../examples/McpHttpSmoke/McpHttpSmoke.csproj)
- [../examples/McpHttpSmoke/Program.cs](../examples/McpHttpSmoke/Program.cs)

## Matriz mínima por tool

| Tipo | Verificaciones |
|---|---|
| Lectura | responde, schema correcto, valor concreto conocido, límite respetado |
| Escritura | sin `apply` no muta; con `apply` muta una vez; resultado incluye identificador |
| Destructiva | falta de `confirm` rechazada; cleanup en `finally` |
| Privada | anónimo no la ve/no la ejecuta; usuario autorizado sí |
| Filesystem | traversal y symlink escape rechazados |
| API externa | timeout/cancelación, error redactado, retry solo en operaciones seguras |

## Reglas de fixtures

- Marcar datos creados con `[MCP TEST]`.
- Cleanup en `finally`.
- No ejecutar smoke de escritura contra producción.
- Las credenciales llegan por secret store/variables, no por el repositorio.
- Un assert por comportamiento; no agrupar fallos distintos.

## CI

Orden recomendado:

```text
restore → build → unit → stdio contract/smoke → [HTTP parity] → publish
```

`StdioClientTransport` administra el proceso del smoke local. Levantar un proceso HTTP en
background y esperar `/health` solo dentro del job de publicación opcional; detenerlo
siempre aunque falle el smoke.

Fuente primaria:

- <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md>
