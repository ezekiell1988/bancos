#Requires -Version 7.0
<#
.SYNOPSIS
    Ejemplo — gpt-realtime-translate : Traducción en Tiempo Real (WebSocket)
.NOTES
    Credenciales: .local-secrets/ai-foundry.json (raíz del workspace)
    Estructura:   ver .local-secrets/ai-foundry.example.json
.DESCRIPTION
    Usa System.Net.WebSockets.ClientWebSocket (.NET nativo, sin dependencias externas).

    DIFERENCIAS respecto a gpt-realtime:
      - Endpoint: /realtime/translations (NO /realtime — ese devuelve HTTP 400 para este modelo)
      - Finalizar sesión con: session.close (NO input_audio_buffer.commit)
      - Eventos de salida:   session.output_transcript.delta (texto traducido)
                             session.input_transcript.delta  (transcripción original)
                             session.output_audio.delta      (audio traducido en base64)
      - Fin de sesión con evento: session.closed
#>

# ── Cargar credenciales ──────────────────────────────────────────────────────
$credsPath = Join-Path ($PSScriptRoot | Split-Path | Split-Path | Split-Path | Split-Path) '.local-secrets\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

$model  = $creds.models.realtimeTranslation
$apiKey = $creds.apiKey
$wsBase = $creds.azureOpenAIEndpoint -replace '^https://', 'wss://'
# IMPORTANTE: endpoint /realtime/translations — no /realtime
$wsUri  = [Uri]"$wsBase/realtime/translations?model=$model"

# ── Audio de entrada (PCM16, 16 kHz) ─────────────────────────────────────────
# Opción A: cargar archivo real
# $audioBytes = [IO.File]::ReadAllBytes('assets\demo-audio.pcm')
# Opción B: 1 segundo de silencio sintético para probar la conexión
$audioBytes = [byte[]]::new(32000)   # 1 s de silencio PCM16 @ 16 kHz

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

# ── 1. Leer session.created (el server lo envía automáticamente al conectar) ──
$ack = Receive-WsMessage $ws | ConvertFrom-Json   # session.created

# ── 2. Configurar sesión: idioma destino de la traducción ────────────────────
Send-WsMessage $ws @{
    type    = 'session.update'
    session = @{
        audio = @{
            output = @{ language = 'en' }   # traducir a inglés
        }
    }
}

# ── 3. Enviar audio en chunks de 4 096 bytes (base64) ────────────────────────
$chunkSize = 4096
$offset    = 0
while ($offset -lt $audioBytes.Length) {
    $end   = [Math]::Min($offset + $chunkSize, $audioBytes.Length)
    $chunk = $audioBytes[$offset..($end - 1)]
    Send-WsMessage $ws @{ type = 'session.input_audio_buffer.append'; audio = [Convert]::ToBase64String($chunk) }
    $offset = $end
}

# IMPORTANTE: cerrar con session.close — input_audio_buffer.commit no existe en translation sessions
Send-WsMessage $ws @{ type = 'session.close' }

# ── 4. Leer eventos hasta session.closed ─────────────────────────────────────
$done = $false
while (-not $done -and $ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $event = Receive-WsMessage $ws | ConvertFrom-Json
    switch ($event.type) {
        'session.output_transcript.delta'   { Write-Host $event.delta -NoNewline }   # texto traducido
        'session.input_transcript.delta'    { <# transcripción original — ignorar o mostrar #> }
        'session.output_audio.delta'        { <# audio traducido base64 PCM16 — guardar si se necesita #> }
        'session.closed'                    { $done = $true }
        'error'                             { Write-Error $event.error.message; $done = $true }
        'session.updated'                   { <# ACK — ignorar #> }
    }
}

$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'ok', $token).GetAwaiter().GetResult() | Out-Null
$ws.Dispose()
