# TASK-EBC-BE-04 — Lector de financiamientos BAC en XLS binario

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Lector de financiamientos BAC en XLS binario

## Contexto

El detector ya reconoce bac-credit-financing-xls-v1 mediante contenido XLS BIFF/OLE, pero no existe extractor ni persistencia de sus saldos y cuotas.

## Objetivo

Extraer y validar financiamientos BAC desde XLS binario usando una firma de contenido y datos normalizados.

## Alcance permitido

* Implementar el extractor bac-credit-financing-xls-v1.
* Usar ExcelDataReader para XLS binario.
* Persistir datos estructurados de financiamiento asociados al auxiliar.
* Crear fixtures anonimizados y pruebas.

## Fuera de alcance

* Interpretar compras de tarjeta como movimientos contables.
* Procesar otros formatos BAC.
* Modificar muestras reales.

## Criterios de aceptación

* [ ] Un fixture XLS anonimizado detecta la plantilla correcta.
* [ ] Se extraen fecha, concepto, cuotas, monto, saldo inicial y saldo faltante.
* [ ] Datos inválidos dejan la importación auditada como fallida.
* [ ] Reintentos son idempotentes.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Parsing`
* `src/Bancos.Api/Features/Imports`
* `src/Bancos.Api/Data`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Definir entidad o extensión de auxiliar para financiamiento.
2. Implementar extractor y validación.
3. Integrar al job.
4. Agregar migración y pruebas.

## Pasos

1. Modelar datos.
2. Implementar extractor.
3. Integrar job.
4. Validar.

## Salida esperada

Importación de financiamientos BAC XLS completa y verificable.

## Validación

* [ ] dotnet test
* [ ] dotnet build
* [ ] Pruebas de detección y validación XLS

## Rollback

Deshabilitar el extractor BAC sin eliminar las importaciones auditadas.

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

Trabajar directamente en main durante construcción.

## Issues vinculados

* ninguno
