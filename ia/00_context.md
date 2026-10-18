# 00 — Contexto del Proyecto

> Última actualización: 2026-10-17
> Alcance activo: Bancos.Mcp es el único proyecto funcional.

## Identidad

| Campo | Valor |
|---|---|
| Nombre | Bancos |
| Propósito | Generar estados mensuales de pérdidas y ganancias y situación financiera para Familia Baltodano Soto. |
| Usuarios | Ezequiel Baltodano y Karen Soto. |
| Despliegue actual | Local, sin autenticación. |
| Despliegue futuro | Contenedor Azure; autenticación pendiente antes de exponerlo. |
| Dirección estratégica | Operar exclusivamente mediante `Bancos.Mcp`. |

## Stack decidido

| Capa | Tecnología |
|---|---|
| MCP y jobs | .NET 10, C# 14, Minimal APIs por features |
| Datos | Microsoft SQL Server |
| Procesos | Hangfire SQL Server y Hangfire.Console |
| Clasificación excepcional | Azure AI; solo descripción normalizada y catálogo de categorías |

## Proyectos y persistencia

| Proyecto | Responsabilidad | Estado |
|---|---|---|
| `Bancos.Mcp` | Servidor MCP para importaciones, cierres, conciliación y gestión financiera | **Único proyecto activo** |

`Bancos.Mcp` mantiene su propio contexto, migraciones y base de datos.

## Límites y constantes críticas

* Datos financieros y credenciales son sensibles: nunca copiarlos a `/ia`, logs o prompts de IA.
* La moneda funcional es CRC. USD conserva importe original, importe convertido y tipo de cambio diario único.
* Activos y pasivos USD generan diferencial cambiario en alcance inicial.
* Archivos importados se descartan después de extracción exitosa; persisten datos estructurados y huellas de importación.
* No hay autenticación local. Azure exige una tarea de seguridad aprobada antes de publicar.

## Mapa

| Ruta | Propósito |
|---|---|
| `src/input/` | Muestra histórica inicial; no se modifica durante análisis. |
| `src/output/` | Aplicación final compilada/servida. |
| `.local-secrets/` | Configuración local ignorada; nunca versionar ni leer valores en respuestas. |
| `.mcp/db-up.ps1` | Levanta SQL Server (Docker) si no corre y aplica las migraciones de `Bancos.Mcp.Tools`. |
| `.mcp.json` | Registra `bancosMcpStdio`: el cliente MCP arranca `src/Bancos.Mcp.Stdio` por stdio. |
| `ia/` | Contexto y trazabilidad de trabajo. |

## Desarrollo local

### Levantar BD y MCP

* BD: `pwsh .mcp/db-up.ps1` arranca el contenedor de SQL Server (si no está corriendo) y aplica las migraciones.
* MCP: no se levanta a mano; el cliente (Claude Code, Codex, VS Code) arranca `bancosMcpStdio` desde `.mcp.json` por stdio.

**Regla operativa:** Ezequiel maneja el ciclo de vida de la BD y del servidor MCP. Si un cambio requiere levantar o reiniciar algo, el agente no lo hace por su cuenta: le pide al usuario que corra `pwsh .mcp/db-up.ps1` o que reconecte `bancosMcpStdio` desde su cliente (por ejemplo `/mcp` en Claude Code).

Requisitos: Docker Desktop corriendo y PowerShell (`pwsh`) instalado.

## Validación esperada

| Alcance | Comando |
|---|---|
| MCP | Con `bancosMcpStdio` conectado, `tools/list` responde con tools disponibles |
| Datos | Consultas de conciliación vía MCP `dbquery` cuando esté disponible |
