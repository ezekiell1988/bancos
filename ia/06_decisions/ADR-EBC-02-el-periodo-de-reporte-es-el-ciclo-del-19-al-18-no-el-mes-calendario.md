# ADR-EBC-02 — El periodo de reporte es el ciclo del 19 al 18, no el mes calendario

**Estado:** aceptada
**Dominio:** reportes
**Fecha:** 2026-10-31

## Contexto

El dueño lleva las cuentas por ciclos del 19 de un mes al 18 del siguiente (corte de la tarjeta BAC). Los tools de consulta solo aceptaban mes calendario, lo que causaba enredos (p. ej. decir que un salario cuenta para «octubre» o «noviembre»).

## Decisión

Los tools de consulta (resumen, movimientos, detalles, pendientes) aceptan `ciclo: AAAA-MM` = del 19 del mes anterior al 18 de ese mes (ciclo 2026-11 = 2026-10-19 a 2026-11-18), y es el periodo por defecto para todo reporte. `mes` calendario solo si el dueño lo pide. Al hablar con el dueño, el ciclo se nombra por sus fechas, nunca solo por un nombre de mes.

## Razón

El dueño lo pidió de forma explícita y coincide con el corte de la BAC; nombrar por fechas evita ambigüedad.

## Alternativas

* Cambiar el significado de `mes` a ciclo (descartado: rompe pruebas y sigue siendo ambiguo)
* Nombrar el ciclo por el mes en que empieza (descartado: el estado de la BAC se nombra por su corte del 18)

## Consecuencias

* El ciclo se nombra por el mes donde termina (su 18).
* Un ingreso que se adelanta o atrasa unos días cae en el mismo ciclo si no cruza el 18/19.
