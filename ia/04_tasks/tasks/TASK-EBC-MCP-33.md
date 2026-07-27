# TASK-EBC-MCP-33 — Clasificación MCP con Azure AI como fallback seguro

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:47 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** pendiente

---

## Título

Clasificación MCP con Azure AI como fallback seguro

## Contexto

Cuando reglas .NET no resuelvan un movimiento, Bancos.Mcp debe poder solicitar una sugerencia a Azure AI sin enviar datos sensibles.

## Objetivo

Integrar Azure AI como fallback de clasificación con límite de datos, umbral de confianza y salida No clasificado.

## Alcance permitido

* src/Bancos.Mcp/Features/Classification/
* src/Bancos.Mcp/appsettings*.json
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Publicar recursos Azure.
* Enviar archivos, saldos, cuentas o credenciales al LLM.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Azure AI se invoca únicamente tras fallar reglas deterministas.
* [ ] El prompt contiene solo descripción normalizada y catálogo permitido.
* [ ] Baja confianza o error devuelve No clasificado.
* [ ] La configuración usa secretos locales ignorados.
* [ ] Existen pruebas con cliente simulado.

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Classification/`
* `src/Bancos.Mcp/appsettings.Development.example.json`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir contrato de cliente IA y filtro de datos.
2. Configurar fallback y umbral.
3. Auditar origen/confianza.
4. Probar con doble.

## Pasos

1. Aprobar seguridad.
2. Implementar cliente.
3. Probar.

## Salida esperada

Fallback Azure AI seguro y auditable.

## Validación

* [ ] Revisión de seguridad.
* [ ] dotnet test de Bancos.Mcp.

## Rollback

Deshabilitar proveedor Azure AI por configuración y revertir feature.

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

Sin notas adicionales.

## Issues vinculados

* ninguno
