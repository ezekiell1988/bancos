# resize.ps1 — Verificar dimensiones, redimensionar y recortar PNGs.
# Usa sips (macOS built-in) para resize y Python + Pillow para trim.
#
# Uso:
#   .\resize.ps1 <archivo.png>                          # solo verificar dimensiones
#   .\resize.ps1 <carpeta/>                             # verificar todos los PNGs
#   .\resize.ps1 <ruta> -Width 128 -Height 128          # resize in-place
#   .\resize.ps1 <ruta> -Width 128 -Height 128 -Out <dest>  # resize a copia
#   .\resize.ps1 <ruta> -Trim                           # recortar transparencia/whitespace
#   .\resize.ps1 <ruta> -Trim -Padding 8                # recortar + padding de 8px
#   .\resize.ps1 <ruta> -Trim -Width 128 -Height 128    # trim y luego resize

param(
    [Parameter(Mandatory)][string]$Path,
    [int]$Width    = 0,
    [int]$Height   = 0,
    [string]$Out   = '',
    [switch]$Trim,
    [int]$Padding  = 0
)

# ── Helpers ──────────────────────────────────────────────────────────────────

function Get-PngFiles([string]$p) {
    $resolved = [IO.Path]::GetFullPath($p)
    if (Test-Path $resolved -PathType Leaf) {
        if ($resolved -notmatch '\.png$') { Write-Warning "$resolved no es PNG"; return @() }
        return @($resolved)
    }
    if (Test-Path $resolved -PathType Container) {
        $files = Get-ChildItem $resolved -Filter '*.png' | Sort-Object Name | % FullName
        if (-not $files) { Write-Warning "No se encontraron PNGs en $resolved" }
        return @($files)
    }
    # Glob
    $files = Get-Item $p -ErrorAction SilentlyContinue |
             Where-Object Extension -eq '.png' |
             Sort-Object Name | % FullName
    if (-not $files) { Write-Warning "No se encontraron PNGs para: $p" }
    return @($files)
}

function Check-Sips {
    if (-not (Get-Command sips -ErrorAction SilentlyContinue)) {
        Write-Error "sips no encontrado. Reinstala Xcode Command Line Tools: xcode-select --install"
        exit 1
    }
}

function Get-Python {
    $py = Get-Command python3 -ErrorAction SilentlyContinue
    if (-not $py) { $py = Get-Command python -ErrorAction SilentlyContinue }
    if (-not $py) { Write-Error "Python no encontrado. Instala Python 3 + Pillow (pip install Pillow)"; exit 1 }
    return $py.Source
}

# ── Main ─────────────────────────────────────────────────────────────────────

$files = Get-PngFiles $Path
if (-not $files) { exit 0 }

$doResize = ($Width -gt 0 -or $Height -gt 0)
$hasOut   = $Out -ne ''

# Preparar carpeta de salida
if ($hasOut) {
    $outResolved = [IO.Path]::GetFullPath($Out)
    if ($outResolved -notmatch '\.png$') {
        New-Item -ItemType Directory -Path $outResolved -Force | Out-Null
    }
} else {
    $outResolved = ''
}

# ── 1. CHECK: siempre mostrar dimensiones actuales ───────────────────────────
Write-Host "Dimensiones actuales:"
foreach ($f in $files) {
    $info = sips -g pixelWidth -g pixelHeight $f 2>&1 |
            Select-String 'pixelWidth|pixelHeight'
    $w = ($info | Where-Object { $_ -match 'pixelWidth'  }) -replace '.*:\s*', ''
    $h = ($info | Where-Object { $_ -match 'pixelHeight' }) -replace '.*:\s*', ''
    $alpha = (sips -g hasAlpha $f 2>&1 | Select-String 'hasAlpha') -replace '.*:\s*', ''
    Write-Host "  $([IO.Path]::GetFileName($f))  ${w}x${h}  alpha:$alpha"
}

if (-not $doResize -and -not $Trim) { exit 0 }

Check-Sips

# ── 2. TRIM: recortar transparencia/whitespace con Pillow ────────────────────
if ($Trim) {
    $py = Get-Python
    Write-Host "`nRecortando transparencia (padding: $Padding px)..."

    foreach ($f in $files) {
        $dest = if ($outResolved -and $outResolved -notmatch '\.png$') {
                    [IO.Path]::Combine($outResolved, [IO.Path]::GetFileName($f))
                } elseif ($outResolved -match '\.png$') { $outResolved }
                else { $f }

        $pyScript = @"
from PIL import Image
import sys
src  = r'$f'
dst  = r'$dest'
pad  = $Padding
img  = Image.open(src).convert('RGBA')
bbox = img.getbbox()
if bbox is None:
    img.save(dst)
    print(f'  {src} → sin contenido, guardado sin cambios')
    sys.exit(0)
left  = max(0, bbox[0] - pad)
upper = max(0, bbox[1] - pad)
right = min(img.width,  bbox[2] + pad)
lower = min(img.height, bbox[3] + pad)
cropped = img.crop((left, upper, right, lower))
cropped.save(dst, 'PNG')
print(f'  original={img.size} → recortado={cropped.size}')
"@

        Write-Host -NoNewline "  $([IO.Path]::GetFileName($f)) ... "
        $result = $pyScript | & $py - 2>&1
        if ($LASTEXITCODE -eq 0) { Write-Host $result } else { Write-Host "ERROR: $result" }

        # Si se hizo trim in-place, el resize posterior opera sobre el mismo archivo
        if ($dest -ne $f) { $f = $dest }
    }
}

# ── 3. RESIZE con sips ───────────────────────────────────────────────────────
if ($doResize) {
    $wArg = if ($Width  -gt 0) { $Width  } else { $Height }
    $hArg = if ($Height -gt 0) { $Height } else { $Width  }
    Write-Host "`nRedimensionando a ${wArg}x${hArg}..."

    foreach ($f in $files) {
        $dest = if ($outResolved -and $outResolved -notmatch '\.png$') {
                    [IO.Path]::Combine($outResolved, [IO.Path]::GetFileName($f))
                } elseif ($outResolved -match '\.png$') { $outResolved }
                else { '' }

        $sipsArgs = @('--resampleHeightWidth', $hArg, $wArg, $f)
        if ($dest) { $sipsArgs += @('--out', $dest) }

        Write-Host -NoNewline "  $([IO.Path]::GetFileName($f)) ... "
        $result = & sips @sipsArgs 2>&1
        if ($LASTEXITCODE -eq 0) { Write-Host "OK" } else { Write-Host "ERROR: $result" }
    }
}

# ── 4. Confirmar dimensiones finales ────────────────────────────────────────
Write-Host "`nDimensiones finales:"
$checkFiles = if ($outResolved -and (Test-Path $outResolved -PathType Container)) {
    Get-ChildItem $outResolved -Filter '*.png' | % FullName
} else { $files }

foreach ($f in $checkFiles) {
    if (-not (Test-Path $f)) { continue }
    $info = sips -g pixelWidth -g pixelHeight $f 2>&1 |
            Select-String 'pixelWidth|pixelHeight'
    $w = ($info | Where-Object { $_ -match 'pixelWidth'  }) -replace '.*:\s*', ''
    $h = ($info | Where-Object { $_ -match 'pixelHeight' }) -replace '.*:\s*', ''
    Write-Host "  $([IO.Path]::GetFileName($f))  ${w}x${h}"
}
