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

Definidas en `docker-compose.yml`:

| Campo    | Valor               |
|----------|---------------------|
| Host     | `localhost,1433`    |
| Usuario  | `sa`                |
| Password | `DevLocal_Bancos1!` |
| Base     | `dbbancos`          |

### Configuración para la aplicación (código)

La cadena de conexión se configura en `src/Bancos.Api/appsettings.Development.json` — esta es la **única fuente** que usa el código de la aplicación:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=dbbancos;User Id=sa;Password=DevLocal_Bancos1!;TrustServerCertificate=True"
  }
}
```

### Configuración para el MCP `dbquery` (uso exclusivo del asistente IA)

El archivo `.local-secrets/db.json` usa el formato propio del tool MCP y **no** es leído por la aplicación:

```json
{
  "Server": "localhost,1433",
  "Database": "dbbancos",
  "User": "sa",
  "Password": "DevLocal_Bancos1!"
}
```

> **Importante:** No mezclar estos dos archivos. `appsettings.Development.json` = código de la app. `.local-secrets/db.json` = acceso del asistente IA para consultas de solo lectura. Tienen formatos distintos.

## Notas

* Los datos persisten en el volumen Docker `bancos_bancos-sql-data`. Para resetear: `docker compose down -v`.
* La imagen es `mcr.microsoft.com/mssql/server:2022-latest` (linux/amd64; emulado via Rosetta en Apple Silicon).
* Para producción/Azure, la cadena de conexión se inyecta via variable de entorno o appsettings no versionados — no hay cambios en código.

## Datos de referencia necesarios para importación

### Tipos de cambio USD

La tabla `ExchangeRates` debe tener datos para el rango de fechas de los archivos a importar. Sin ellos, las transacciones USD en tarjetas de crédito BAC fallan con "No existe tipo de cambio USD para la fecha ...".

```bash
# Insertar tipos de cambio de referencia (ajustar fecha y tasa según el período)
docker exec bancos-sql-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'DevLocal_Bancos1!' -d dbbancos -No -Q "
DECLARE @rate DECIMAL(18,4) = 519.50;
DECLARE @d DATE = '2026-05-01';
WHILE @d <= '2026-07-31'
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.ExchangeRates WHERE RateDate = @d AND CurrencyCode = 'USD')
        INSERT INTO dbo.ExchangeRates (Id, RateDate, CurrencyCode, CrcPerUnit, CreatedUtc)
        VALUES (NEWID(), @d, 'USD', @rate, GETUTCDATE());
    SET @d = DATEADD(DAY, 1, @d);
END"
```

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

```bash
# 1. Detener el servidor de la aplicación primero

# 2. Eliminar y recrear la base de datos
docker exec bancos-sql-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'DevLocal_Bancos1!' -No -Q \
  "DROP DATABASE IF EXISTS dbbancos; CREATE DATABASE dbbancos;"

# 3. Aplicar migraciones
cd src/Bancos.Api
dotnet ef database update

# 4. Insertar tipos de cambio (ver sección anterior)
```
