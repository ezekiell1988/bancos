# TASK-EBC-FE-01 — Revisión interactiva de cuentas de débito: saldos y clasificación de movimientos

**Estado:** Borrador
**Autor:** ezekiell1988 `<ezekiell1988@hotmail.com>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 20:34 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Revisión interactiva de cuentas de débito: saldos y clasificación de movimientos

## Contexto

El usuario quiere revisar todas las cuentas de débito activas, confirmar que cada una tenga el saldo correcto y que todos los movimientos estén clasificados en una categoría específica (sin dejar ninguno en "General"). La revisión es interactiva: se presenta la información y el usuario valida o corrige.

## Objetivo

Lograr que todas las cuentas de débito activas tengan saldo confirmado por el usuario y que ningún movimiento quede en la categoría "General".

## Alcance permitido

* Pantallas de cuentas de débito en el frontend Angular
* Endpoint de consulta de movimientos no clasificados
* UI de clasificación masiva de movimientos
* Filtro por categoría 'General' en listado de movimientos

## Fuera de alcance

* Importación de nuevos estados de cuenta
* Categorías de tarjetas de crédito
* Préstamos

## Criterios de aceptación

* [ ] Todas las cuentas de débito activas tienen saldo confirmado por el usuario
* [ ] Cero movimientos en categoría 'General' en cuentas de débito

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Consultar DB para obtener cuentas de débito activas con saldo actual vs saldo esperado
2. Construir vista/endpoint que liste movimientos con categoría 'General' agrupados por cuenta
3. Agregar UI para reclasificar movimientos uno a uno o en lote
4. Sesión interactiva con el usuario para revisar y aprobar cada cuenta

## Pasos

1. 1. Listar cuentas de débito activas y sus saldos actuales en DB
2. 2. Mostrar al usuario y confirmar/corregir saldos
3. 3. Listar todos los movimientos en categoría 'General' por cuenta
4. 4. Clasificar cada movimiento con el usuario hasta que la lista quede vacía
5. 5. Confirmar saldos finales post-clasificación

## Salida esperada

Cuentas de débito con saldos correctos y todos los movimientos clasificados en categorías específicas.

## Validación

* [ ] Query COUNT(*) WHERE categoria = 'General' retorna 0 para cuentas de débito
* [ ] El usuario aprueba explícitamente los saldos de cada cuenta

## Rollback

No aplica: es revisión de datos, sin cambios estructurales. Las reclasificaciones pueden revertirse manualmente.

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
