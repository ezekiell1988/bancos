[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$IdClientCustomer,
    [Parameter(Mandatory)][string]$Prompt,
    [string]$FirstFrame,
    [string]$LastFrame,
    [string[]]$ReferenceImages = @(),
    [string]$ExtendVeoVideo,
    [ValidateSet('16:9','9:16')][string]$AspectRatio = '16:9',
    [ValidateSet('720p','1080p','4k')][string]$Resolution = '720p',
    [ValidateSet('4','6','8')][string]$DurationSeconds = '8',
    [ValidateSet('allow_all','allow_adult')][string]$PersonGeneration = 'allow_all',
    [string]$Model = 'veo-3.1-generate-preview',
    [int]$PollSeconds = 10,
    [Parameter(Mandatory)][string]$OutputPath
)

. (Join-Path $PSScriptRoot 'GoogleGeminiVideo.Common.ps1')
if ($ReferenceImages.Count -gt 3) { throw 'Veo 3.1 permite máximo tres imágenes de referencia.' }
if ($LastFrame -and -not $FirstFrame) { throw 'LastFrame requiere FirstFrame.' }
if ($ExtendVeoVideo -and ($Resolution -ne '720p')) { throw 'Extensión Veo requiere 720p.' }
$config = Get-GeminiCustomerConfig -IdClientCustomer $IdClientCustomer
$instance = @{ prompt = $Prompt }
# Veo REST acepta el shape de GenerateVideos del SDK; no Content.inlineData.
if ($FirstFrame) { $d = Get-GoogleInlineData $FirstFrame; $instance.image = @{ imageBytes = $d.data; mimeType = $d.mimeType } }
if ($LastFrame) { $d = Get-GoogleInlineData $LastFrame; $instance.lastFrame = @{ imageBytes = $d.data; mimeType = $d.mimeType } }
if ($ExtendVeoVideo) { $d = Get-GoogleInlineData $ExtendVeoVideo; $instance.video = @{ videoBytes = $d.data; mimeType = $d.mimeType } }
if ($ReferenceImages.Count) {
    $instance.referenceImages = @($ReferenceImages | ForEach-Object { $d = Get-GoogleInlineData $_; @{ image = @{ imageBytes = $d.data; mimeType = $d.mimeType }; referenceType = 'asset' } })
}
$parameters = @{ aspectRatio = $AspectRatio; resolution = $Resolution; durationSeconds = $DurationSeconds; personGeneration = $PersonGeneration; numberOfVideos = 1 }
$operation = Invoke-GoogleJson -Uri "https://generativelanguage.googleapis.com/v1beta/models/$Model`:predictLongRunning" -ApiKey $config.ApiKey -Body @{ instances = @($instance); parameters = $parameters }
if (-not $operation.name) { throw 'Google no devolvió nombre de operación.' }
Write-Host "Operación Veo creada: $($operation.name)" -ForegroundColor Cyan
do { Start-Sleep -Seconds $PollSeconds; $operation = Invoke-RestMethod -Method Get -Uri "https://generativelanguage.googleapis.com/v1beta/$($operation.name)" -Headers @{ 'x-goog-api-key' = $config.ApiKey } } while (-not $operation.done)
if ($operation.error) { throw "Operación Veo falló: $($operation.error.message)" }
$videoUri = $operation.response.generateVideoResponse.generatedSamples[0].video.uri
if (-not $videoUri) { throw 'Operación completada sin URI de video.' }
Save-GoogleVideoUri -Uri $videoUri -ApiKey $config.ApiKey -OutputPath $OutputPath
$operation | ConvertTo-Json -Depth 30 | Set-Content -Path ([IO.Path]::ChangeExtension($OutputPath, '.operation.json')) -Encoding utf8
Write-Host "Video Veo guardado: $OutputPath" -ForegroundColor Green
