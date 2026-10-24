# 03 — Plan de Desarrollo

> Última actualización: 2026-10-23

## Dirección actual

`Bancos.Mcp` es el único proyecto activo. Un LLM realiza todo el trabajo mediante tools MCP; no existen API HTTP ni interfaz web en la arquitectura activa.

La fase activa es la Fase 2 — Carga y cierres en MCP. Para cerrarla faltan tareas para las filas «Tools MCP de extracción por formato» e «Idempotencia e importación con huella».

## Fases

### Fase 1 — Fundación y contexto ✅ Completada

> Requisitos: ninguno (base /ia y MCP de soporte)

| Componente | Estado |
|---|---|
| `/ia` y reglas operativas | ✅ |
| MCP `iaWorkflow` y `dbquery` | ✅ |

### Fase 2 — Carga y cierres en MCP 🔄 En curso

> Avance: 2 de 4 componentes completados (actualizado 2026-09-25).
> Requisitos: REQ-001, REQ-002, REQ-003, REQ-004, REQ-009, REQ-010, REQ-012, REQ-013, REQ-014, REQ-019

| Componente | Estado |
|---|---|
| Catálogo y tablas transaccionales MCP | ✅ TASK-EBC-DB-07 |
| Tools MCP de extracción por formato | ⏳ En revisión |
| Idempotencia e importación con huella | ⏳ Pendiente |
| Cierres y períodos | ✅ TASK-EBC-MCP-31 |

### Fase 3 — Clasificación híbrida en MCP ⏳ Pendiente

> Avance: 0 de 4 componentes completados (actualizado 2026-09-25).
> Requisitos: REQ-005, REQ-006, REQ-018

| Componente | Estado |
|---|---|
| Categorías, reglas deterministas e historial | ⏳ Pendiente |
| Tools de clasificación y revisión manual aprendible | ⏳ Pendiente |
| Azure AI como fallback seguro | ⏳ Pendiente; riesgo alto |
| Estado `No clasificado` y cola de revisión | ⏳ Pendiente |

Orden obligatorio: reglas .NET → Azure AI → `No clasificado` → corrección del usuario → nueva regla .NET.

### Fase 4 — Contabilidad y reportes HTML en MCP ⏳ Pendiente

> Avance: 0 de 3 componentes completados (actualizado 2026-09-25).
> Requisitos: REQ-007, REQ-008, REQ-011, REQ-015, REQ-016, REQ-017

| Componente | Estado |
|---|---|
| Tools de libro mayor y diferencial cambiario | ⏳ Pendiente |
| Tool HTML de estado de resultados | ⏳ Pendiente |
| Tool HTML de situación financiera | ⏳ Pendiente |

### Fase 5 — Operación MCP autónoma ⏳ Pendiente

> Requisitos: REQ-020, REQ-021

El LLM podrá cargar archivos, ejecutar cierres, resolver clasificaciones, solicitar revisión humana para excepciones y generar reportes HTML autocontenidos.

### Fase 6 — Preparación Azure ⏳ Pendiente

> Requisitos: ninguno (autenticación y despliegue Azure están fuera del alcance inicial)

Aplicable solo tras una tarea de seguridad aprobada. Incluye autenticación del servidor MCP, secretos y plataforma de despliegue; no forma parte del alcance actual.

## Pendiente de planificación

* Ninguno por ahora.
