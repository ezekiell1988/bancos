# ADR-01 — Campo opcional «de» para movimientos de otra persona

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

El dueño administra la plata de un familiar: recibe sus ingresos y paga sus gastos desde las mismas cuentas, y quiere comparar ingresos y gastos de esa persona.

## Decisión

Cada movimiento puede llevar un campo opcional `de` (p. ej. "papa"). bancos_movimientos y bancos_resumen filtran por `de` ("dueno" = sin `de`). Nuevos tools bancos_editar_movimiento (tipo, de, comercio, nota) y bancos_renombrar_tipo. Las reglas con montos para reconocer cada caso quedan en datos/reglas.md, fuera de git.

## Razón

Separa de quién es el movimiento sin duplicar los tipos (comida, vivienda…) y sin tocar los movimientos viejos; los montos se mantienen fuera del repo público.

## Alternativas

* Tipos con prefijo (papa-comida, papa-vivienda): duplica el catálogo
* Buscar «papi» en comercio o nota: frágil, depende del detalle del banco

## Consecuencias

* Los movimientos sin `de` siguen siendo del dueño
* Para el gasto real del dueño usar de: dueno
