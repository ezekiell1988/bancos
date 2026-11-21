# 05 — Progreso Actual

> Última actualización: 2026-11-21 12:01 CR
> Fase activa: ninguna

<!-- Con el MCP iaWorkflow, las dos líneas de arriba, «Completado en sesiones recientes» y la tabla de
«En progreso» las mantienen work_task, finish_task, close_issue e ia_add_progress_entry: no editarlas a
mano. A mano solo: Resumen ejecutivo, Pendiente y Riesgos y bloqueos. -->

## Resumen ejecutivo

Proyecto reiniciado como registro sencillo de movimientos en jsonl (`datos/`, fuera de git). `/ia` nueva creada con create-ia-workflow. Próxima prioridad: MCP `bancos` (TASK-EBC-MCP-01) y luego el skill para anotar documentos (TASK-EBC-DOC-04).

## Completado en sesiones recientes

* **2026-11-21** — TASK-EBC-FE-04: El resumen del panel muestra «Traspasos a tarjeta» y «Deuda de tarjeta» aparte de Gastos y balance.
* **2026-11-21** — TASK-EBC-MCP-12: El pago de tarjeta es traspaso: bancos_resumen ya no lo suma como gasto y agrega los bloques traspasos y deudaTarjeta; incluirTraspasos lo vuelve a sumar.
* **2026-11-21** — TASK-EBC-FE-03: Dashboard en 3 niveles: sección «Por categoría», pantalla de categoría con movimientos y pantalla de movimiento con partidas, facturas, líneas, ajustes e imagen. Atrás y Enviar al chat en cada nivel.
* **2026-11-21** — TASK-EBC-MCP-10: MCP bancos con el modelo de 3 niveles de ADR-EBC-03 (Bancos, Libros con la categoría en partidas, Cuentas por pagar con facturas y ajustes). Datos migrados con respaldo; totales por ciclo iguales antes y después. SQLite con libros, partidas, facturas y vistas; skill, README y arquitectura al día. Quedan 2 casos a revisar a mano (factura con 2 pagos y diferencia de ₡1,20).
* **2026-11-21** — TASK-EBC-MCP-04: Cerrada por reemplazo: ADR-06 quedó reemplazada por ADR-EBC-03. Lo que falte de facturas con pagos se absorbe en TASK-EBC-MCP-10 (nivel 2 con uno o varios nivel 3).
* **2026-11-21** — TASK-EBC-MCP-09: bancos_pendientes concilia solo lo que tiene documento (detallesSinMovimiento, facturasConSaldo) y lista sinCategoria; el dashboard sigue ese criterio; las facturas se vinculan hasta 35 días después con ±1; 4 facturas de servicios del ciclo 2026-11 vinculadas.
* **2026-11-21** — EBC: TASK-EBC-MCP-08 probada por el dueño de punta a punta: el enlace de 1 hora llegó por WhatsApp, el dashboard abrió y «Enviar al chat» devolvió la imagen al mismo WhatsApp. La falla previa era de configuración del WhatsApp del bot (no llegaban mensajes de Meta desde el 16/11), no del código.
* **2026-11-21** — TASK-EBC-MCP-08: Tool bancos_dashboard_enlace, destino WhatsApp en /enviar, números en config. Falta prueba real con WhatsApp (ventana de 24 h cerrada).
* **2026-11-21** — TASK-EBC-FE-02: Los montos del dashboard abren un detalle con tabla de movimientos, total, Enviar al chat y Atrás.
* **2026-11-19** — TASK-EBC-FE-01: Dashboard de finanzas (dashboard/) con pestaña «💰 Finanzas» declarada en la Mini App, reportes en imagen de 1080 px enviados al chat de Bancos, y servicio launchd.
* **2026-11-06** — TASK-EBC-MCP-07: Helper detalleSinUbicacion conserva la cuenta en comprobantes y facturas; tool bancos_fijar_cuenta; smoke cubre; 43 detalles completados con evidencia, 26 quedaron vacíos.
* **2026-11-02** — ISSUE-EBC-001 cerrado: Corregido con TASK-EBC-MCP-06: El arnés .mcp/_shared es un enlace a ~/.mcp/_shared, corregido allí (TASK-EBC-MCP-11 de ~/.mcp); smoke bancos termina al instante con TODO OK.
* **2026-11-02** — TASK-EBC-MCP-06: El arnés .mcp/_shared es un enlace a ~/.mcp/_shared, corregido allí (TASK-EBC-MCP-11 de ~/.mcp); smoke bancos termina al instante con TODO OK.
* **2026-11-01** — IA: MCP bancos: nueva cuenta `bncr` (tarjeta de crédito del Banco Nacional) en CUENTAS, descripciones, README y skill record-movements (cuotas de financiamiento BN con fecha de corte). Smoke test OK. Primer estado BN anotado (datos/ privado).
* **2026-10-31** — EBC: Tools de consulta del MCP bancos aceptan `ciclo` (19 al 18) y el skill record-movements lo fija como periodo por defecto; smoke test OK (72 checks).
* **2026-10-26** — TASK-EBC-DOC-04: Skill record-movements probado con documentos reales: 2 HTML de movimientos del BCR y 2 estados de la BAC (404 movimientos) más 15 comprobantes SINPE. Su versión por niveles quedó en TASK-EBC-DOC-06.
* **2026-10-26** — TASK-EBC-DOC-06: Skill record-movements reescrito por niveles (qué documento va a cada nivel y con qué tool), rubros hogar/papá, vínculos y pendientes, reportes (todo, sin papá, solo papá) y bancos_sql. datos/reglas.md ajustado a «hogar». 00_context y 02_architecture al día.
* **2026-10-26** — TASK-EBC-MCP-03: datos/ migrado con respaldo previo: 404 movimientos con id, 389 con referencia sacada del HTML BCR y PDF BAC (los 15 sin referencia son intereses y cuotas, que no la traen); 15 fotos de SINPE pasadas a comprobantes pendientes con su número de documento. El nivel 1 solo bajó lo de esas fotos (oct BCR). SQLite reconstruida.
* **2026-10-26** — TASK-EBC-MCP-02: MCP bancos 2.0: movimientos con id y referencia; comprobantes (nivel 2) y facturas (nivel 3) con vínculo automático y manual; resumen por rubro (hogar, papa) con excluirDe/de/excluirTipos; pendientes; SQLite derivado con bancos_sql. Smoke: 61 OK.
* **2026-10-26** — EBC: MCP bancos: `fecha` pasa a ser la fecha de proceso del banco y se agrega `fechaReal` de referencia; registrar reubica anotados con fecha provisional; editar acepta fechaReal. Datos migrados (419 movimientos) con respaldo en datos/respaldos/. Smoke OK.
* **2026-10-25** — TASK-EBC-MCP-01: MCP bancos con 6 tools (tipos, agregar tipo, registrar con respaldo y sin duplicados, movimientos, resumen, borrar), smoke 24/24 OK sobre carpeta temporal y registrado en Claude Code, VS Code y Codex.
* **2026-10-25** — TASK-EBC-DOC-03: /ia completada para el registro sencillo en jsonl: contexto, 6 requisitos, arquitectura, fases 2 (MCP bancos) y 3 (skill), progreso por componente; reglas de CLAUDE.md/AGENTS.md pasadas a 00_context y esos archivos retirados.
* **2026-10-25** — TASK-EBC-DOC-02: Mapa de área a skill: MCP → mcp-vscode, SKILL → create-skill, DOC → ia-workflow.
* **2026-10-25** — TASK-EBC-DOC-01: Checklist de revisión propio: privacidad del repo público, formato de datos/, contrato del MCP bancos (mcp-vscode), skills (create-skill) y trazabilidad.

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| — | — | — | — |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| Alta | TASK-EBC-MCP-01 MCP bancos | Sin él los movimientos se anotan a mano |
| Media | TASK-EBC-DOC-04 Skill para anotar documentos | Depende del MCP |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Repo público con datos financieros | Alto | `datos/` y `.local-secrets/` en `.gitignore`; checklist de revisión |
