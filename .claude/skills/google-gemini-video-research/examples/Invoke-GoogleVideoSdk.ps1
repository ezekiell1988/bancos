<#!
.SYNOPSIS
  Genera video con Veo 3.1 u Omni Flash usando el SDK oficial de Google desde PowerShell.
.DESCRIPTION
  Lee apiKey solo en memoria desde tbClientCustomerGemini. Instala @google/genai en un cache
  temporal si falta; los ejemplos publicados permanecen exclusivamente .ps1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$IdClientCustomer,
    [Parameter(Mandatory)][ValidateSet('Veo31','Omni')][string]$Engine,
    [Parameter(Mandatory)][string]$Prompt,
    [Parameter(Mandatory)][string]$ImagePath,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidateSet('16:9','9:16')][string]$AspectRatio = '16:9',
    [ValidateSet('720p','1080p','4k')][string]$Resolution = '720p',
    [ValidateSet('4','6','8')][string]$DurationSeconds = '8'
)

. (Join-Path $PSScriptRoot 'GoogleGeminiVideo.Common.ps1')
if (-not (Get-Command node -ErrorAction SilentlyContinue) -or -not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'Node.js y npm son requeridos para el SDK oficial @google/genai.'
}
if (-not (Test-Path $ImagePath)) { throw "Imagen no encontrada: $ImagePath" }

$config = Get-GeminiCustomerConfig -IdClientCustomer $IdClientCustomer
$cacheRoot = Join-Path ([IO.Path]::GetTempPath()) 'm1on1-google-genai-sdk'
if (-not (Test-Path (Join-Path $cacheRoot 'node_modules/@google/genai'))) {
    New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
    & npm install --prefix $cacheRoot --no-save '@google/genai'
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar @google/genai en cache temporal.' }
}

$runnerPath = Join-Path $cacheRoot ("video-{0}.mjs" -f [guid]::NewGuid().ToString('N'))
$runner = @'
import fs from 'node:fs';
import { GoogleGenAI } from '@google/genai';

const req = JSON.parse(process.env.M1ON1_GOOGLE_VIDEO_REQUEST);
const ai = new GoogleGenAI({ apiKey: process.env.GEMINI_API_KEY });
const image = { imageBytes: fs.readFileSync(req.imagePath).toString('base64'), mimeType: req.mimeType };
fs.mkdirSync(new URL('.', `file://${req.outputPath}`).pathname, { recursive: true });

if (req.engine === 'Veo31') {
  const operation = await ai.models.generateVideos({
    model: 'veo-3.1-generate-preview', prompt: req.prompt, image,
    config: { aspectRatio: req.aspectRatio, resolution: req.resolution, durationSeconds: Number(req.durationSeconds) },
  });
  console.log(JSON.stringify({ type: 'veo-operation', name: operation.name }));
} else {
  const interaction = await ai.interactions.create({
    model: 'gemini-omni-flash-preview',
    input: [{ type: 'image', data: image.imageBytes, mime_type: image.mimeType }, { type: 'text', text: req.prompt }],
    generation_config: { video_config: { task: 'image_to_video' } },
    response_format: { type: 'video', delivery: 'uri' },
  });
  const video = interaction.outputVideo ?? interaction.steps?.flatMap((step) => step.content ?? []).find((content) => content.type === 'video');
  if (!video) throw new Error(`Omni completed without video. Status: ${interaction.status}`);
  if (video.uri) await ai.files.download({ file: video, downloadPath: req.outputPath });
  else if (video.data) fs.writeFileSync(req.outputPath, Buffer.from(video.data, 'base64'));
  else throw new Error('Omni video has neither URI nor inline data.');
  console.log(JSON.stringify({ type: 'omni-video', interactionId: interaction.id }));
}
'@

$mimeType = Get-GoogleMimeType -Path $ImagePath
$request = @{ engine = $Engine; prompt = $Prompt; imagePath = (Resolve-Path $ImagePath).Path; outputPath = $OutputPath; mimeType = $mimeType; aspectRatio = $AspectRatio; resolution = $Resolution; durationSeconds = $DurationSeconds } | ConvertTo-Json -Compress
$oldKey = $env:GEMINI_API_KEY
$oldRequest = $env:M1ON1_GOOGLE_VIDEO_REQUEST
try {
    [IO.File]::WriteAllText($runnerPath, $runner)
    $env:GEMINI_API_KEY = $config.ApiKey
    $env:M1ON1_GOOGLE_VIDEO_REQUEST = $request
    $result = (& node $runnerPath | Select-Object -Last 1 | ConvertFrom-Json)
    if ($result.type -eq 'veo-operation') {
        Write-Host "Operación Veo creada: $($result.name)" -ForegroundColor Cyan
        $isDone = $false
        do {
            Start-Sleep -Seconds 10
            $operation = Invoke-RestMethod -Method Get -Uri "https://generativelanguage.googleapis.com/v1beta/$($result.name)" -Headers @{ 'x-goog-api-key' = $config.ApiKey }
            $isDone = $operation.PSObject.Properties.Match('done').Count -gt 0 -and [bool]$operation.done
        } while (-not $isDone)
        if ($operation.PSObject.Properties.Match('error').Count) { throw "Veo falló: $($operation.error.message)" }
        $videoUri = $operation.response.generateVideoResponse.generatedSamples[0].video.uri
        if (-not $videoUri) { throw 'Veo terminó sin URI de video.' }
        Save-GoogleVideoUri -Uri $videoUri -ApiKey $config.ApiKey -OutputPath $OutputPath
    }
    Write-Host "Video guardado: $OutputPath" -ForegroundColor Green
} finally {
    Remove-Item -Force -ErrorAction SilentlyContinue $runnerPath
    $env:GEMINI_API_KEY = $oldKey
    $env:M1ON1_GOOGLE_VIDEO_REQUEST = $oldRequest
}
