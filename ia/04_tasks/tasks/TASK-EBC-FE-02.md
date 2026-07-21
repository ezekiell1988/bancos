# TASK-EBC-FE-02 — Revisión interactiva de tarjetas de crédito: saldo al corte, gastos post-corte y ajuste contra cuentas por pagar

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

Revisión interactiva de tarjetas de crédito: saldo al corte, gastos post-corte y ajuste contra cuentas por pagar

## Contexto

El usuario quiere una vista de tarjetas de crédito que distinga claramente: (a) el saldo al corte del período cerrado y (b) los gastos realizados después del corte. Ambos montos deben reconciliarse contra las cuentas por pagar registradas en el sistema. La revisión es interactiva.

## Objetivo

Presentar por cada tarjeta de crédito el desglose: saldo al corte + gastos post-corte, y verificar que la suma cuadre contra las cuentas por pagar correspondientes.

## Alcance permitido

* Vista de tarjetas de crédito en Angular
* Endpoint que separe movimientos pre y post corte
* Reconciliación visual contra cuentas por pagar
* Indicador de diferencia/ajuste pendiente

## Fuera de alcance

* Cuentas de débito
* Préstamos
* Importación de nuevos estados

## Criterios de aceptación

* [ ] Cada tarjeta muestra saldo al corte y gastos post-corte por separado
* [ ] La suma cuadra contra la cuenta por pagar o se documenta la diferencia
* [ ] El usuario aprueba la vista y los montos

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Identificar fecha de corte por tarjeta en DB
2. Calcular saldo al corte (movimientos <= fecha corte del período activo)
3. Calcular gastos post-corte (movimientos > fecha corte)
4. Obtener saldo de cuenta por pagar asociada a la tarjeta
5. Mostrar los tres valores y la diferencia; resaltar si no cuadra

## Pasos

1. 1. Consultar tarjetas de crédito activas y sus fechas de corte
2. 2. Calcular y mostrar saldo al corte por tarjeta
3. 3. Calcular y mostrar gastos post-corte por tarjeta
4. 4. Comparar totales contra cuentas por pagar
5. 5. Revisar diferencias con el usuario y registrar ajustes si aplica

## Salida esperada

Vista por tarjeta con tres columnas: saldo al corte / gastos post-corte / cuenta por pagar, con indicador de diferencia.

## Validación

* [ ] Para cada tarjeta: saldo_corte + gastos_post_corte = cuenta_por_pagar ± tolerancia acordada
* [ ] El usuario confirma que los montos corresponden a los estados reales

## Rollback

No aplica: es revisión y visualización, sin cambios estructurales.

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
