#Requires -Version 7.0
<#
.SYNOPSIS
    Ejemplo — gpt-realtime : Chat de Voz en Tiempo Real (WebSocket)
.NOTES
    Credenciales: .local-secrets/ai-foundry.json (raíz del workspace)
    Estructura:   ver .local-secrets/ai-foundry.example.json
.DESCRIPTION
    Usa System.Net.WebSockets.ClientWebSocket (.NET nativo, sin dependencias externas).
    Endpoint: wss://{resource}.openai.azure.com/openai/v1/realtime?model=gpt-realtime

    IMPORTANTE: session.type = 'realtime' es obligatorio en session.update.
    Sin ese campo el server devuelve: "Missing required parameter: session.type"
#>

# ── Cargar credenciales ──────────────────────────────────────────────────────
$credsPath = Join-Path ($PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path) '.local-secrets\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

$model  = $creds.models.realtime
$apiKey = $creds.apiKey
$wsBase = $creds.azureOpenAIEndpoint -replace '^https://', 'wss://'
$wsUri  = [Uri]"$wsBase/realtime?model=$model"

# ── Helpers WebSocket ────────────────────────────────────────────────────────
$ws    = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $apiKey)
$cts   = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(60))
$token = $cts.Token

function Send-WsMessage([System.Net.WebSockets.ClientWebSocket]$socket, [hashtable]$payload) {
    $json  = $payload | ConvertTo-Json -Depth 10
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $seg   = [System.ArraySegment[byte]]::new($bytes)
    $socket.SendAsync($seg, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $token).GetAwaiter().GetResult() | Out-Null
}

function Receive-WsMessage([System.Net.WebSockets.ClientWebSocket]$socket) {
    $buffer = [byte[]]::new(8192)
    $sb     = [System.Text.StringBuilder]::new()
    do {
        $seg    = [System.ArraySegment[byte]]::new($buffer)
        $result = $socket.ReceiveAsync($seg, $token).GetAwaiter().GetResult()
        $sb.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count)) | Out-Null
    } while (-not $result.EndOfMessage)
    return $sb.ToString()
}

# ── Conectar ─────────────────────────────────────────────────────────────────
$ws.ConnectAsync($wsUri, $token).GetAwaiter().GetResult() | Out-Null

# ── 1. Configurar sesión (session.type = 'realtime' es OBLIGATORIO) ──────────
Send-WsMessage $ws @{
    type    = 'session.update'
    session = @{
        type         = 'realtime'   # Campo obligatorio
        instructions = 'Eres un asistente de certificaciones Microsoft. Responde en español.'
    }
}
$ack = Receive-WsMessage $ws | ConvertFrom-Json   # session.updated

# ── 2. Enviar mensaje de texto ────────────────────────────────────────────────
Send-WsMessage $ws @{
    type = 'conversation.item.create'
    item = @{
        type    = 'message'
        role    = 'user'
        content = @( @{ type = 'input_text'; text = '¿Qué cubre el módulo 2 del examen AZ-204?' } )
    }
}

# ── 3. Solicitar respuesta ────────────────────────────────────────────────────
Send-WsMessage $ws @{ type = 'response.create' }

# ── 4. Leer deltas hasta response.done ───────────────────────────────────────
$done = $false
while (-not $done -and $ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $event = Receive-WsMessage $ws | ConvertFrom-Json
    switch ($event.type) {
        'response.text.delta'                        { Write-Host $event.delta -NoNewline }
        'response.output_audio_transcript.delta'     { Write-Host $event.delta -NoNewline }
        'response.done'                              { $done = $true }
        'error'                                      { Write-Error $event.error.message; $done = $true }
    }
}

# ── Cerrar ────────────────────────────────────────────────────────────────────
$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'ok', $token).GetAwaiter().GetResult() | Out-Null
$ws.Dispose()
