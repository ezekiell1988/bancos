# demo-06-png-to-ico.ps1
# Convierte un PNG (generado por gpt-image-2) a favicon.ico con múltiples tamaños.
# Requiere: Python 3 + Pillow  →  pip install Pillow
#
# Uso:
#   .\demo-06-png-to-ico.ps1
#   .\demo-06-png-to-ico.ps1 -SrcPng "ruta\logo.png" -DstIco "ruta\favicon.ico"

param(
    [string]$SrcPng = (Join-Path $PSScriptRoot '..\..\..\..\src\MarketingOneOnOneWeb2\public\logo.png'),
    [string]$DstIco = (Join-Path $PSScriptRoot '..\..\..\..\src\MarketingOneOnOneWeb2\public\favicon.ico')
)

$SrcPng = [IO.Path]::GetFullPath($SrcPng)
$DstIco = [IO.Path]::GetFullPath($DstIco)

if (-not (Test-Path $SrcPng)) {
    Write-Error "No se encontro el PNG fuente: $SrcPng"
    exit 1
}

$python = Get-Command python3 -ErrorAction SilentlyContinue
if (-not $python) { $python = Get-Command python -ErrorAction SilentlyContinue }
if (-not $python) {
    Write-Error "Python no encontrado. Instala Python 3 + Pillow (pip install Pillow)."
    exit 1
}

$script = @"
from PIL import Image
src = r'$SrcPng'
dst = r'$DstIco'
img = Image.open(src).convert('RGBA')
img.save(dst, format='ICO', sizes=[(16,16),(32,32),(48,48),(64,64)])
print(f'favicon.ico guardado: {dst}')
"@

Write-Host "Convirtiendo $SrcPng -> $DstIco ..."
$result = $script | & $python.Source -
if ($LASTEXITCODE -eq 0) {
    Write-Host $result
} else {
    Write-Error "Error al convertir. Verifica que Pillow este instalado: pip install Pillow"
    exit 1
}
