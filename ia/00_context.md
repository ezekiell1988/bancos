# 00 — Contexto del Proyecto

> Última actualización: 2026-07-18
> Alcance activo: aplicación familiar local para consolidar estados bancarios.

## Identidad

| Campo | Valor |
|---|---|
| Nombre | Bancos |
| Propósito | Generar estados mensuales de pérdidas y ganancias y situación financiera para Familia Baltodano Soto. |
| Usuarios | Ezequiel Baltodano y Karen Soto. |
| Despliegue actual | Local, sin autenticación. |
| Despliegue futuro | Contenedor Azure; autenticación pendiente antes de exponerlo. |

## Stack decidido

| Capa | Tecnología |
|---|---|
| API y jobs | .NET 10, C# 14, Minimal APIs por features |
| Frontend | Angular standalone compilado y servido por API |
| Datos | Microsoft SQL Server |
| Procesos | Hangfire SQL Server y Hangfire.Console |
| Clasificación excepcional | Azure AI; solo descripción normalizada y catálogo de categorías |

## Límites y constantes críticas

* Datos financieros y credenciales son sensibles: nunca copiarlos a `/ia`, logs o prompts de IA.
* La moneda funcional es CRC. USD conserva importe original, importe convertido y tipo de cambio diario único.
* Solo pasivos USD generan diferencial cambiario en alcance inicial.
* Archivos importados se descartan después de extracción exitosa; persisten datos estructurados y huellas de importación.
* No hay autenticación local. Azure exige una tarea de seguridad aprobada antes de publicar.

## Mapa

| Ruta | Propósito |
|---|---|
| `src/input/` | Muestra histórica inicial; no se modifica durante análisis. |
| `src/output/` | Aplicación final compilada/servida. |
| `.local-secrets/` | Configuración local ignorada; nunca versionar ni leer valores en respuestas. |
| `ia/` | Contexto y trazabilidad de trabajo. |

## Validación esperada

| Alcance | Comando |
|---|---|
| API | `dotnet build` y `dotnet test` |
| Frontend | `npm run build` y pruebas configuradas |
| Datos | Consultas de conciliación vía MCP `dbquery` cuando esté disponible |
