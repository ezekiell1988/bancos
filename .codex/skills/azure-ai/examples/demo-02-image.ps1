#Requires -Version 7.0
<#
.SYNOPSIS
    Ejemplo — gpt-image-2 : Generación de imágenes (modo Generate)
.NOTES
    Credenciales: .local-secrets/ai-foundry.json (raíz del workspace)
    Estructura:   ver .local-secrets/ai-foundry.example.json
.DESCRIPTION
    El modelo devuelve b64_json por defecto (no URL).
    Hay que decodificar y guardar manualmente.
#>

# ── Cargar credenciales ──────────────────────────────────────────────────────
$credsPath = Join-Path ($PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path) '.local-secrets\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

$model   = $creds.models.imageGeneration
$apiKey  = $creds.apiKey
$uri     = "$($creds.azureOpenAIEndpoint)/images/generations"
$headers = @{ 'api-key' = $apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model   = $model
    prompt  = 'A professional digital certification badge for Microsoft Azure, modern flat design, blue and white colors'
    n       = 1
    size    = '1024x1024'
    quality = 'medium'   # gpt-image-2 acepta: low / medium / high / auto
} | ConvertTo-Json -Depth 5

$result = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $body

# La respuesta incluye b64_json — decodificar y guardar
$item = $result.data[0]

if ($item.PSObject.Properties['b64_json'] -and $item.b64_json) {
    $bytes   = [Convert]::FromBase64String($item.b64_json)
    $outPath = 'output-image.png'
    [IO.File]::WriteAllBytes($outPath, $bytes)
    Write-Host "Imagen guardada en: $outPath"
}
elseif ($item.PSObject.Properties['url'] -and $item.url) {
    # Fallback por si el API devuelve URL
    Invoke-WebRequest -Uri $item.url -OutFile 'output-image.png'
    Write-Host "Imagen descargada desde URL."
}

# ── Modo Edición (multipart/form-data) ──────────────────────────────────────
# Para EDITAR una imagen existente, el endpoint es el mismo pero se usa
# Invoke-RestMethod con -Form (multipart). Ver src/demo/demo-02-image.ps1
# para la implementación completa con -Mode Edit, -ImagePath y -MaskPath.
