# MCP dbQuery — SQL Server local para voice-bot

MCP local que expone un único tool, `db_exec`, para correr lotes de sentencias
T-SQL arbitrarias y documentar el resultado en un Markdown. Uso exclusivo de
desarrollo local — no aplica al MCP SQL de producción.

El mismo `server.mjs` se registra dos veces, una por base de datos, cada una
apuntando a un archivo de credenciales distinto vía `--secrets-file`:

| Instancia | `--secrets-file` | Base |
|---|---|---|
| `dbQuery` | `db_voicebot.json` | `voicebot` |
| `dbQueryClickeat` | `db_dev_clickeat.json` | `dev_clickeat` |

No hay lógica distinta entre ambas — es el mismo código, solo cambian las
credenciales cargadas y el `serverInfo.name` (`--server-name`) usado en logs.

## Arquitectura

Sigue el patrón del skill `mcp-vscode` (1 tool = 1 archivo en `tools/`, autodescubierto
por `.mcp/_shared/registry.mjs`). `server.mjs` es un adaptador delgado que solo resuelve
`--project-root`/`--secrets-file`/`--server-name` y delega el arranque en
`.mcp/_shared/runtime.mjs`.

```text
.mcp/db-query/
├── server.mjs               ← host stdio; resuelve flags y llama runServer()
├── src/
│   ├── common.mjs           ← --project-root/--secrets-file/--server-name (singleton)
│   ├── database.mjs         ← getPool() cableado sobre _shared/mssql-pool + sql-secrets
│   ├── executor.mjs         ← ejecuta una query, normaliza a {status, rowsAffected, resultsets, error}
│   ├── markdown.mjs         ← renderiza el reporte Markdown
│   └── paths.mjs            ← resuelve dónde escribir el reporte (queries/, ruta relativa o absoluta)
├── tools/db_exec.mjs        ← schema, handler y smoke() del único tool
└── tests/smoke.mjs          ← protocolo + ejecución real contra ambas instancias
```

El driver `mssql` y la plomería SQL transversal (`mssql-pool.mjs`, `sql-secrets.mjs`, ya
compartida con `llm-audit-db` cuando exista) viven en `.mcp/_shared/`. `mssql` se resuelve
desde `.mcp/node_modules` (dependencia en `.mcp/package.json`, único `package.json` del
workspace de servidores MCP locales).

## Tool → transporte

| Tool | Transporte | Descripción |
|---|---|---|
| `db_exec` | stdio (MCP tools/call) | Ejecuta `queries: string[]` contra SQL Server, una request independiente por elemento, y guarda un Markdown en `filePath`. |

Con un único tool no aplica el patrón de ahorro de tokens que usa `ia-workflow`
(bundle por `intent`, `mode: summary`, etc.) — el catálogo ya es mínimo.

## Contrato de `db_exec`

| Parámetro | Tipo | Requerido | Uso |
|---|---|---|---|
| `queries` | `string[]` | Sí | Sentencias T-SQL ejecutadas en orden, cada una como request independiente (SELECT, DECLARE, DDL, DML, procs). |
| `filePath` | `string` | Sí | Archivo de salida, extensión `.md` obligatoria. |
| `timeoutSeconds` | `integer` | No | 1–120, default 30. |

Resolución de `filePath`:

- nombre suelto (`diagnostico.md`) → `.mcp/db-query/queries/diagnostico.md`
- ruta relativa con directorio → relativa a la raíz del proyecto
- ruta absoluta → se usa tal cual
- el directorio destino se crea si no existe

La respuesta del tool es solo `{ success, filePath }` — el contenido completo
vive en el Markdown, no se duplica en la respuesta JSON. `success` es `false`
si alguna consulta falló, pero el archivo se escribe siempre con todas las
entradas documentadas (una consulta que falla no detiene las siguientes).

## Política de escritura — sin gate de `apply`

A diferencia de `db-query-pro` (paquete opcional, no incluido aquí), este
perfil local **no exige `apply: true` ni filtra operaciones por palabra
clave**: cualquier sentencia T-SQL aceptada por el driver `mssql` se ejecuta
directamente, incluidas DDL/DML. La única protección es de secretos: los
valores de `Server/User/Password` nunca se registran en logs ni se devuelven
en la respuesta del tool (ver `.mcp/_shared/sql-secrets.mjs`).

