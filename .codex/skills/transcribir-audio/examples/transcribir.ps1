<#
.SYNOPSIS
    Transcribe un archivo de audio usando Azure Speech Fast Transcription API con diarización.

.DESCRIPTION
    1. Convierte el audio a WAV 16kHz mono usando ffmpeg.
    2. Envía el WAV a Azure Speech Fast Transcription API.
    3. Guarda el resultado como Markdown con timestamps y hablantes.

    Las credenciales se leen desde el archivo .local-secrets ubicado en el
    mismo directorio que este script. Ver .local-secrets.template para el formato.

.PARAMETER AudioPath
    Ruta al archivo de audio de entrada (m4a, mp3, wav, ogg, flac, aac, etc.).

.PARAMETER Language
    Código de idioma BCP-47. Por defecto "es-CR".

.PARAMETER OutputPath
    Ruta del archivo Markdown de salida.
    Si se omite, se genera automáticamente junto al audio como {nombre}_transcript.md.

.EXAMPLE
    .\transcribir.ps1 -AudioPath "C:\recordings\reunion.m4a"
    .\transcribir.ps1 -AudioPath "reunion.m4a" -Language "en-US" -OutputPath "C:\out\reunion.md"
#>

param(
    [Parameter(Mandatory = $true, HelpMessage = "Ruta al archivo de audio (m4a, mp3, wav, etc.)")]
    [string]$AudioPath,

    [string]$Language = "es-CR",

    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Helpers ────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) {
    Write-Host "  $msg" -ForegroundColor Cyan
}

function Write-Ok([string]$msg) {
    Write-Host "  ✓ $msg" -ForegroundColor Green
}

function Write-Fail([string]$msg) {
    Write-Host "  ✗ $msg" -ForegroundColor Red
}

# ─── 0. Cargar credenciales desde .local-secrets/azure_speech.json ──────────────────────────────
# Busca subiendo desde el directorio del script hasta encontrar .local-secrets/azure_speech.json

$secretsPath = $null
$search = $PSScriptRoot
while ($search -and $search -ne (Split-Path $search -Parent)) {
    $candidate = Join-Path $search ".local-secrets" "azure_speech.json"
    if (Test-Path $candidate) {
        $secretsPath = $candidate
        break
    }
    $search = Split-Path $search -Parent
}

if (-not $secretsPath) {
    Write-Fail ".local-secrets/azure_speech.json no encontrado (buscado desde $PSScriptRoot hacia la raíz)"
    Write-Host ""
    Write-Host "  Crea el archivo en la raíz del repo:" -ForegroundColor Yellow
    Write-Host "  cp .local-secrets/azure_speech.example.json .local-secrets/azure_speech.json" -ForegroundColor Yellow
    Write-Host "  Y rellena tus credenciales de Azure Speech." -ForegroundColor Yellow
    exit 1
}

$secrets        = Get-Content $secretsPath -Raw | ConvertFrom-Json
$speechKey      = $secrets.AZURE_SPEECH_KEY
$speechEndpoint = $secrets.AZURE_SPEECH_ENDPOINT.TrimEnd("/")

if (-not $speechKey -or -not $speechEndpoint) {
    Write-Fail "AZURE_SPEECH_KEY o AZURE_SPEECH_ENDPOINT no están definidos en .local-secrets"
    exit 1
}

# ─── 1. Validar archivo de audio ─────────────────────────────────────────────

$audioItem = Get-Item -LiteralPath $AudioPath -ErrorAction SilentlyContinue
if (-not $audioItem) {
    Write-Fail "Archivo no encontrado: $AudioPath"
    exit 1
}

Write-Host ""
Write-Host "MeetingRecorder — transcribiendo: " -NoNewline
Write-Host $audioItem.Name -ForegroundColor Cyan
Write-Host ""

# ─── 2. Convertir a WAV 16kHz mono con ffmpeg ────────────────────────────────

Write-Step "Convirtiendo audio a WAV 16kHz mono..."

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Fail "ffmpeg no encontrado. Instálalo con: brew install ffmpeg  (macOS) o winget install ffmpeg (Windows)"
    exit 1
}

$wavPath = [System.IO.Path]::ChangeExtension($audioItem.FullName, ".wav")
if ($audioItem.Extension.ToLower() -eq ".wav") {
    $wavPath = [System.IO.Path]::Combine(
        $audioItem.DirectoryName,
        $audioItem.BaseName + "_16k.wav"
    )
}

$ffmpegOut = & ffmpeg -y -i $audioItem.FullName -ar 16000 -ac 1 -c:a pcm_s16le $wavPath 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Fail "ffmpeg falló (código $LASTEXITCODE)"
    Write-Host ($ffmpegOut | Select-Object -Last 10 | Out-String) -ForegroundColor DarkGray
    exit 1
}

