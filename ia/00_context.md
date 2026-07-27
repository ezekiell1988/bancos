# 00 — Contexto del Proyecto

> Última actualización: 2026-07-26
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
| `.mcp/bancos-mcp.ps1` | Script que levanta SQL Server (Docker) y el servidor MCP en modo watch. |
| `ia/` | Contexto y trazabilidad de trabajo. |

## Desarrollo local

### Levantar BD y MCP

El script `.mcp/bancos-mcp.ps1` arranca el contenedor Docker de SQL Server (si no está corriendo) y ejecuta `dotnet watch run` del proyecto `Bancos.Mcp` en el puerto 8000.

```bash
# macOS / Linux
pwsh .mcp/bancos-mcp.ps1

# Windows (PowerShell)
.\.mcp\bancos-mcp.ps1
```

Requisitos: Docker Desktop corriendo y PowerShell (`pwsh`) instalado.

## Validación esperada

| Alcance | Comando |
|---|---|
| MCP | `pwsh .mcp/bancos-mcp.ps1` → `tools/list` responde con tools disponibles |
| Datos | Consultas de conciliación vía MCP `dbquery` cuando esté disponible |
