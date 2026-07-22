#Requires -Version 7.0
<#
.SYNOPSIS
    Ejemplo — gpt-5.5 : LLM General (Chat Completion)
.NOTES
    Credenciales: .local-secrets/ai-foundry.json (raíz del workspace)
    Estructura:   ver .local-secrets/ai-foundry.example.json
#>

# ── Cargar credenciales ──────────────────────────────────────────────────────
$credsPath = Join-Path ($PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path) '.local-secrets\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

$model   = $creds.models.llm
$uri     = "$($creds.azureOpenAIEndpoint)/chat/completions"
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model    = $model
    messages = @(
        @{ role = 'system'; content = 'Eres un asistente experto en certificaciones Microsoft.' }
        @{ role = 'user';   content = '¿Cuáles son los 3 temas más importantes del examen AZ-900?' }
    )
    # IMPORTANTE — gpt-5.5 es modelo de razonamiento:
    #   - Usar max_completion_tokens, NO max_tokens
    #   - NO enviar temperature
    #   - Mínimo 2000 tokens o la respuesta puede truncarse
    max_completion_tokens = 2000
} | ConvertTo-Json -Depth 10

$result = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $body

Write-Host $result.choices[0].message.content
Write-Host "Tokens usados: $($result.usage.total_tokens)"
