# Local Secrets

Esta carpeta contiene configuración local sensible usada por herramientas de desarrollo y el servidor MCP local.

## Reglas

- Esta carpeta debe estar en `.gitignore`. Solo este README y los `*.example.json` son versionables.
- **Nunca subir archivos reales al repositorio.**
- Los agentes no deben leer ni mostrar secretos.
- Las herramientas MCP pueden usar estos archivos internamente, pero no retornar valores sensibles al modelo.
- Para validar presencia de configuración sin exponerla: usar la tool `secret.status` del MCP `local-dev-tools`.

## Archivos esperados (no versionados)

| Archivo | Uso | Basado en |
|---|---|---|
| `db.json` | Credenciales SQL del servidor de desarrollo | `db.example.json` |
| `azure_speech.json` | API key y endpoint de Azure Speech | `azure_speech.example.json` |

## Fuente de credenciales

Las credenciales reales provienen de `appsettings.Development.json` (no versionado).
Copiar el archivo `.example.json` correspondiente, renombrarlo sin el `.example` y completar los valores reales.

El skill `voice-bot-db-access` usa `.local-secrets/` como fuente autoritativa.
