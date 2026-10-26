# Mapa de área a skill — Bancos

Regla propia del proyecto. La usa [`gestion-tareas.md`](./gestion-tareas.md): antes de implementar una
tarea, cargar el skill técnico de su área. `init-ia.mjs --refresh-rules` nunca sobrescribe este archivo.

Este proyecto usa solo dos skills técnicos. Iniciales por defecto para los IDs de tarea: las de
`ia-workflow.json` (EBC).

| Área | Alcance | Skill(s) de implementación |
|------|---------|-----------------------------|
| `MCP` | Servidor MCP local `.mcp/bancos/` (tools para anotar, listar, resumir y corregir movimientos) | `mcp-vscode` |
| `DOC` | Skills del proyecto en `.agents/skills/` (cómo procesar documentos y facturas) | `create-skill` |
| `DOC` | `/ia`, `README.md` y formato de `datos/` | `ia-workflow` |
