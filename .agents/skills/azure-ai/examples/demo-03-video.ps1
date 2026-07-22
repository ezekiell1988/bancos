#Requires -Version 7.0
<#
.SYNOPSIS
    Ejemplo — sora-2 : Generación de Video (asíncrono con polling)
.NOTES
    Credenciales: .local-secrets/ai-foundry.json (raíz del workspace)
    Estructura:   ver .local-secrets/ai-foundry.example.json
.DESCRIPTION
    Flujo:
      1. POST /openai/v1/videos  → obtiene job ID
      2. GET  /openai/v1/videos/{id}  → polling hasta status = 'completed'
      3. GET  /openai/v1/videos/{id}/content  → descarga el .mp4

    IMPORTANTE: el campo `seconds` va como STRING ('4', '8', '12') — no como número.
#>

# ── Cargar credenciales ──────────────────────────────────────────────────────
$credsPath = Join-Path ($PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path) '.local-secrets\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

$model   = $creds.models.videoGeneration
$baseUri = $creds.azureOpenAIEndpoint
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model   = $model
    prompt  = 'Abstract animation showing cloud computing concepts: data flowing through networks, glowing nodes, blue and white color scheme'
    seconds = '4'           # IMPORTANTE: string, no entero. Valores válidos: '4', '8', '12'
    size    = '1280x720'    # Resoluciones válidas: 720x1280, 1280x720, 1024x1792, 1792x1024
} | ConvertTo-Json -Depth 5

# ── 1. Iniciar generación ────────────────────────────────────────────────────
$result = Invoke-RestMethod -Uri "$baseUri/videos" -Method POST -Headers $headers -Body $body
$jobId  = $result.id
Write-Host "Job iniciado. ID: $jobId — Estado: $($result.status)"

# ── 2. Polling hasta 'completed' ─────────────────────────────────────────────
$maxWait = 15   # 15 × 20 s = 300 s máximo
for ($i = 1; $i -le $maxWait; $i++) {
    Start-Sleep -Seconds 20
    $poll   = Invoke-RestMethod -Uri "$baseUri/videos/$jobId" -Method GET -Headers $headers
    $status = $poll.status
    Write-Host "  [$i/$maxWait] Estado: $status"

    if ($status -eq 'completed') {
        Write-Host "Video completado. Duracion: $($poll.seconds)s  Resolucion: $($poll.size)"
        break
    }
    if ($status -eq 'failed') {
        Write-Error "Generación fallida: $($poll.error)"
        exit 1
    }
}

# ── 3. Descargar video ────────────────────────────────────────────────────────
# GET /openai/v1/videos/{id}/content  → devuelve el .mp4 directamente
$headers_download = @{ 'api-key' = $creds.apiKey }   # sin Content-Type para download
Invoke-WebRequest -Uri "$baseUri/videos/$jobId/content" `
    -Method GET -Headers $headers_download -OutFile 'output-video.mp4'
Write-Host "Video guardado en: output-video.mp4"
