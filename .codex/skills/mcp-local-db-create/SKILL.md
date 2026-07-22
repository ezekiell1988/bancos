---
name: mcp-local-db-create
description: >
   Crear, copiar, instalar y registrar el MCP SQL local .mcp/db-query en VS Code,
   Codex y Claude Code. Incluye consultas SQL seguras y validaciones Azure Sponsorship
   con aprobación explícita. Usar cuando se pida crear db-query, instalar un MCP SQL local,
   configurar dbQuery en mcp.json o config.toml, copiar el paquete a otro proyecto,
   preparar mssql o validar el smoke test. No aplica a MCP SQL de produccion aislado.
---

# MCP Local DB Create

Prepara el paquete portable `.mcp/db-query` para consultar SQL Server desde clientes
MCP locales. El paquete base contiene el runtime, `mssql`, `db_exec` y la validación
acotada `azure_sponsorship_validate`. `db-query-pro` es opcional, depende de
`db-query` y mantiene `requireApply:true`.

## Cuando Usarlo

* Crear o copiar `.mcp/db-query` a otro repositorio
* Instalar las dependencias npm del MCP SQL local
* Registrar `dbQuery` en VS Code, Codex o Claude Code
* Diagnosticar que el cliente no detecta `db_exec`
* Validar el acceso ARM de las suscripciones Azure Sponsorship configuradas
* Validar que el paquete no expone secretos ni reportes en Git

## Contrato Del Paquete

1. Copiar la carpeta completa `.mcp/db-query/`, incluidos `src/`, `tools/`, `tests/`,
   `package.json`, `package-lock.json` y `.gitignore`.
2. Crear `.local-secrets/sqlserver.json` en la raiz del proyecto con los campos
   `Server`, `Database`, `User` y `Password`. No incluir este archivo en Git, prompts,
   logs ni configuraciones MCP.
3. Ejecutar [examples/install-db-query.ps1](./examples/install-db-query.ps1) desde la
   raiz del proyecto. Instala unicamente `.mcp/db-query`; `db-query-pro` no lleva
   `package.json` ni `node_modules` propios.
4. Ejecutar `node .mcp/db-query/tests/smoke.mjs`. El smoke valida el protocolo y el
   catalogo sin conectarse a SQL Server.
5. Registrar el mismo entrypoint `server.mjs` en el cliente correspondiente. Agregar
   una entrada sin reemplazar los servidores MCP ya existentes.

## Herramientas

| Tool | Uso | Seguridad |
| --- | --- | --- |
| `db_exec` | Ejecuta T-SQL en EvistaDev y genera un reporte Markdown | Bloquea resultados con columnas sensibles |
| `azure_sponsorship_validate` | Valida token ARM y APIs Azure para suscripciones Sponsorship activas | Exige aprobación literal por llamada y nunca devuelve secretos ni tokens |

### Validación Sponsorship Con Aprobación

La herramienta lee `TenantId`, `ClientId` y `ClientSecret` solo en memoria para validar
cada fila activa de `[amgt].[SponsorshipSubscription]`. Devuelve únicamente metadatos
de la suscripción y códigos HTTP para token, ARM, UsageAggregates, RateCard y, si se
solicita, Cost Management.

Requiere ambos valores exactos en cada llamada:

```json
{
   "approval": "I_APPROVE_SENSITIVE_CREDENTIAL_USE",
   "purpose": "validate_azure_sponsorship_access"
}
```

La aprobación no habilita lecturas SQL genéricas de secretos ni revela valores en la
respuesta, los logs o los reportes. Solo autoriza esa validación de Azure durante la
ejecución de la llamada.

## Referencias Por Cliente

| Cliente | Archivo | Configuracion |
| --- | --- | --- |
| VS Code / GitHub Copilot | [references/vscode.md](./references/vscode.md) | `.vscode/mcp.json` |
| Codex | [references/codex.md](./references/codex.md) | `~/.codex/config.toml` o `.codex/config.toml` |
| Claude Code | [references/claude.md](./references/claude.md) | `.mcp.json` |

## Verificacion

```powershell
node --check .mcp/db-query/server.mjs
node .mcp/db-query/tests/smoke.mjs
```

Despues de editar configuracion de Codex o Claude Code, abrir una sesion nueva. En
VS Code, reiniciar el servidor MCP desde la vista de MCP o recargar la ventana.

## Seguridad

* Mantener `.local-secrets/` ignorado en el `.gitignore` raiz.
* Conservar el `.gitignore` incluido en `db-query`: excluye `node_modules/`, `reports/`,
  `.env*` y logs locales al copiar el paquete.
* No declarar credenciales en `.vscode/mcp.json`, `.mcp.json` ni `config.toml`.
* Usar `db-query-pro` solo junto al paquete base. Sus mutaciones requieren `apply:true`.