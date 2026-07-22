<#
.SYNOPSIS
    Revisa y sincroniza las carpetas de skills entre .agents, .claude y .codex.

.DESCRIPTION
    Para cada skill compara contenido entre .agents, .claude y .codex y elige como
    fuente la copia con LastWriteTimeUtc mas reciente. Luego la replica a los tres
    destinos. .agents conserva prioridad solo como desempate cuando contenido y fecha
    son identicos. Un empate de fecha con contenido distinto es ambiguo: se reporta y
    no se sobrescribe automaticamente.

.PARAMETER Apply
    Ejecuta los cambios. Sin este switch corre en modo reporte (dry-run).

.PARAMETER AutoBase
    Compatibilidad/CI: fuerza .agents como fuente incluso si no es la mas reciente.

.EXAMPLE
    .\sync-skills.ps1              # solo reporte
    .\sync-skills.ps1 -Apply       # interactivo, aplica cambios
    .\sync-skills.ps1 -Apply -AutoBase
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$AutoBase
)

$ErrorActionPreference = 'Stop'
function Find-RepoRoot {
    param([string]$StartPath)

    $current = Get-Item -LiteralPath $StartPath
    while ($null -ne $current) {
        if (Test-Path (Join-Path $current.FullName '.agents/skills') -PathType Container) {
            return $current.FullName
        }
        $current = $current.Parent
    }
    throw "No se encontro raiz de repo desde: $StartPath"
}

$repoRoot = Find-RepoRoot -StartPath $PSScriptRoot

$baseName = '.agents'
$roots = [ordered]@{
    '.agents' = Join-Path $repoRoot '.agents\skills'
    '.claude' = Join-Path $repoRoot '.claude\skills'
    '.codex'  = Join-Path $repoRoot '.codex\skills'
}

if (-not (Test-Path $roots[$baseName] -PathType Container)) {
    throw "No existe la carpeta base: $($roots[$baseName])"
}
foreach ($name in @($roots.Keys)) {
    if (-not (Test-Path $roots[$name])) {
        Write-Host "Creando carpeta faltante: $($roots[$name])" -ForegroundColor Yellow
        if ($Apply) { New-Item -ItemType Directory -Path $roots[$name] -Force | Out-Null }
    }
}

