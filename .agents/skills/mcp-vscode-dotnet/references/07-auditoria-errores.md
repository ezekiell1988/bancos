# 07 — Auditoría y diagnóstico

## Checklist de auditoría

### Transporte stdio principal

- [ ] Usa `ModelContextProtocol` y `WithStdioServerTransport()`, no JSON-RPC manual.
- [ ] `.vscode/mcp.json` usa `type: "stdio"`, `command: "dotnet"` y el proyecto correcto.
- [ ] VS Code administra el lifecycle del child process.
- [ ] stdout contiene únicamente MCP; logs, diagnósticos y banners van a stderr.
- [ ] `WithToolsFromAssembly` apunta al assembly compartido `CompanyMcp.Tools`.
- [ ] Build, contract tests, smoke y debugging se realizan por stdio.
- [ ] No se requiere puerto, URL, `/health` ni `/mcp` para desarrollo local.

### Host HTTP — comprobar solo si se publica

- [ ] Usa `ModelContextProtocol.AspNetCore` y referencia el mismo catálogo compartido.
- [ ] `MapMcp` expone una ruta estable y la config remota coincide exactamente.
- [ ] El modo HTTP es stateless salvo requisito stateful documentado.
- [ ] No habilita SSE legacy por defecto.
- [ ] `AllowedHosts` no es `*`.
- [ ] CORS está ausente o restringido a orígenes concretos.
- [ ] HTTPS y autenticación se aplican antes del endpoint MCP.
- [ ] El catálogo HTTP tiene paridad con el catálogo stdio.

### Tools

- [ ] Cada tool vive en su feature folder y se descubre por assembly.
- [ ] Nombre `snake_case`, título legible y descripción específica.
- [ ] Todos los argumentos visibles tienen `[Description]`.
- [ ] Nullability/defaults reflejan required/opcional correctamente.
- [ ] Validación runtime explícita.
- [ ] Hints de seguridad coherentes con el efecto real.
- [ ] Write tools usan preview + `apply`; destructivas además `confirm`.
- [ ] Respuestas compactas, tipadas y sin payloads crudos.

### Operación

- [ ] Dependencias HTTP usan `IHttpClientFactory` y resiliencia estándar.
- [ ] Logs estructurados y redactados.
- [ ] Unit, contract y smoke tests pasan.
- [ ] Config del workspace no contiene secretos.
- [ ] Si existe publicación: `/health` está separado de `/mcp` y el estado de scale-out es externo.

### Eficiencia de tokens

- [ ] Cada tool representa un workflow acotado; no es CRUD microscópico ni una mega-tool genérica.
- [ ] No existen tools duplicadas o con descripciones solapadas.
- [ ] Nombres y descripciones son breves, específicos y suficientes para el routing.
- [ ] `inputSchema` contiene solo parámetros necesarios y evita DTOs profundamente anidados.
- [ ] Resultados retornan únicamente campos útiles, con `limit`, paginación o `maxChars` cuando aplica.
- [ ] Dominios separados en varios MCP pueden deshabilitarse realmente; no están todos activos siempre.
- [ ] Tool sets/configuración del agente habilitan solo las tools necesarias para cada workflow.

## Diagnóstico rápido

| Síntoma | Causa probable | Acción |
|---|---|---|
| VS Code no inicia | `command`, `args`, `cwd` o SDK .NET incorrectos | Copiar el comando, ejecutarlo en terminal y revisar `MCP: Show Output` |
| `initialize` no responde | stdout contaminado por logs/build/ANSI | Enviar logs a stderr; usar `--tl:off --verbosity quiet`; compilar sin warnings |
| El proceso termina al abrir | Build o DI falló antes del handshake | `dotnet build`, ejecutar smoke stdio y revisar stderr |
| Breakpoint no entra | Debugger no adjunto al child process | Iniciar MCP en VS Code y usar `.NET: Attach to Process` |
| Tools antiguas | Catálogo cacheado o proceso no reiniciado | Reiniciar stdio y ejecutar `MCP: Reset Cached Tools` |
| Tool no aparece | Falta atributo o se escanea el assembly del host | Verificar atributos y `CompanyMcpCatalog.Assembly` |
| Schema marca opcional como requerido | Firma sin default o nullability incorrecta | Corregir firma y observar `tools/list` por stdio |
| El modelo elige otra tool | Descripciones ambiguas/solapadas | Explicar cuándo usar y cuándo no usar cada una |
| Error genérico al llamar | Excepción no controlada o servicio DI ausente | Revisar stderr/trace ID; mapear error de negocio |
| HTTP opcional no conecta | ASP.NET no está corriendo o URL/ruta incorrecta | `curl /health`, revisar puerto/ruta y smoke HTTP |
| 404 en MCP | `MapMcp("/mcp")` no coincide con `url` | Alinear ambas rutas; no usar wildcard |
| 401/403 | Token ausente, issuer/audience/policy incorrectos | Probar auth por separado y revisar claims validados |
| Error de certificado | Certificado local/remoto no confiable | Confiar dev cert o corregir cadena TLS; no desactivar validación |
| Funciona local, falla tras scale-out | Estado en memoria/sesión | Externalizar estado y usar stateless |
| Primera llamada lenta | Cold start por `minReplicas: 0` en Container Apps | Esperado con escala a cero; subir `minReplicas` si el consumidor no tolera el cold start |

## Orden de depuración

```text
1. dotnet build
2. smoke stdio con cliente oficial
3. initialize + tools/list y schema observado
4. tools/call con argumentos mínimos
5. dependencia de dominio
6. integración stdio desde VS Code
7. attach debugger al proceso stdio, si aplica
8. GET /health + TLS + Host + auth, solo si existe HTTP
9. smoke HTTP y paridad del catálogo, solo si se publica
10. integración desde Copilot Studio, si aplica
```

No comenzar modificando versiones MCP o el body JSON-RPC: si el SDK oficial está
actualizado, primero aislar proceso, stdout/stderr, catálogo, schema y lógica de tool. Red,
ruta y auth se investigan después y únicamente para el host HTTP opcional.
