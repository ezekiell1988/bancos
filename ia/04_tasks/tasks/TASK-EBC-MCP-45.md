# TASK-EBC-MCP-45 — Aplicar patrones confirmados y actualizar pendientes

**Estado:** En revisión
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-27 10:55 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Aplicar patrones confirmados y actualizar pendientes

## Contexto

El usuario autorizó aplicar los 138 movimientos detectados por patrones de alta confianza.

## Objetivo

Confirmar las clasificaciones de tarjeta, préstamo, servicios, transporte y alimentación identificadas en la cola y regenerar el Markdown de pendientes.

## Alcance permitido

* Confirmaciones de clasificación para los patrones aprobados
* docs/movimientos-pendientes-clasificacion.md

## Fuera de alcance

* Clasificar transferencias internas
* Crear categorías nuevas
* Clasificar movimientos fuera de los patrones aprobados

## Criterios de aceptación

* [ ] Se aplican únicamente los cinco patrones autorizados.
* [ ] El documento contiene solo movimientos pendientes vigentes.
* [ ] No se incluyen IDs internos en el documento.

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `docs/movimientos-pendientes-clasificacion.md`

## Plan técnico

1. Usar categorías existentes: Tarjetas de crédito, Préstamos, Servicios, Transporte y Alimentación.
2. Mantener intactas las transferencias internas.

## Pasos

1. Recuperar el listado vigente.
2. Confirmar cada movimiento que coincida con uno de los patrones aprobados.
3. Consultar el total posterior.
4. Regenerar el Markdown de pendientes.

## Salida esperada

Movimientos de los cinco patrones confirmados y Markdown reducido a los pendientes restantes.

## Validación

* [ ] Comparar conteo antes y después.
* [ ] Contar filas del Markdown actualizado.
* [ ] Verificar ausencia de IDs internos.

## Rollback

Las reglas deterministas se deben corregir o eliminar explícitamente si una clasificación resultara incorrecta.

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

* Pendiente de revisión: Se confirmaron 80 de los 138 movimientos autorizados y se actualizó el listado a 410 pendientes. El servidor limitó temporalmente las 58 confirmaciones restantes.

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-27 10:55 CR.

Los patrones autorizados suman hasta 138 movimientos según la consulta previa.

## Issues vinculados

* ninguno
