[CmdletBinding(DefaultParameterSetName = 'Generate')]
param(
    [Parameter(Mandatory)][int]$IdClientCustomer,
    [Parameter(Mandatory)][string]$Prompt,
    [string[]]$ReferenceImages = @(),
    [Parameter(ParameterSetName = 'Previous', Mandatory)][string]$PreviousInteractionId,
    [Parameter(ParameterSetName = 'File', Mandatory)][string]$VideoPath,
    [ValidateSet('text_to_video','image_to_video','reference_to_video','edit')][string]$Task,
    [string]$Model = 'gemini-omni-flash-preview',
    [Parameter(Mandatory)][string]$OutputPath
)

. (Join-Path $PSScriptRoot 'GoogleGeminiVideo.Common.ps1')
$config = Get-GeminiCustomerConfig -IdClientCustomer $IdClientCustomer
if ($ReferenceImages.Count -gt 0 -or $VideoPath) {
    $input = @()
    foreach ($image in $ReferenceImages) { $d = Get-GoogleInlineData $image; $input += @{ type = 'image'; data = $d.data; mime_type = $d.mimeType } }
    if ($VideoPath) { $d = Get-GoogleInlineData $VideoPath; $input += @{ type = 'video'; data = $d.data; mime_type = $d.mimeType } }
    $input += @{ type = 'text'; text = $Prompt }
} else { $input = $Prompt }

$body = @{ model = $Model; input = $input; response_format = @{ type = 'video'; delivery = 'uri' } }
if ($PreviousInteractionId) { $body.previous_interaction_id = $PreviousInteractionId }
if ($Task) { $body.generation_config = @{ video_config = @{ task = $Task } } }
$response = Invoke-GoogleJson -Uri 'https://generativelanguage.googleapis.com/v1beta/interactions' -ApiKey $config.ApiKey -Body $body
if ($response.error) { throw "Google Omni rechazó solicitud: $($response.error.message)" }

$video = $response.output_video
foreach ($step in @($response.steps)) { foreach ($content in @($step.content)) { if ($content.type -eq 'video') { $video = $content } } }
if ($null -eq $video) { throw "Respuesta Omni sin video. Estado: $($response.status); interaction: $($response.id)" }
if ($video.uri) { Save-GoogleVideoUri -Uri $video.uri -ApiKey $config.ApiKey -OutputPath $OutputPath }
elseif ($video.data) {
    $folder = Split-Path -Parent $OutputPath
    if ($folder) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
    [IO.File]::WriteAllBytes($OutputPath, [Convert]::FromBase64String($video.data))
}
else { throw 'Video sin URI ni datos inline.' }
$metadataPath = [IO.Path]::ChangeExtension($OutputPath, '.interaction.json')
$response | ConvertTo-Json -Depth 30 | Set-Content -Path $metadataPath -Encoding utf8
Write-Host "Video guardado: $OutputPath" -ForegroundColor Green
Write-Host "Interaction ID para edición posterior: $($response.id)" -ForegroundColor Cyan
