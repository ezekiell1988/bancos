# Entorno de desarrollo local — SQL Server en Docker

> Creado: 2026-07-20 | Tarea: TASK-EBC-INF-08

## Requisitos

* Docker Desktop corriendo
* .NET 10 SDK con `dotnet-ef` instalado globalmente

## Primer uso (desde cero)

```bash
# 1. Levantar SQL Server local
docker compose up -d

# 2. Esperar ~15 s a que el servidor esté listo y aplicar migraciones
cd src/Bancos.Api
dotnet ef database update
```

## Uso diario

```bash
# Levantar contenedor (si no está corriendo)
docker compose up -d

# Detener al terminar el día (opcional, los datos persisten en volumen)
docker compose stop
```

## Credenciales del contenedor local

Definidas en `docker-compose.yml` (no se documentan aquí para evitar exponer secretos fuera de su fuente única).

### Configuración para la aplicación (código)

La cadena de conexión se configura en `src/Bancos.Api/appsettings.Development.json` — esta es la **única fuente** que usa el código de la aplicación. El valor real vive solo en ese archivo local (no versionado con la contraseña real) y en `docker-compose.yml`.

### Configuración para el MCP `dbquery` (uso exclusivo del asistente IA)

El archivo `.local-secrets/db.json` usa el formato propio del tool MCP y **no** es leído por la aplicación. Su contenido nunca se copia a `/ia`, logs ni respuestas.

> **Importante:** No mezclar estos dos archivos. `appsettings.Development.json` = código de la app. `.local-secrets/db.json` = acceso del asistente IA para consultas de solo lectura. Tienen formatos distintos.
>
> **Regla operativa:** para leer datos de `dbbancos`, el asistente IA usa únicamente el MCP `dbquery` (solo lectura). No se documentan ni ejecutan comandos SQL directos (`sqlcmd`, `docker exec`, etc.) con credenciales embebidas en este repositorio.

## Notas

* Los datos persisten en el volumen Docker `bancos_bancos-sql-data`. Para resetear: `docker compose down -v`.
* La imagen es `mcr.microsoft.com/mssql/server:2022-latest` (linux/amd64; emulado via Rosetta en Apple Silicon).
* Para producción/Azure, la cadena de conexión se inyecta via variable de entorno o appsettings no versionados — no hay cambios en código.

## Datos de referencia necesarios para importación

### Tipos de cambio USD

La tabla `ExchangeRates` debe tener datos para el rango de fechas de los archivos a importar. Sin ellos, las transacciones USD en tarjetas de crédito BAC fallan con "No existe tipo de cambio USD para la fecha ...".

Para verificar qué fechas ya tienen tipo de cambio, usa el MCP `dbquery` (solo lectura) contra `dbbancos`. La inserción de datos de referencia es una operación administrativa local que no se documenta aquí con credenciales embebidas; coordinar directamente con Ezequiel si hace falta poblar el rango.

## Reintentar imports fallidos

Si un import queda en `status=3 Failed` o `status=1 Processing` (atascado):

```bash
curl -X POST http://localhost:8000/api/imports/{id}/retry
```

> El archivo temporal (`.local-secrets/imports/{uuid}.upload`) debe existir. Si el import completó anteriormente y el archivo fue borrado, restaurarlo del ZIP original:
> ```bash
> unzip -p src/input.zip "ruta/interna/archivo.pdf" \
>   > "src/Bancos.Api/.local-secrets/imports/{uuid}.upload"
> # El UUID se obtiene del campo TemporaryPath en la tabla Imports
> ```

Ver [IMPORT-PARSER-TROUBLESHOOTING.md](./IMPORT-PARSER-TROUBLESHOOTING.md) para diagnóstico detallado de errores de parsing y concurrencia.

## Resetear la BD completamente

1. Detener el servidor de la aplicación.
2. Eliminar el volumen Docker para reiniciar la base desde cero: `docker compose down -v` (ver sección "Notas").
3. Levantar el contenedor de nuevo (`docker compose up -d`) y aplicar migraciones: `cd src/Bancos.Api && dotnet ef database update`.
4. Insertar tipos de cambio de referencia (ver sección anterior).

## Tool MCP `db_reset_schemas` (elimina todas las tablas de `dbo` y el schema `HangFire`)

**Regla operativa:** cada vez que se use el tool MCP `db_reset_schemas` (con `confirm: true`) para regenerar las migraciones de EF Core desde cero, el asistente debe pedirle a Ezequiel que corra `.mcp/bancos-mcp.ps1` después, para que el MCP vuelva a levantar el contenedor y reaplique migraciones sobre la base limpia. El agente no debe levantar ni reiniciar el proceso por su cuenta (ver regla en `ia/00_context.md`).
