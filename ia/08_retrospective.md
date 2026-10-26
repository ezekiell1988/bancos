# 08 — Retrospectiva

## Fases pendientes de retrospectiva

<!-- El MCP iaWorkflow agrega aquí «* <fase> completada — pendiente de retrospective.» al completarse una
fase de 03_plan.md (reemplaza el marcador de abajo). Documentar la retrospectiva y resolver cada línea
con resolve_phase_review. -->

## Revisiones de retrospectiva de fases
* Fase 3 — Skill para anotar documentos: recorded. Evidencia: El skill se probó con los estados reales de BCR y BAC; en el uso aparecieron la regla de fecha del banco (ADR-02), el rubro del papá (ADR-01/04) y la necesidad de separar comprobantes y facturas (ADR-03).
* Fase 5 — Contabilidad familiar en 3 niveles: recorded. Evidencia: Salió bien: las fotos de SINPE del BCR traen el mismo «Documento» que el HTML, así que el vínculo por referencia es exacto; la migración dejó 389 de 404 movimientos con referencia y solo bajó lo de las fotos. A mejorar: los intereses y cuotas de la BAC no traen referencia y dependen de monto y fecha; la sesión de Claude necesita reconectar el MCP para ver los tools nuevos.
* Fase 2 — MCP bancos: recorded. Evidencia: El enlace .mcp/_shared a ~/.mcp/_shared evita copiar el runtime; el smoke con --datos-dir temporal permite probar escrituras reales sin tocar datos/.

* Fase 1 — Workflow /ia funcional: recorded. Evidencia: Lección: el MCP iaWorkflow solo acepta áreas FE, BE, HF, DB, INF, DOC, MCP, ARCH, QA, CAP; los skills del proyecto se mapearon al área DOC en rules/mapa-area-skill.md.
