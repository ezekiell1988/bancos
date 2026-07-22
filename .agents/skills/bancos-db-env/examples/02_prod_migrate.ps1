# 02_prod_migrate.ps1
# Aplica migraciones EF pendientes a la base de datos de producción.
# NUNCA hace drop. Solo schema changes incrementales.
# Requiere confirmación explícita antes de ejecutar.
#
# Uso: pwsh examples/02_prod_migrate.ps1 [-WhatIf] [-Confirm]

param(
    [switch]$WhatIf,    # solo listar migraciones pendientes, no aplicar
    [switch]$Confirm    # requerido para ejecutar en prod (sin -WhatIf)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot    = Resolve-Path "$PSScriptRoot/../../../.."
$ApiDir      = Join-Path $RepoRoot "src/Bancos.Api"
$SecretsFile = Join-Path $RepoRoot ".local-secrets/dbProd.json"

if (-not (Test-Path $SecretsFile)) {
    Write-Error "No se encontró .local-secrets/dbProd.json. Crear el archivo con Server, Database, User, Password."
}

# Leer secretos y construir connection string (sin exponer contraseña en logs)
$s  = Get-Content $SecretsFile | ConvertFrom-Json
$cs = "Server=$($s.Server);Database=$($s.Database);User Id=$($s.User);Password=$($s.Password);TrustServerCertificate=True;Connect Timeout=30;"

Write-Host ">> Entorno PROD: $($s.Server) / $($s.Database)" -ForegroundColor Red
Write-Warning "Este script modifica la base de datos de producción."

# Listar migraciones pendientes siempre
Write-Host "`n>> Migraciones pendientes en prod:" -ForegroundColor Cyan
Push-Location $ApiDir
dotnet ef migrations list --connection $cs --project Bancos.Api.csproj
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -ne 0) { Write-Error "No se pudo conectar a prod o listar migraciones." }

if ($WhatIf) {
    Write-Host "`n>> Modo --WhatIf: no se aplicaron cambios." -ForegroundColor Yellow
    exit 0
}

if (-not $Confirm) {
    Write-Error "Agregar -Confirm para aplicar migraciones a prod. Revisar la lista de pendientes antes."
}

# Aplicar migraciones (schema only, sin tocar datos)
Write-Host "`n>> Aplicando migraciones EF a prod..." -ForegroundColor Red
Push-Location $ApiDir
dotnet ef database update --connection $cs --project Bancos.Api.csproj
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -ne 0) { Write-Error "dotnet ef database update a prod falló. Revisar rollback manual si aplica." }

Write-Host "`n>> Migraciones aplicadas a prod exitosamente." -ForegroundColor Green
Write-Host ">> Verificar que la API prod responde y Hangfire está operativo." -ForegroundColor Cyan
