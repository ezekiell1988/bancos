# ADR-04 — El papá es un rubro aparte que entra en los reportes por defecto

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

El papá del dueño vive en otra casa y depende económicamente de él. El dueño quiere analizar primero el hogar (él y Karen), poder quitar al papá fácilmente, y sacar un reporte solo del papá para mostrárselo. ADR-01 ya marca sus movimientos con de: "papa".

## Decisión

Sin de = hogar (dueño y Karen). bancos_resumen devuelve el hogar por tipo y cada de (papa, primo) como rubro con su propio desglose y balance; el total incluye todo. excluirDe: ["papa"] lo quita y de: "papa" deja solo lo suyo. Ningún filtro se aplica por defecto. "hogar" es el valor para filtrar lo que no tiene de ("dueno" queda como sinónimo).

## Razón

Ver al papá como un rubro deja claro cuánto cuesta sin mezclarlo con los tipos del hogar, y no esconde plata que sí sale de la cuenta.

## Alternativas

* Excluir al papá por defecto: el dueño pidió que no.
* Un tipo papa en el catálogo: se pierde en qué gasta el papá.

## Consecuencias

* Los tipos siguen siendo los mismos para todos; el rubro sale del campo de.
