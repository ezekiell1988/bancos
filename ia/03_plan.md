# 03 — Plan de Desarrollo

> Última actualización: 2026-07-26

## Dirección actual

`Bancos.Mcp` es el único proyecto activo. Un LLM realiza todo el trabajo mediante tools MCP; no existen API HTTP ni interfaz web en la arquitectura activa.

---

### Fase 1 — Fundación y contexto ✅ Completada

| Componente | Estado |
|---|---|
| `/ia` y reglas operativas | ✅ |
| MCP `iaWorkflow` y `dbquery` | ✅ |

---

### Fase 2 — Carga y cierres en MCP ⏳ En curso

| Componente | Estado |
|---|---|
| Catálogo y tablas transaccionales MCP | ✅ |
| Tools MCP de extracción por formato | ⏳ En revisión |
| Idempotencia e importación con huella | ⏳ Pendiente |
| Cierres y períodos | ✅ Implementado |

---

### Fase 3 — Clasificación híbrida en MCP ⏳

| Componente | Estado |
|---|---|
| Categorías, reglas deterministas e historial | ⏳ Pendiente |
| Tools de clasificación y revisión manual aprendible | ⏳ Pendiente |
| Azure AI como fallback seguro | ⏳ Pendiente; riesgo alto |
| Estado `No clasificado` y cola de revisión | ⏳ Pendiente |

Orden obligatorio: reglas .NET → Azure AI → `No clasificado` → corrección del usuario → nueva regla .NET.

---

### Fase 4 — Contabilidad y reportes HTML en MCP ⏳

| Componente | Estado |
|---|---|
| Tools de libro mayor y diferencial cambiario | ⏳ Pendiente |
| Tool HTML de estado de resultados | ⏳ Pendiente |
| Tool HTML de situación financiera | ⏳ Pendiente |

---

### Fase 5 — Operación MCP autónoma ⏳

El LLM podrá cargar archivos, ejecutar cierres, resolver clasificaciones, solicitar revisión humana para excepciones y generar reportes HTML autocontenidos.

### Fase 6 — Preparación Azure ⏳

Aplicable solo tras una tarea de seguridad aprobada. Incluye autenticación del servidor MCP, secretos y plataforma de despliegue; no forma parte del alcance actual.
