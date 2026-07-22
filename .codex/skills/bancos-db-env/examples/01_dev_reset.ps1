# 01_dev_reset.ps1
# Resetea completamente la BD de desarrollo local.
# Uso: pwsh examples/01_dev_reset.ps1 [-SkipSeedZip]
# SOLO para entorno dev. NUNCA ejecutar contra prod.

param(
    [switch]$SkipSeedZip   # omitir la carga de src/input.zip tras el reset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot  = Resolve-Path "$PSScriptRoot/../../../.."
$ApiDir    = Join-Path $RepoRoot "src/Bancos.Api"
$SecretsFile = Join-Path $RepoRoot ".local-secrets/db.json"

# Validar que apuntamos al entorno local (no prod)
if (-not (Test-Path $SecretsFile)) {
    Write-Error "No se encontró .local-secrets/db.json. Este script es solo para dev local."
}
$secrets = Get-Content $SecretsFile | ConvertFrom-Json
if ($secrets.Server -match "azure|database\.windows\.net") {
    Write-Error "ABORT: el servidor parece ser Azure/Prod. Este script es solo para dev local."
}

Write-Host ">> Entorno dev: $($secrets.Server) / $($secrets.Database)" -ForegroundColor Cyan

# Detener procesos de la API que puedan tener conexiones abiertas
$procs = Get-Process -Name "Bancos.Api" -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host ">> Deteniendo Bancos.Api..."
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Drop completo
Write-Host ">> Eliminando base de datos dev..." -ForegroundColor Yellow
Push-Location $ApiDir
dotnet ef database drop --force --project Bancos.Api.csproj
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "dotnet ef database drop falló." }

# Recrear desde migraciones
Write-Host ">> Aplicando migraciones EF..." -ForegroundColor Yellow
dotnet ef database update --project Bancos.Api.csproj
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "dotnet ef database update falló." }
Pop-Location

# Verificar estado de migraciones
Write-Host ">> Estado de migraciones:" -ForegroundColor Cyan
Push-Location $ApiDir
dotnet ef migrations list --project Bancos.Api.csproj
Pop-Location

Write-Host "`n>> Reset dev completado. Inicia la API para que Hangfire cree sus tablas." -ForegroundColor Green

if (-not $SkipSeedZip) {
    $zipPath = Join-Path $RepoRoot "src/input.zip"
    if (Test-Path $zipPath) {
        Write-Host ">> Para cargar datos de prueba, usa:" -ForegroundColor Cyan
        Write-Host "   Invoke-RestMethod -Uri http://localhost:5000/api/imports -Method POST -Form @{file=Get-Item '$zipPath'}"
    }
}
