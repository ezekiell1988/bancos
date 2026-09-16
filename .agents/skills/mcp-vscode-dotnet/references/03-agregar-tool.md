# 03 — Agregar y diseñar una tool

## Flujo de una feature

```text
1. Crear Features/{Dominio}/{Nombre}Tool.cs
2. Marcar la clase con [McpServerToolType]
3. Marcar un método público con [McpServerTool]
4. Agregar [Description] al método y a cada argumento visible
5. Inyectar servicios de dominio como parámetros DI
6. Validar explícitamente argumentos y permisos
7. dotnet build + smoke stdio
```

`WithToolsFromAssembly(CompanyMcpCatalog.Assembly)` evita registrar la tool manualmente. Si
agregar una tool obliga a tocar un host, mapa o switch del catálogo, la arquitectura perdió
el autodescubrimiento. Un servicio de dominio nuevo sí debe registrarse en la composición
compartida o seguir la convención de registro del proyecto.

La implementación y el contrato se validan primero mediante `CompanyMcp.Stdio`. No iniciar
ni modificar `CompanyMcp.Http` durante este flujo. Si el host HTTP opcional existe, la
paridad se verifica después de que stdio pasa.

## Nombre, título y descripción

```csharp
[McpServerTool(
    Name = "work_items_list",
    Title = "List work items",
    ReadOnly = true,
    Destructive = false,
    Idempotent = true,
    OpenWorld = true,
    UseStructuredContent = true)]
[Description(
    "Lists work items for one project with id, title and state. " +
    "Use it for summaries; do not use it for full descriptions.")]
```

- `Name`: `snake_case`, específico del dominio.
- `Title`: legible para humanos.
- `Description`: explica qué devuelve, cuándo usar y cuándo no usar.
- Mantener la descripción breve: no repetir el nombre, los parámetros ni información que
  ya expresa el schema.
- Declarar hints con honestidad; no marcar una operación mutante como read-only.
- No fusionar operaciones sin relación en una mega-tool con un parámetro `action`; diseñar
  tools de workflow acotadas y diferenciables.

## Argumentos y JSON Schema

El SDK genera JSON Schema 2020-12 desde la firma y nullability:

```csharp
public static Task<Result> ListAsync(
    [Description("Project name, for example PAYMENTS.")] string project,
    IWorkItemService service,
    [Description("Maximum items from 1 to 100. Default: 20.")] int limit = 20,
    CancellationToken ct = default)
```

`IWorkItemService` y `CancellationToken` se resuelven y no aparecen en el schema.

Reglas:

- Parámetro requerido: no nullable y sin valor por defecto.
- Parámetro opcional: nullable o con valor por defecto explícito.
- Colocar servicios DI antes de los argumentos opcionales para respetar las reglas de C#.
- Usar parámetros planos cuando el contrato sea pequeño; DTOs complejos solo cuando mejoren
  de verdad la cohesión.
- No exponer parámetros que el servidor pueda inferir de identidad, configuración o contexto.
- Data annotations influyen en el schema, pero el SDK no garantiza su validación en runtime:
  validar límites, strings, enums y relaciones en el método/servicio.
- Tras actualizar el SDK, comparar el schema observado en `tools/list`.

## Resultados

Para datos tipados, usar `UseStructuredContent = true` y retornar records pequeños. Para
texto narrativo, retornar `string`. El SDK crea los content blocks MCP; no envolver a mano
en `content[0].text`.

- Retornar solo campos útiles para el modelo.
- Omitir metadata operativa, links, HTML y objetos downstream que no cambien la decisión.
- Paginar o aceptar `limit`, `summary`, `includeText` y `maxChars` para respuestas grandes.
- Usar TOON/CSV solo como representación textual de listados cuando una medición justifique
  el ahorro. No reemplazar structured content por un formato propietario sin necesidad.
- No devolver respuestas HTTP crudas de dependencias.

## Errores de tool

- Error de negocio que el modelo puede corregir: `McpException` con mensaje breve o
  `CallToolResult` con `IsError = true`.
- Cancelación: propagar `OperationCanceledException`/`CancellationToken`.
- Error inesperado: registrar internamente con trace ID y devolver mensaje genérico.
- No usar excepciones de protocolo para validaciones ordinarias de dominio.

## Safe writes

Toda tool mutante acepta `apply`, con default `false`:

```text
apply != true
  → preview=true, applied=false, plan de cambio

apply == true
  → revalidar autorización e inputs, ejecutar, applied=true
```

Operaciones destructivas requieren además `confirm: true`. Nunca exponer `write_file`,
`run_sql` o `execute_command` genéricas; exponer workflows acotados.

## Filesystem

Una tool con acceso a archivos debe:

- Resolver el path canónico bajo un root autorizado.
- Rechazar traversal y symlinks que escapen del root.
- Restringir extensiones y tamaño.
- Escanear secretos antes de escribir.
- Aplicar preview y confirmación según el riesgo.

Plantilla: [../examples/tool-template.cs](../examples/tool-template.cs).

Fuente primaria:

- <https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/tools/tools.md>
