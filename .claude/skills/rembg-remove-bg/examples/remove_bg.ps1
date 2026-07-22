# remove_bg.ps1 — Elimina el fondo de PNGs usando rembg (U2Net IA).
# Requiere: Python 3 + rembg  →  pip install rembg[cpu]
#
# Uso:
#   .\remove_bg.ps1 <archivo.png>                         # in-place
#   .\remove_bg.ps1 <carpeta/>                            # todos los PNGs in-place
#   .\remove_bg.ps1 <ruta> -Out <carpeta-salida>          # guarda copia sin tocar original
#   .\remove_bg.ps1 <ruta> -Model isnet-general-use       # bordes más nítidos
#   .\remove_bg.ps1 <ruta> -DryRun                        # solo muestra qué procesaría

param(
    [Parameter(Mandatory)][string]$Path,
    [string]$Out    = '',
    [ValidateSet('u2net','u2net_human_seg','isnet-general-use','silueta')]
    [string]$Model  = 'u2net',
    [switch]$DryRun
)

# Verificar Python
$py = Get-Command python3 -ErrorAction SilentlyContinue
if (-not $py) { $py = Get-Command python -ErrorAction SilentlyContinue }
if (-not $py) {
    Write-Error "Python no encontrado. Instala Python 3 y ejecuta: pip install rembg[cpu]"
    exit 1
}

# Recopilar archivos PNG
$files = @()
$resolved = [IO.Path]::GetFullPath($Path)

if (Test-Path $resolved -PathType Leaf) {
    if ($resolved -notmatch '\.png$') {
        Write-Warning "$resolved no es PNG, se omite."
        exit 0
    }
    $files = @($resolved)
} elseif (Test-Path $resolved -PathType Container) {
    $files = Get-ChildItem $resolved -Filter '*.png' | Sort-Object Name | Select-Object -ExpandProperty FullPath
    if (-not $files) {
        Write-Warning "No se encontraron PNGs en $resolved"
        exit 0
    }
} else {
    # Glob
    $files = Get-Item $Path -ErrorAction SilentlyContinue |
             Where-Object { $_.Extension -eq '.png' } |
             Sort-Object Name |
             Select-Object -ExpandProperty FullPath
    if (-not $files) {
        Write-Warning "No se encontraron PNGs para: $Path"
        exit 0
    }
}

$total = $files.Count

if ($DryRun) {
    Write-Host "[dry-run] Modelo: $Model | Archivos encontrados: $total"
    foreach ($f in $files) {
        $dest = if ($Out) { [IO.Path]::Combine([IO.Path]::GetFullPath($Out), [IO.Path]::GetFileName($f)) } else { "(in-place)" }
        Write-Host "  $([IO.Path]::GetFileName($f))  -> $dest"
    }
    exit 0
}

# Verificar rembg
$check = & $py.Source -c "import rembg" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "rembg no instalado. Ejecuta: pip install rembg[cpu]"
    exit 1
}

# Crear carpeta de salida si aplica
if ($Out) {
    $outResolved = [IO.Path]::GetFullPath($Out)
    New-Item -ItemType Directory -Path $outResolved -Force | Out-Null
} else {
    $outResolved = ''
}

Write-Host "Procesando $total archivo(s) con modelo '$Model'..."

$ok = 0; $errors = 0

foreach ($i in 1..$total) {
    $src  = $files[$i - 1]
    $name = [IO.Path]::GetFileName($src)
    $dest = if ($outResolved) { [IO.Path]::Combine($outResolved, $name) } else { $src }

    Write-Host -NoNewline "  [$i/$total] $name ... "

    $pyScript = @"
from rembg import remove, new_session
from PIL import Image
session = new_session('$Model')
img = Image.open(r'$src').convert('RGBA')
result = remove(img, session=session)
result.save(r'$dest')
"@

    $out2 = $pyScript | & $py.Source - 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK"
        $ok++
    } else {
        Write-Host "ERROR: $out2"
        $errors++
    }
}

Write-Host "`nResultado: $ok OK, $errors errores de $total archivos."
