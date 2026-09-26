# Mapa de área a skill — Bancos

Lo usa la regla [`gestion-tareas.md`](./gestion-tareas.md): antes de implementar una tarea, cargar el skill técnico de su área. El área es el prefijo del ID (`TASK-EBC-{ÁREA}-NN`; los IDs heredados `TASK-EZ-{ÁREA}-NN` usan la misma tabla). Skills propios del repo en `.agents/skills/` (espejos `.claude/`, `.codex/`): `bccr-sdde` y `llm-audit-db`; los demás son globales.

| Área | Alcance | Skill principal | Por dominio (cargar si la tarea lo toca) |
|------|---------|-----------------|------------------------------------------|
| `MCP` | Servidor `Bancos.Mcp` (.NET 10): tools, catálogo, protocolo stdio/HTTP, importaciones, cierres y reportes | `mcp-vscode-dotnet` | Código C#: `dotnet-10-csharp-14` · Tipo de cambio USD/CRC: `bccr-sdde` · Auditoría LLM: `llm-audit-db` |
| `BE` | Lógica .NET de `Bancos.Mcp.Tools`: features, jobs Hangfire, conciliación, diferencial cambiario | `dotnet-10-csharp-14` | Tipo de cambio BCCR: `bccr-sdde` · Diagnóstico con logs de IA: `llm-audit-dotnet` |
| `DB` | SQL Server propio de `Bancos.Mcp`: modelo EF, migraciones, consultas | `mssql-ef-best-practices` | `tbLlmAuditLog`: `llm-audit-db` · Consultas: MCP `dbQuery` |
| `INF` | Docker local (`.mcp/bancos-mcp.ps1`, `docker-compose.yml`), configuración del MCP en los clientes; Azure a futuro | `mcp-vscode-dotnet` | Publicar en Azure exige antes una tarea de seguridad aprobada (autenticación) |
| `QA` | Pruebas de `tests/` y validación del MCP (`tools/list`, flujos de importación y cierre) | `dotnet-10-csharp-14` | — |
| `DOC` | Contenido de `/ia`, reglas de `ia/rules/` y documentación | `ia-workflow` | — |
| `ARCH` | Decisiones de diseño (registrar con `ia_create_decision`) | `dotnet-10-csharp-14` | — |

Sin frontend activo: las tareas `FE` históricas corresponden a una interfaz retirada (hoy se opera solo por `Bancos.Mcp`). Si se reabre una interfaz web, mapear `FE` a `angular22` antes de implementar.

La clasificación excepcional con Azure AI usa el MCP global `misc` (su `docs/azure-ai-foundry.md`); no hay skill aparte.
