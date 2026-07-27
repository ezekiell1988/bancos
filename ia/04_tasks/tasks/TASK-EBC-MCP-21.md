# TASK-EBC-MCP-21 — Habilitar y verificar MCP Bancos nativo en Codex, VS Code y Claude

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 21:44 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Habilitar y verificar MCP Bancos nativo en Codex, VS Code y Claude

## Contexto

Se requiere revisar y habilitar los servidores MCP locales Bancos para los tres clientes.

## Objetivo

Registrar configuraciones nativas y verificar que los clientes puedan descubrir los servidores MCP sin exponer secretos.

## Alcance permitido

* .vscode/mcp.json
* .mcp.json
* configuración local de Codex
* configuración local de Claude
* verificación de los servidores .mcp

## Fuera de alcance

* Cambios a herramientas MCP
* Cambios a base de datos
* Exposición de secretos

## Criterios de aceptación

* [ ] VS Code declara ambos servidores
* [ ] Codex declara ambos servidores
* [ ] Claude declara ambos servidores
* [ ] Los smoke tests pasan

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `.vscode/mcp.json`
* `.mcp.json`

## Plan técnico

1. Reutilizar .mcp/ia-workflow y .mcp/db-query con transporte stdio
2. Usar rutas absolutas solo para configuración local de Codex y rutas de proyecto para VS Code/Claude

## Pasos

1. Inspeccionar registros existentes
2. Ajustar registros nativos faltantes
3. Ejecutar smoke tests y validación de configuración

## Salida esperada

Configuraciones de cliente correctas y smoke tests de los servidores MCP.

## Validación

* [ ] JSON válido
* [ ] node --check
* [ ] smoke tests

## Rollback

Restaurar las entradas agregadas de configuración.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: Se verificó que VS Code y Claude Code ya registran bancosMcp. Se añadió bancosMcp al registro global nativo de Codex en ~/.codex/config.toml con transporte streamable HTTP hacia localhost:8000/mcp. Codex descubre la herramienta health_status, pero su ejecución fue rechazada por límite de tasa HTTP 429 del servidor local.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-26 21:44 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