function Get-SkillState {
    <# Devuelve fingerprint (hash combinado) y ultima modificacion de un skill dir. #>
    param([string]$Path)

    if (-not (Test-Path $Path -PathType Container)) { return $null }
    $files = Get-ChildItem -LiteralPath $Path -Recurse -File | Sort-Object { $_.FullName.Substring($Path.Length).ToLowerInvariant() }
    if (-not $files) {
        return [pscustomobject]@{ Hash = 'EMPTY'; LastWrite = (Get-Item $Path).LastWriteTime; Files = 0 }
    }
    $sb = [System.Text.StringBuilder]::new()
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($Path.Length).TrimStart('\', '/').ToLowerInvariant()
        $md5 = (Get-FileHash -LiteralPath $f.FullName -Algorithm MD5).Hash
        [void]$sb.AppendLine("$rel|$md5")
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($sb.ToString())
    $hash = [System.BitConverter]::ToString([System.Security.Cryptography.MD5]::Create().ComputeHash($bytes)).Replace('-', '')
    $lastWrite = ($files | Measure-Object LastWriteTime -Maximum).Maximum
    return [pscustomobject]@{ Hash = $hash; LastWrite = $lastWrite; Files = $files.Count }
}

function Sync-Skill {
    <# Copia espejo cross-platform de un skill desde $From hacia $To. #>
    param([string]$From, [string]$To)

    if (-not $Apply) {
        Write-Host "    [dry-run] $From -> $To" -ForegroundColor DarkGray
        return
    }
    if (Test-Path $To) { Remove-Item -LiteralPath $To -Recurse -Force }
    Copy-Item -LiteralPath $From -Destination $To -Recurse -Force
    Write-Host "    Sincronizado: $To" -ForegroundColor Green
}

# --- Recolectar skills (union de las tres carpetas) ---
$allSkills = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($name in $roots.Keys) {
    if (Test-Path $roots[$name]) {
        Get-ChildItem -LiteralPath $roots[$name] -Directory | ForEach-Object { [void]$allSkills.Add($_.Name) }
    }
}

Write-Host "`nSkills encontrados: $($allSkills.Count)  (base: $baseName)" -ForegroundColor Cyan
if (-not $Apply) { Write-Host "MODO REPORTE (dry-run). Usa -Apply para aplicar cambios.`n" -ForegroundColor Yellow }

$stats = @{ Identicos = 0; Sincronizados = 0; Ambiguos = 0; SoloFuera = 0 }

foreach ($skill in $allSkills) {
    $states = [ordered]@{}
    foreach ($name in $roots.Keys) {
        $states[$name] = Get-SkillState -Path (Join-Path $roots[$name] $skill)
    }

    $present = @($states.Keys | Where-Object { $states[$_] })
    $hashes = $present | ForEach-Object { $states[$_].Hash } | Sort-Object -Unique

    # Caso 1: existe en todas y son identicas
    if ($present.Count -eq $roots.Count -and $hashes.Count -eq 1) {
        $stats.Identicos++
        continue
    }

    # Caso 2: existe solo fuera de .agents; la copia mas reciente se adopta automaticamente.
    if ($baseName -notin $present) {
        $stats.SoloFuera++
        $src = $present | Sort-Object { $states[$_].LastWrite.ToUniversalTime() } -Descending | Select-Object -First 1
        Write-Host "`n[$skill] solo existe en: $($present -join ', ') -> fuente mas reciente: $src" -ForegroundColor Magenta
        foreach ($name in $roots.Keys) {
            if ($name -ne $src) { Sync-Skill -From (Join-Path $roots[$src] $skill) -To (Join-Path $roots[$name] $skill) }
        }
        $stats.Sincronizados++
        continue
    }

    # Caso 3: identicas donde existen, pero falta en alguna carpeta -> copiar desde base
    if ($hashes.Count -eq 1) {
        $missing = @($roots.Keys | Where-Object { -not $states[$_] })
        Write-Host "`n[$skill] falta en: $($missing -join ', ') -> copiando desde $baseName" -ForegroundColor Yellow
        foreach ($name in $missing) {
            Sync-Skill -From (Join-Path $roots[$baseName] $skill) -To (Join-Path $roots[$name] $skill)
        }
        $stats.Sincronizados++
        continue
    }

    # Caso 4: contenido diferente -> usar ultima modificacion UTC, excepto empate ambiguo.
    $newestTime = ($present | ForEach-Object { $states[$_].LastWrite.ToUniversalTime() } | Measure-Object -Maximum).Maximum
    $newestCandidates = @($present | Where-Object { $states[$_].LastWrite.ToUniversalTime() -eq $newestTime })
    $newest = $newestCandidates | Select-Object -First 1
    Write-Host "`n[$skill] DIFERENCIAS detectadas:" -ForegroundColor Red
    foreach ($name in $present) {
        $s = $states[$name]
        $mark = if ($name -in $newestCandidates) { ' <- mas reciente' } else { '' }
        $shortHash = if ($s.Hash.Length -gt 8) { $s.Hash.Substring(0, 8) } else { $s.Hash }
        Write-Host ("  {0,-8} {1}  {2} archivo(s)  hash {3}{4}" -f $name, $s.LastWrite.ToString('yyyy-MM-dd HH:mm:ss'), $s.Files, $shortHash, $mark)
    }

    $newestHashes = @($newestCandidates | ForEach-Object { $states[$_].Hash } | Sort-Object -Unique)
    if ($newestHashes.Count -gt 1) {
        Write-Host '  Empate ambiguo: misma fecha UTC, contenido distinto. Sin cambios.' -ForegroundColor Yellow
        $stats.Ambiguos++
        continue
    }

    $source = if ($AutoBase) { $baseName } elseif ($baseName -in $newestCandidates) { $baseName } else { $newest }

    Write-Host "  Fuente elegida: $source" -ForegroundColor Cyan
    foreach ($name in $roots.Keys) {
        if ($name -ne $source) {
            Sync-Skill -From (Join-Path $roots[$source] $skill) -To (Join-Path $roots[$name] $skill)
        }
    }
    $stats.Sincronizados++
}

Write-Host "`n===== Resumen =====" -ForegroundColor Cyan
Write-Host "  Identicos:      $($stats.Identicos)"
Write-Host "  Sincronizados:  $($stats.Sincronizados)"
Write-Host "  Ambiguos:       $($stats.Ambiguos)"
Write-Host "  Solo fuera base:$($stats.SoloFuera)"
if (-not $Apply) { Write-Host "`n(dry-run: nada fue modificado)" -ForegroundColor Yellow }
