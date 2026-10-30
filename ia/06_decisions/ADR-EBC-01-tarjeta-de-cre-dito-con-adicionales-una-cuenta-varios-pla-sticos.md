# ADR-EBC-01 — Tarjeta de crédito con adicionales: una cuenta, varios plásticos

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-30

## Contexto

Una tarjeta BAC con plástico adicional sale en un solo estado de cuenta y se paga junta, pero cada plástico tiene su bloque. La tarjeta se anotaba solo como texto en la nota y las compras del adicional quedaban mezcladas con las del titular.

## Decisión

Los movimientos de `bac` llevan `tarjeta` (4 dígitos de la cuenta titular, la que se paga) y, si los hizo un adicional, `adicional` (4 dígitos de ese plástico). Intereses y comisiones son de la cuenta, sin adicional. bancos_resumen da `tarjetas`: lo de cada plástico y el total por cuenta; bancos_movimientos filtra por `tarjeta` y `plastico`; SQLite suma las columnas tarjeta, adicional y plastico. La tarjeta ya no se repite en la nota.

## Razón

Así funciona una tarjeta con adicionales: un saldo y un pago, con el consumo de cada plástico separado. Permite ver cuánto gastó cada persona sin partir el pago.

## Alternativas

* Tratar al adicional como otra tarjeta: rompe el pago único y el saldo de la cuenta
* Marcar al adicional con `de`: lo sacaría del hogar, y sigue siendo gasto del hogar

## Consecuencias

* Los movimientos BAC ya anotados se migraron con los bloques de cada plástico del PDF
* El nombre de cada plástico vive en datos/reglas.md (privado), no en el código
