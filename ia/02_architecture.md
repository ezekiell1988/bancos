# 02 — Arquitectura del Sistema

> Última actualización: 2026-10-25
> Alcance: repo Bancos completo

## Resumen

Sin servidor ni base de datos: el agente lee los documentos que le pasa el dueño y, con el MCP local `bancos`, escribe archivos jsonl en `datos/` (fuera de git). Un skill del proyecto le dice cómo procesar cada tipo de documento.

## Mapa de componentes

| Componente | Responsabilidad | Ruta fuente | Responsable |
|------------|-----------------|-------------|-------------|
| Datos | Movimientos, catálogos de tipos y respaldos | `datos/` (fuera de git) | EBC |
| MCP `bancos` | Anotar, listar, resumir y borrar movimientos; agregar tipos; copiar respaldos | `.mcp/bancos/` | EBC |
| Runtime compartido | Protocolo stdio, registry, resultados y arnés de smoke | `.mcp/_shared` → `~/.mcp/_shared` | EBC |
| Skill del proyecto | Procedimiento para leer documentos y anotarlos con el MCP | `.agents/skills/` | EBC |

## Flujo en tiempo de ejecución

```text
[Dueño en Telegram] -> foto / PDF / Excel en state/inbox
  -> [Agente + skill del proyecto] lee el documento
  -> [MCP bancos] preview -> apply
  -> datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl + datos/multimedia/AAAA-MM/<respaldo>
```

## Contratos de datos

| Contrato | Productor | Consumidor | Ubicación |
|----------|-----------|------------|-----------|
| Movimiento `{fecha, clase, tipo, total, moneda, comercio, nota, archivo}` | MCP `bancos` | Agente, dueño | `datos/<bcr\|bac>/AAAA-MM/AAAA-MM-DD.jsonl` |
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
* Fase 2 — MCP bancos: not_applicable. Evidencia: El MCP se construyó tal como lo describe 02_architecture.md (componentes, flujo, contratos y --datos-dir); no hubo cambios de arquitectura.

* Fase 1 — Workflow /ia funcional: updated. Evidencia: 02_architecture.md escrito desde cero en esta fase para el registro en jsonl: componentes (datos/, MCP bancos, runtime compartido, skill), flujo y contratos de datos. Referencias: 02_architecture.md.
