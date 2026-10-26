# 02 — Arquitectura del Sistema

> Última actualización: 2026-10-26
> Alcance: repo Bancos completo

## Resumen

Sin servidor: el agente lee los documentos que le pasa el dueño y, con el MCP local `bancos`, escribe jsonl en `datos/` (fuera de git) en 3 niveles (ADR-03): movimientos de los estados de cuenta, comprobantes y facturas, vinculados por id. Tras cada escritura el MCP reconstruye `datos/bancos.sqlite`, un índice de solo lectura (ADR-05). El skill `record-movements` dice cómo procesar cada documento.

## Mapa de componentes

| Componente | Responsabilidad | Ruta fuente | Responsable |
|------------|-----------------|-------------|-------------|
| Datos | Movimientos, comprobantes, facturas, tipos, respaldos e índice SQLite | `datos/` (fuera de git) | EBC |
| MCP `bancos` | Anotar los 3 niveles, vincularlos, resumir por rubro, conciliar, corregir, consultar SQL; copiar respaldos | `.mcp/bancos/` | EBC |
| Runtime compartido | Protocolo stdio, registry, resultados y arnés de smoke | `.mcp/_shared` → `~/.mcp/_shared` | EBC |
| Skill del proyecto | Procedimiento para leer documentos y anotarlos con el MCP | `.agents/skills/` | EBC |

## Flujo en tiempo de ejecución

```text
[Dueño en Telegram] -> HTML BCR / PDF BAC / foto de SINPE / factura en state/inbox
  -> [Agente + skill record-movements] lee el documento y elige el nivel
  -> [MCP bancos] preview -> apply (vincula detalles <-> movimiento por referencia o monto y fecha)
  -> datos/<bcr|bac|comprobantes|facturas>/AAAA-MM/AAAA-MM-DD.jsonl + datos/multimedia/AAAA-MM/<respaldo>
  -> datos/bancos.sqlite (reconstruida)
```

## Contratos de datos

| Contrato | Productor | Consumidor | Ubicación |
|----------|-----------|------------|-----------|
| Movimiento (nivel 1) `{id, fecha, fechaReal?, referencia?, clase, tipo, total, moneda, comercio, nota, de?, archivo}` | MCP `bancos` | Reportes, agente | `datos/<bcr\|bac>/AAAA-MM/AAAA-MM-DD.jsonl` |
| Comprobante (nivel 2) `{id, fecha, hora?, cuenta?, clase, total, moneda, comercio, referencia?, tipo?, de?, nota, archivo, movimiento}` | MCP `bancos` | Movimiento (completa tipo, de, nota) | `datos/comprobantes/AAAA-MM/AAAA-MM-DD.jsonl` |
| Factura (nivel 3) `{id, fecha, comercio, numero?, total, moneda, cuenta?, tipo?, de?, nota, archivo, movimiento, comprobantes?[], lineas[]}`; `comprobantes` = sus pagos, varios a varios (ADR-06) | MCP `bancos` | Movimientos de sus pagos, consultas | `datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl` |
| Índice SQLite (movimientos, comprobantes, facturas, factura_pagos, factura_lineas, tipos, vistas movimientos_detalle y facturas_saldo) | MCP `bancos` (reconstrucción) | `bancos_sql` | `datos/bancos.sqlite` |
| Tipo `{id, nombre}` | MCP `bancos` | MCP `bancos` | `datos/tipos-gasto.jsonl`, `datos/tipos-ingreso.jsonl` |
| Respaldo | MCP `bancos` (copia) | Movimiento (`archivo`, relativo a `datos/`) | `datos/multimedia/AAAA-MM/` |

## Configuración

| Parámetro | Propósito | Fuente de entorno |
|-----------|-----------|------------------|
| `--datos-dir` | Carpeta de datos del MCP (default `<repo>/datos`; las pruebas usan una temporal) | Argumento del servidor |

## Validación

| Verificación | Comando o método |
|--------------|-----------------|
| Build | `node --check .mcp/bancos/server.mjs` |
| Smoke test | `node .mcp/bancos/tests/smoke.mjs` |

## Fases pendientes de revisión arquitectónica

<!-- El MCP iaWorkflow agrega aquí «* <fase> completada — pendiente de architecture.» al completarse una
fase de 03_plan.md (reemplaza el marcador de abajo). Resolver cada línea con resolve_phase_review. -->

## Revisiones arquitectónicas de fases
* Fase 3 — Skill para anotar documentos: not_applicable. Evidencia: La fase solo agregó el skill record-movements; no cambió componentes ni contratos (los cambios de arquitectura son de la Fase 5).
* Fase 5 — Contabilidad familiar en 3 niveles: updated. Evidencia: 02_architecture.md describe los 3 niveles, el vínculo por id, el índice SQLite derivado y los contratos de movimiento, comprobante, factura e índice (ADR-03, ADR-04, ADR-05). Referencias: 02_architecture.md, 06_decisions/ADR-03-tres-niveles-de-detalle-solo-el-movimiento-bancario-cuenta-en-los-reportes.md, 06_decisions/ADR-05-sqlite-como-i-ndice-derivado-de-los-jsonl.md.
* Fase 2 — MCP bancos: not_applicable. Evidencia: El MCP se construyó tal como lo describe 02_architecture.md (componentes, flujo, contratos y --datos-dir); no hubo cambios de arquitectura.

* Fase 1 — Workflow /ia funcional: updated. Evidencia: 02_architecture.md escrito desde cero en esta fase para el registro en jsonl: componentes (datos/, MCP bancos, runtime compartido, skill), flujo y contratos de datos. Referencias: 02_architecture.md.
