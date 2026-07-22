[CmdletBinding()]
param([Parameter(Mandatory)][int]$IdClientCustomer)

. (Join-Path $PSScriptRoot 'GoogleGeminiVideo.Common.ps1')
$config = Get-GeminiCustomerConfig -IdClientCustomer $IdClientCustomer
$models = Invoke-RestMethod -Method Get -Uri 'https://generativelanguage.googleapis.com/v1beta/models' -Headers @{ 'x-goog-api-key' = $config.ApiKey }

Write-Host "Acceso Gemini válido para customer $IdClientCustomer. API key no mostrada." -ForegroundColor Green
Write-Host "Modelo configurado: $($config.DefaultModel)" -ForegroundColor Cyan
$models.models | Where-Object { $_.name -match 'omni|veo' } | Select-Object name, displayName, supportedGenerationMethods | Format-Table -AutoSize
