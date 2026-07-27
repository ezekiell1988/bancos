# ISSUE-008 — Pruebas MCP desactualizadas y validación de sesión incompleta

**Severidad:** medium
**Estado:** abierto
**Componente:** quality
**Detectado:** 2026-07-27 09:42 CR
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`

---

## Síntoma

La suite de Bancos.Mcp deja tres pruebas fallidas: el conteo de relaciones plantilla-cuenta espera 40 aunque el catálogo actual genera 41; tools/list y tools/call aceptan sesiones o versiones de protocolo inválidas con HTTP 200.

## Causa raíz

La expectativa de McpCatalogDbContextTests no se actualizó al incorporar las relaciones de plantillas sentinel de BN. McpHandler negocia la versión durante initialize, pero no valida la sesión ni el header de versión antes de procesar solicitudes posteriores.

## Workaround

Ejecutar las pruebas específicas de la feature afectada mientras se corrige la suite MCP completa.

## Fix propuesto

Actualizar la expectativa de relaciones de plantillas al catálogo vigente y exigir una sesión existente con versión de protocolo coincidente para tools/list y tools/call, devolviendo HTTP 400 ante solicitudes inválidas.

## Tareas vinculadas

* TASK-EBC-BE-29
