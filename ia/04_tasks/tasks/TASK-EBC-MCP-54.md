# TASK-EBC-MCP-54 — Ajustar db-query MCP para usar db.json

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-31 15:42 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Ajustar db-query MCP para usar db.json

## Contexto

El MCP db-query busca .local-secrets/sqlserver.json, pero la configuración local del proyecto usa .local-secrets/db.json. El servidor principal responde, pero db_exec falla al no encontrar el archivo esperado.

## Objetivo

Hacer que el perfil local de db-query lea .local-secrets/db.json y actualizar su documentación operativa para mantener el contrato consistente.

## Alcance permitido

* .mcp/db-query/src/profile.mjs
* .mcp/db-query/README.md
* .agents/skills/mcp-local-db-create/SKILL.md

## Fuera de alcance

* Leer, crear o modificar valores de secretos
* Cambiar el servidor Bancos MCP o la conexión de producción
* Cambiar esquemas o datos de SQL Server

## Criterios de aceptación

* [ ] El perfil db-query usa db.json como archivo de secretos local.
* [ ] La documentación del paquete y del skill menciona db.json de forma consistente.
* [ ] node --check server.mjs pasa.
* [ ] El smoke test del paquete pasa sin conectarse a SQL Server.

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `.mcp/db-query/src/profile.mjs`
* `.mcp/db-query/README.md`
* `.agents/skills/mcp-local-db-create/SKILL.md`

## Plan técnico

1. Cambiar únicamente secretsFile en el perfil local.
2. Actualizar referencias documentales al nombre del archivo, preservando las reglas de seguridad.
3. Ejecutar node --check y el smoke test.

## Pasos

1. Actualizar el perfil de db-query.
2. Actualizar README y skill.
3. Validar sintaxis y protocolo.

## Salida esperada

db-query puede localizar .local-secrets/db.json y mantiene el contrato documentado.

## Validación

* [ ] node --check .mcp/db-query/server.mjs
* [ ] node .mcp/db-query/tests/smoke.mjs

## Rollback

Restaurar secretsFile a sqlserver.json y revertir los cambios documentales.

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

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-31 15:42 CR.

No imprimir ni inspeccionar el contenido de .local-secrets/db.json.

## Issues vinculados

* ninguno
