#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Extrae contexto desde ia/05_progress.md e ia/04_tasks.md y produce
  un resumen estructurado que el agente puede usar para llenar el template HTML.

.DESCRIPTION
  Lee los archivos IA del proyecto y genera en STDOUT un bloque Markdown
  con las 4 secciones clave del reporte de avance:
    1. Estado anterior (última sesión reportada)
    2. Lo realizado hoy
    3. Tareas completadas hoy (por ID)
    4. Tareas pendientes (por ID)

  El agente puede copiar esta salida y usarla para completar el template.html.

.PARAMETER Fecha
  Fecha del avance en formato YYYYMMDD. Por defecto, usa la fecha de hoy.

.PARAMETER Abrir
  Si se especifica, abre el HTML generado en el navegador al terminar.

.EXAMPLE
  pwsh .agents/skills/clickeat-avances/examples/generar-avance.ps1
  pwsh .agents/skills/clickeat-avances/examples/generar-avance.ps1 -Fecha 20260602
  pwsh .agents/skills/clickeat-avances/examples/generar-avance.ps1 -Abrir
#>

[CmdletBinding()]
param(
    [string] $Fecha = (Get-Date -Format 'yyyyMMdd'),
    [switch] $Abrir
)

# ── Rutas ───────────────────────────────────────────────────────────────────
$root        = Resolve-Path (Join-Path $PSScriptRoot '../../..')
$progressMd  = Join-Path $root 'ia/05_progress.md'
$tasksMd     = Join-Path $root 'ia/04_tasks.md'
$outputDir   = Join-Path $root 'docs/avances'
$outputFile  = Join-Path $outputDir "$Fecha.html"

# ── Verificar archivos fuente ────────────────────────────────────────────────
foreach ($f in @($progressMd, $tasksMd)) {
    if (-not (Test-Path $f)) {
        Write-Error "No se encontró el archivo: $f"
        exit 1
    }
}

# ── Leer archivos ────────────────────────────────────────────────────────────
$progressContent = Get-Content $progressMd -Raw
$tasksContent    = Get-Content $tasksMd    -Raw

# ── Construir fecha larga en español ────────────────────────────────────────
$meses = @(
    'enero','febrero','marzo','abril','mayo','junio',
    'julio','agosto','septiembre','octubre','noviembre','diciembre'
)
$dt        = [datetime]::ParseExact($Fecha, 'yyyyMMdd', $null)
$fechaLarga = "$($dt.Day) de $($meses[$dt.Month - 1]) de $($dt.Year)"

# ── Extraer sesiones del día ─────────────────────────────────────────────────
# Busca líneas que contengan la fecha en formatos comunes usados en 05_progress.md
$fechaPatterns = @(
    $Fecha,
    $dt.ToString('d/M/yyyy'),
    $dt.ToString('dd/MM/yyyy'),
    $dt.ToString('M/d/yyyy'),
    "$($dt.Day) de $($meses[$dt.Month - 1])"
)

$lines       = $progressContent -split "`n"
$todayLines  = @()
$inSection   = $false

foreach ($line in $lines) {
    $isDateHeader = $fechaPatterns | Where-Object { $line -match [regex]::Escape($_) }
    if ($isDateHeader) {
        $inSection = $true
    }
    elseif ($inSection -and $line -match '^## ' -and -not ($fechaPatterns | Where-Object { $line -match [regex]::Escape($_) })) {
        $inSection = $false
    }
    if ($inSection) {
        $todayLines += $line
    }
}

# ── Extraer tareas completadas hoy ──────────────────────────────────────────
# Busca líneas con ✅ en ia/04_tasks.md que contengan la fecha como modificación reciente
$doneTasks    = $tasksContent -split "`n" | Where-Object { $_ -match '✅' } | Select-Object -First 20
$pendingTasks = $tasksContent -split "`n" | Where-Object { $_ -match '⬜|🔄' } | Select-Object -First 20

# ── Mostrar resumen en STDOUT ────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  RESUMEN PARA REPORTE DE AVANCE — $fechaLarga" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📅 FECHA LARGA:   $fechaLarga"                              -ForegroundColor Yellow
Write-Host "📁 OUTPUT:        $outputFile"                              -ForegroundColor Yellow
Write-Host ""

Write-Host "─── SESIONES DEL DÍA (desde ia/05_progress.md) ───────────" -ForegroundColor Green
if ($todayLines.Count -eq 0) {
    Write-Host "  (No se encontraron entradas para $Fecha — revisar manualmente)"  -ForegroundColor DarkGray
} else {
    $todayLines | ForEach-Object { Write-Host "  $_" }
}
Write-Host ""

Write-Host "─── TAREAS COMPLETADAS ✅ (desde ia/04_tasks.md) ──────────" -ForegroundColor Green
if ($doneTasks.Count -eq 0) {
    Write-Host "  (No se encontraron tareas ✅)"  -ForegroundColor DarkGray
} else {
    $doneTasks | ForEach-Object { Write-Host "  $_" }
}
Write-Host ""

Write-Host "─── TAREAS PENDIENTES ⬜/🔄 (desde ia/04_tasks.md) ────────" -ForegroundColor Yellow
if ($pendingTasks.Count -eq 0) {
    Write-Host "  (No se encontraron tareas pendientes)" -ForegroundColor DarkGray
} else {
    $pendingTasks | ForEach-Object { Write-Host "  $_" }
}
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PRÓXIMO PASO: pide al agente que genere el HTML en:"      -ForegroundColor Cyan
Write-Host "  $outputFile"                                              -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Comando sugerido para el agente:" -ForegroundColor Gray
Write-Host "  'Con el contexto anterior, genera el avance del $fechaLarga'" -ForegroundColor White
Write-Host "   'usando el skill clickeat-avances y guárdalo en docs/avances/$Fecha.html'" -ForegroundColor White
Write-Host ""

# ── Abrir en navegador si se solicitó ───────────────────────────────────────
if ($Abrir -and (Test-Path $outputFile)) {
    Write-Host "🌐 Abriendo $outputFile en el navegador..." -ForegroundColor Cyan
    if ($IsMacOS) {
        open $outputFile
    } elseif ($IsWindows) {
        Start-Process $outputFile
    } else {
        xdg-open $outputFile 2>/dev/null
    }
}