## Diferencia conocida vs. la versión .NET (driver `mssql`/tedious)

Para sentencias sin resultset ni modificación de filas (`DECLARE`, `PRINT`,
`SET ...`), `SqlClient` (.NET) reporta `RecordsAffected = -1` y el reporte
muestra `Filas afectadas: 0`; el driver `mssql`/tedious no distingue ese caso
y reporta `rowsAffected: [1]`, por lo que el Markdown puede mostrar
`Filas afectadas: 1` donde la versión .NET mostraba `0`. El resto del
contrato (estructura del reporte, columnas/filas de resultsets, manejo de
errores, `success`) es idéntico. No se intenta emular el comportamiento de
`SqlClient` aquí — es una limitación del driver Node, no un bug del port.

## Ejecución local

```bash
node .mcp/db-query/server.mjs --project-root . --secrets-file db_voicebot.json
node .mcp/db-query/server.mjs --project-root . --secrets-file db_dev_clickeat.json --server-name dbQueryClickeat
```

| Flag | Default | Uso |
|---|---|---|
| `--project-root` | `process.cwd()` | Raíz del proyecto; ahí vive `.local-secrets/`. |
| `--secrets-file` | `sqlserver.json` | Nombre de archivo bajo `.local-secrets/` (sin rutas ni `..`). |
| `--server-name` | `dbQuery` | `serverInfo.name` reportado en `initialize` y prefijo de logs. |

Requiere `.local-secrets/{db_voicebot.json,db_dev_clickeat.json}` en la raíz
del proyecto (mismo esquema que `db_voicebot.example.json` /
`db_dev_clickeat.example.json`: `Server`, `Database`, `User`, `Password`).
Esos archivos nunca se versionan.

## Configuración por cliente

### VS Code (`.vscode/mcp.json`)

```json
{
  "servers": {
    "dbQuery": {
      "type": "stdio",
      "command": "node",
      "args": [
        "${workspaceFolder}/.mcp/db-query/server.mjs",
        "--project-root",
        "${workspaceFolder}",
        "--secrets-file",
        "db_voicebot.json"
      ]
    },
    "dbQueryClickeat": {
      "type": "stdio",
      "command": "node",
      "args": [
        "${workspaceFolder}/.mcp/db-query/server.mjs",
        "--project-root",
        "${workspaceFolder}",
        "--secrets-file",
        "db_dev_clickeat.json",
        "--server-name",
        "dbQueryClickeat"
      ]
    }
  }
}
```

### Claude Code (`.mcp.json`)

```json
{
  "mcpServers": {
    "dbQuery": {
      "command": "node",
      "args": [
        ".mcp/db-query/server.mjs",
        "--project-root",
        ".",
        "--secrets-file",
        "db_voicebot.json"
      ]
    },
    "dbQueryClickeat": {
      "command": "node",
      "args": [
        ".mcp/db-query/server.mjs",
        "--project-root",
        ".",
        "--secrets-file",
        "db_dev_clickeat.json",
        "--server-name",
        "dbQueryClickeat"
      ]
    }
  }
}
```

## Validación

```bash
node --check .mcp/db-query/server.mjs .mcp/db-query/src/*.mjs .mcp/db-query/tools/*.mjs
node .mcp/db-query/tests/smoke.mjs
```

`tests/smoke.mjs` corre contra **ambas instancias reales** (`db_voicebot.json` y
`db_dev_clickeat.json`): protocolo completo (handshake, negociación de
`protocolVersion`, catálogo con `db_exec` únicamente, validación de argumentos)
contra `dbQuery`, y ejecución real de `SELECT 1` contra `dbQueryClickeat` para
confirmar que el segundo `--secrets-file` conecta de verdad. El archivo de
reporte que genera el smoke se limpia al final de cada corrida.

## Protocolo

- `stdio` con JSON-RPC **delimitado por saltos de línea** (nunca `Content-Length`).
- Logs solo a `stderr`.
- Versiones soportadas: `2025-03-26`, `2024-11-05`; versión desconocida → la más reciente soportada.

Documentación de conceptos MCP: <https://modelcontextprotocol.io/> y <https://code.visualstudio.com/docs/copilot/chat/mcp-servers>.