Write-Ok "WAV generado: $wavPath"

# ─── 3. Llamar a Azure Speech Fast Transcription API ─────────────────────────

Write-Step "Enviando a Azure Speech Fast Transcription API..."

$url = "$speechEndpoint/speechtotext/transcriptions:transcribe?api-version=2024-11-15"

$definition = @{
    locales    = @($Language)
    diarization = @{
        enabled    = $true
        maxSpeakers = 35
    }
    properties = @{
        wordLevelTimestampsEnabled = $false
        punctuationMode            = "DictatedAndAutomatic"
        profanityFilterMode        = "None"
    }
} | ConvertTo-Json -Depth 5 -Compress

# Construir multipart/form-data con System.Net.Http para control total de content-types
$boundary   = [System.Guid]::NewGuid().ToString()
$multipart  = [System.Net.Http.MultipartFormDataContent]::new($boundary)

# Parte: definition (application/json)
$defBytes   = [System.Text.Encoding]::UTF8.GetBytes($definition)
$defContent = [System.Net.Http.ByteArrayContent]::new($defBytes)
$defContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("application/json")
$multipart.Add($defContent, "definition")

# Parte: audio (audio/wav)
$audioBytes   = [System.IO.File]::ReadAllBytes($wavPath)
$audioContent = [System.Net.Http.ByteArrayContent]::new($audioBytes)
$audioContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("audio/wav")
$multipart.Add($audioContent, "audio", [System.IO.Path]::GetFileName($wavPath))

# Enviar
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", $speechKey)
$httpClient.Timeout = [System.TimeSpan]::FromMinutes(10)

try {
    $response   = $httpClient.PostAsync($url, $multipart).GetAwaiter().GetResult()
    $body       = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
} finally {
    $httpClient.Dispose()
    $multipart.Dispose()
}

if (-not $response.IsSuccessStatusCode) {
    Write-Fail "Azure Speech error $([int]$response.StatusCode): $body"
    exit 1
}

Write-Ok "Transcripción recibida."

# ─── 4. Parsear respuesta y generar Markdown ──────────────────────────────────

$data      = $body | ConvertFrom-Json
$phrases   = $data.phrases

if (-not $phrases -or $phrases.Count -eq 0) {
    Write-Fail "La respuesta no contiene frases. Verifica el audio."
    exit 1
}

# Ordenar por offset y asignar etiquetas A, B, C...
$utterances = $phrases | ForEach-Object {
    [PSCustomObject]@{
        Speaker = $_.speaker
        OffsetMs = $_.offsetMilliseconds
        Text    = $_.text.Trim()
    }
} | Sort-Object OffsetMs

$speakerIds    = ($utterances | Select-Object -ExpandProperty Speaker -Unique | Sort-Object)
$speakerLabels = @{}
$labelIdx      = 65  # 'A'
foreach ($id in $speakerIds) {
    $speakerLabels[$id] = [char]$labelIdx
    $labelIdx++
}

$timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm")
$lines = @(
    "# Transcripción — $timestamp",
    "",
    "**Fuente:** $($audioItem.Name)",
    "**Hablantes detectados:** $($speakerIds.Count)",
    "",
    "---",
    ""
)

foreach ($u in $utterances) {
    if (-not $u.Text) { continue }
    $totalSec = [int]($u.OffsetMs / 1000)
    $ts = "{0:D2}:{1:D2}:{2:D2}" -f [int]($totalSec / 3600), [int](($totalSec % 3600) / 60), ($totalSec % 60)
    $label = $speakerLabels[$u.Speaker]
    $lines += "**Speaker $label** [$ts]"
    $lines += $u.Text
    $lines += ""
}

$markdown = $lines -join "`n"

# ─── 5. Guardar Markdown ──────────────────────────────────────────────────────

if (-not $OutputPath) {
    $OutputPath = [System.IO.Path]::Combine(
        $audioItem.DirectoryName,
        $audioItem.BaseName + "_transcript.md"
    )
}

$outputDir = Split-Path $OutputPath -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

[System.IO.File]::WriteAllText($OutputPath, $markdown, [System.Text.Encoding]::UTF8)

Write-Ok "Transcripción guardada en: $OutputPath"
Write-Host ""

# Mostrar resumen
Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "  Hablantes : $($speakerIds.Count)" -ForegroundColor White
Write-Host "  Fragmentos: $($utterances.Count)" -ForegroundColor White
Write-Host "  Idioma    : $Language" -ForegroundColor White
Write-Host "  Salida    : $OutputPath" -ForegroundColor White
Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""
