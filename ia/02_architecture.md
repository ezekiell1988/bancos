# 02 — Arquitectura del Sistema

> Última actualización: 2026-11-21
> Alcance: repo Bancos completo

## Resumen

Sin servidor: el agente lee los documentos que le pasa el dueño y, con el MCP local `bancos`, escribe jsonl en `datos/` (fuera de git) en 3 niveles contables (ADR-EBC-03, que reemplaza a ADR-06): Bancos (movimientos de los estados de cuenta, la base que suma), Libros (el pago 1 a 1 con un movimiento, con la categoría en partidas) y Cuentas por pagar (facturas y ajustes que cuelgan de un libro), vinculados por id. Tras cada escritura el MCP reconstruye `datos/bancos.sqlite`, un índice de solo lectura (ADR-05). El skill `record-movements` dice cómo procesar cada documento.

## Mapa de componentes

| Componente | Responsabilidad | Ruta fuente | Responsable |
|------------|-----------------|-------------|-------------|
| Datos | Movimientos, libros, facturas, tipos, respaldos e índice SQLite | `datos/` (fuera de git) | EBC |
| MCP `bancos` | Anotar los 3 niveles, vincularlos, resumir por categoría y rubro, conciliar, corregir, consultar SQL, migrar al modelo de libros; copiar respaldos | `.mcp/bancos/` | EBC |
| Dashboard de finanzas | Servidor Node en 127.0.0.1:4780 (launchd `com.ezekl.bancos-dashboard`), reusa los handlers `bancos_*`; abre por el proxy `/svc/` de la Mini App (`miniapp.tabs` id `finanzas`); `enviar` genera un PNG de 1080 px con Chrome y lo manda por `bridge.sock` (op `send_photo`) al chat de Bancos | `dashboard/` | EBC |
| Runtime compartido | Protocolo stdio, registry, resultados y arnés de smoke | `.mcp/_shared` → `~/.mcp/_shared` | EBC |
| Skill del proyecto | Procedimiento para leer documentos y anotarlos con el MCP | `.agents/skills/` | EBC |

## Flujo en tiempo de ejecución

```text
[Dueño en Telegram] -> HTML BCR / PDF BAC / foto de SINPE / factura en state/inbox
  -> [Agente + skill record-movements] lee el documento y elige el nivel
  -> [MCP bancos] preview -> apply (cada movimiento queda con su libro; vincula pagos <-> movimiento por referencia o monto y fecha; facturas cuelgan de un libro)
  -> datos/<bcr|bac|bncr|libros|facturas>/AAAA-MM/AAAA-MM-DD.jsonl + datos/multimedia/AAAA-MM/<respaldo>
  -> datos/bancos.sqlite (reconstruida)
```

## Contratos de datos

| Contrato | Productor | Consumidor | Ubicación |
|----------|-----------|------------|-----------|
| Movimiento (nivel 1, Bancos) `{id, fecha, fechaReal?, referencia?, clase, total, moneda, comercio, nota, de?, tarjeta?, adicional?, archivo}`; ya no guarda `tipo`: al leer se agregan `libro`, `partidas` y `tipo` (la principal) | MCP `bancos` | Reportes, agente | `datos/<bcr\|bac\|bncr>/AAAA-MM/AAAA-MM-DD.jsonl` |
| Libro (nivel 2) `{id, fecha, hora?, cuenta?, clase, total, moneda, comercio, referencia?, de?, nota, documento?, movimiento, partidas[{tipo, monto, de?, nota?}], transito?{razon}}`; 1 a 1 con un movimiento (`null` = por conciliar); partidas suman el total | MCP `bancos` | Resumen por categoría, pendientes | `datos/libros/AAAA-MM/AAAA-MM-DD.jsonl` |
| Factura (nivel 3) `{id, fecha, proveedor, cedula?, numero?, clase factura\|ajuste, total, moneda, cuenta?, de?, nota, archivo, libro, lineas[{descripcion, cantidad, unidad, precio, impuesto?, total, tipo}], ajustes?[]}`; siempre cuelga de un libro; la suma de las de un libro = su monto y sus partidas = líneas por categoría | MCP `bancos` | Dashboard (ciclo → categoría → movimiento → factura), consultas | `datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl` |
| Índice SQLite (movimientos con `ciclo` y `libro_id`, libros, libro_partidas, facturas, factura_lineas, factura_ajustes, tipos; vistas movimiento_categorias, movimientos_detalle, facturas_detalle, libros_cuadre) | MCP `bancos` (reconstrucción) | `bancos_sql`, dashboard | `datos/bancos.sqlite` |
| Tipo `{id, nombre}` | MCP `bancos` | MCP `bancos` | `datos/tipos-gasto.jsonl`, `datos/tipos-ingreso.jsonl` |
| Respaldo | MCP `bancos` (copia) | Movimiento (`archivo`), libro (`documento`) y factura (`archivo`), relativos a `datos/` | `datos/multimedia/AAAA-MM/` |

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
* Fase 6 — Facturas con varios pagos completada — pendiente de architecture.

## Revisiones arquitectónicas de fases
* Fase 3 — Skill para anotar documentos: not_applicable. Evidencia: La fase solo agregó el skill record-movements; no cambió componentes ni contratos (los cambios de arquitectura son de la Fase 5).
* Fase 5 — Contabilidad familiar en 3 niveles: updated. Evidencia: 02_architecture.md describe los 3 niveles, el vínculo por id, el índice SQLite derivado y los contratos de movimiento, comprobante, factura e índice (ADR-03, ADR-04, ADR-05). Referencias: 02_architecture.md, 06_decisions/ADR-03-tres-niveles-de-detalle-solo-el-movimiento-bancario-cuenta-en-los-reportes.md, 06_decisions/ADR-05-sqlite-como-i-ndice-derivado-de-los-jsonl.md.
* Fase 2 — MCP bancos: not_applicable. Evidencia: El MCP se construyó tal como lo describe 02_architecture.md (componentes, flujo, contratos y --datos-dir); no hubo cambios de arquitectura.

* Fase 1 — Workflow /ia funcional: updated. Evidencia: 02_architecture.md escrito desde cero en esta fase para el registro en jsonl: componentes (datos/, MCP bancos, runtime compartido, skill), flujo y contratos de datos. Referencias: 02_architecture.md.
