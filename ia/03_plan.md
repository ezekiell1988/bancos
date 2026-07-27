# 03 — Plan de Desarrollo

> Última actualización: 2026-07-26

## Dirección actual

Bancos.Mcp es el único proyecto activo y concentra las operaciones mediante tools MCP.

---

### Fase 1 — Fundación y contexto ✅ Completada

| Componente | Estado |
|---|---|
| `/ia` y reglas operativas | ✅ |
| Inspección segura de formatos semilla | ✅ |
| MCP `iaWorkflow` y `dbquery` | ✅ |

---

### Fase 2 — Catálogo y parsers en MCP ⏳ En curso

Equivalente funcional de `Imports` + `Parsing` de la API, implementado como tools MCP.

| Componente | Estado |
|---|---|
| Tablas catálogo (`tbBanks`, `tbBankAccounts`, `tbImportTemplates`) | ✅ |
| Tablas transaccionales (`tbTransactions`, `tbCardStatements`, `tbLoanStatements`) | ✅ |
| Tools MCP de extracción por formato (BAC, BCR, BN, Coopealianza) | ⏳ En revisión |
| Idempotencia e importación con huella en MCP | ⏳ Pendiente |

---

### Fase 3 — Tipos de cambio, clasificación y movimientos en MCP ⏳

| Componente | Estado |
|---|---|
| Tool de tipos de cambio diarios | ⏳ Pendiente |
| Tools de clasificación (reglas, categorías, IA) | ⏳ Pendiente |
| Tools de consulta y gestión de movimientos | ⏳ Pendiente |

---

### Fase 4 — Contabilidad y reportes en MCP ⏳

| Componente | Estado |
|---|---|
| Tools de libro mayor (cuentas, auxiliares, comprobantes) | ⏳ Pendiente |
| Tool de diferencial cambiario (pasivos USD) | ⏳ Pendiente |
| Tools de P&G y situación financiera | ⏳ Pendiente |

---

### Fase 5 — Consolidación MCP ✅

| Componente | Estado |
|---|---|
| Retiro de proyectos API y frontend | ✅ |
| Limpieza de configuración de ejecución | ✅ |

---

### Fase 6 — Preparación Azure ⏳

Solo aplica después de eliminar la API.

| Componente | Estado |
|---|---|
| Autenticación | ⏳ Pendiente |
| Contenedor y secretos | ⏳ Pendiente |
