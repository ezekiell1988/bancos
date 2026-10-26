# ADR-05 — SQLite como índice derivado de los jsonl

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

Con 3 niveles hacen falta consultas cruzadas (movimientos sin detalle, productos comprados, comprobantes pendientes) que son incómodas sobre jsonl sueltos. El MCP es Node sin dependencias.

## Decisión

Los jsonl siguen siendo la fuente de verdad. datos/bancos.sqlite se reconstruye completa con node:sqlite (integrado en Node 24) después de cada escritura del MCP y con bancos_sqlite_reconstruir; bancos_sql corre consultas de solo lectura. Nunca se escribe en la base directo.

## Razón

Son pocos cientos de filas al mes: reconstruir tarda milisegundos y evita que la base y los jsonl se desincronicen.

## Alternativas

* SQLite como fuente de verdad: pierde lo legible y fácil de corregir del jsonl.
* Solo jsonl: las consultas cruzadas quedan en código ad hoc.

## Consecuencias

* La base vive en datos/ (fuera de git) y se puede borrar sin perder nada.
* node:sqlite avisa que es experimental en stderr; no afecta al protocolo.
