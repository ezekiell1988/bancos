# 06 — Checklist de auditoría y diagnóstico rápido

## Checklist de auditoría

Marcar como **importante** cuando:

### Protocolo y arranque

- [ ] El servidor usa `Content-Length` framing → VS Code espera `initialize` sin respuesta.
- [ ] `initialize` hace echo ciego de `protocolVersion`.
- [ ] Hay `console.log` de debug hacia stdout (corrompe el stream MCP).

### Estructura

- [ ] Servidor en carpeta ambigua (`tools/` en la raíz del repo) cuando el proyecto ya usa `.mcp/`.
- [ ] Tools registrados a mano en `HANDLERS`/`TOON_TOOLS`/`definitions.mjs` en lugar de
      autodescubiertos desde `tools/` → cada tool nuevo toca 4 archivos (migrar: ver
      [02-estructura.md](02-estructura.md)).
- [ ] `name` de un tool no coincide con su nombre de archivo.
- [ ] Helpers compartidos dentro de `tools/` sin prefijo `_` (el registry los intenta cargar como tools).
- [ ] El servidor lee fuera del root del proyecto sin razón documentada.

### Diseño de tools

- [ ] Las write tools mutan por defecto (sin `apply: true`).
- [ ] Existe una tool genérica `write_file`.
- [ ] Solo se exponen tools de bajo nivel sin capa de vocabulario pública.
- [ ] No hay approval gate para operaciones destructivas (`confirm: true`).
- [ ] Sin modo compacto de lectura (alto consumo de tokens).
- [ ] Falta `additionalProperties: false` en algún `inputSchema`.
- [ ] Listados grandes sin `format: 'toon'`.

### Tests y config

- [ ] No existe `tests/smoke.mjs` con las verificaciones mínimas.
- [ ] Tools sin `smoke()` co-ubicado (el runner solo puede verificar que aparecen en el catálogo).
- [ ] `.vscode/mcp.json` ignorado sin excepción `!.vscode/mcp.json` — el equipo no puede
      compartir la config.
- [ ] Config examples con rutas absolutas del desarrollador sin placeholders.
- [ ] Secretos hardcodeados en `mcp.json` en lugar de `inputs`.

## Diagnóstico rápido

| Síntoma | Causa probable | Fix |
|---------|---------------|-----|
| VS Code espera `initialize` sin timeout | `Content-Length` framing en stdout | Cambiar a newline-delimited JSON-RPC |
| Server no arranca; stderr dice `tools/{archivo}: ...` | El archivo viola el contrato del registry | Corregir lo que indica el mensaje (name ≠ archivo, falta additionalProperties, etc.) |
| Tool no aparece en `tools/list` | Archivo con prefijo `_`, extensión distinta a `.mjs`, o fuera de `tools/` | Renombrar/mover el archivo |
| Server no arranca (sin mensaje del registry) | Error de sintaxis o import roto | `node --check server.mjs` y `node --check tools/{archivo}.mjs` |
| Tool aparece pero VS Code no ve el cambio | `dev.watch` no cubre `tools/**` | Ajustar el glob a `.mcp/mi-servidor/**/*.mjs` |
| Claude Code no ve el server/tool nuevo | `.mcp.json` cargado al inicio de sesión | Reiniciar sesión de Claude Code |
| Codex no muestra tool nativa | Sesión anterior al cambio de config | Abrir nueva sesión |
| Respuesta de listado gigante en tokens | Falta `format: 'toon'` o modo compacto | Declarar `format: 'toon'` / agregar `pathsOnly`/`summary` |
| Write tool muta sin `apply: true` | Falta la guardia de preview | Agregar patrón safe write ([03-agregar-tool.md](03-agregar-tool.md)) |
| Secrets detectados → escritura rechazada | Contenido contiene patrón de secreto | Limpiar el valor antes de pasarlo al MCP |
| `az devops` falla con credenciales | Se pasó `--organization` explícito | Omitir el flag; usar `ensureOrgConfigured` |
