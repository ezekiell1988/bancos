# gpt-realtime-translate — Traducción de Voz en Tiempo Real (WebSocket)

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/speech-service/speech-translation-overview
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/realtime-audio
> - https://platform.openai.com/docs/guides/realtime-translations
> - https://platform.openai.com/docs/api-reference/realtime-translations

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `gpt-realtime-translate` |
| Protocolo | WebSocket (WSS) |
| Capacidades | Traducción de voz en tiempo real (speech-to-speech + speech-to-text) |
| Idiomas soportados | Todos los soportados por Azure Speech (50+) |
| Formatos de audio | PCM16, 16 kHz, mono (entrada) / PCM16 24 kHz (salida) |

---

## ⚠️ DIFERENCIA CRÍTICA con gpt-realtime

El modelo `gpt-realtime-translate` usa un **endpoint WebSocket diferente**:

| Modelo | Endpoint |
|--------|----------|
| `gpt-realtime` | `wss://{resource}.openai.azure.com/openai/v1/realtime?model=gpt-realtime` |
| `gpt-realtime-translate` | `wss://{resource}.openai.azure.com/openai/v1/realtime/translations?model=gpt-realtime-translate` |

> Usar el endpoint `/realtime` con `gpt-realtime-translate` resulta en **HTTP 400** al conectar.  
> Verificado en producción ITQS mayo 2026.

---

## Conexión WebSocket

### URL

```
wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime/translations?model=gpt-realtime-translate
```

### Establecer conexión en PowerShell

```powershell
$wsUri = [Uri]"wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime/translations?model=gpt-realtime-translate"

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync($wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
```

---

## Flujo de eventos (secuencia completa)

```
Cliente → Servidor
  1. [connect]                               ← establecer WebSocket
  2. session.update                          ← configurar idioma destino
  3. session.input_audio_buffer.append       ← chunks de audio fuente (loop)
  4. session.close                           ← señalar fin del audio

Servidor → Cliente
  5. session.created                         ← ACK de conexión
  6. session.updated                         ← ACK de configuración
  7. session.input_transcript.delta          ← transcripción del audio fuente
  8. session.output_transcript.delta         ← traducción al idioma destino (TEXTO)
  9. session.output_audio.delta              ← audio traducido (base64 PCM16 24kHz)
 10. session.closed                          ← sesión cerrada por el servidor
```

---

## Eventos del cliente (mensajes a enviar)

### 1. `session.update` — Configurar idioma destino

```json
{
  "type": "session.update",
  "session": {
    "audio": {
      "output": {
        "language": "en"
      }
    }
  }
}
```

> **Nota**: El idioma fuente se detecta automáticamente. Solo se configura el idioma destino.  
> Códigos de idioma: `"en"` (inglés), `"es"` (español), `"fr"` (francés), `"de"` (alemán), etc.  
> Lista completa: https://learn.microsoft.com/azure/ai-services/speech-service/language-support

### 2. `session.input_audio_buffer.append` — Enviar audio

```json
{
  "type": "session.input_audio_buffer.append",
  "audio": "<base64-encoded-PCM16-16kHz-mono>"
}
```

> **Formato de audio de entrada**:
> - Encoding: PCM16 (signed 16-bit little-endian)
> - Sample rate: 16,000 Hz
> - Canales: 1 (mono)
> - Sin headers (raw PCM, no WAV)

```powershell
# Enviar audio en chunks
$chunkSize = 4096
for ($i = 0; $i -lt $audioBytes.Length; $i += $chunkSize) {
    $end    = [Math]::Min($i + $chunkSize, $audioBytes.Length)
    $chunk  = $audioBytes[$i..($end - 1)]
    $b64    = [Convert]::ToBase64String($chunk)
    Send-WsMessage $ws @{
        type  = 'session.input_audio_buffer.append'
        audio = $b64
    }
}
```

### 3. `session.close` — Señalar fin del stream de audio

```json
{
  "type": "session.close"
}
```

> **⚠️ CRÍTICO**: Usar `session.close` para indicar fin del audio.  
> **NO usar** `input_audio_buffer.commit` — ese evento no existe en el contexto de traducción.  
> Tras `session.close`, el servidor procesa el audio restante y envía `session.closed`.

---

## Eventos del servidor (mensajes a recibir)

### `session.created`

Primer evento tras conectar.

```json
{
  "type": "session.created",
  "session": { "id": "trans_abc123", ... }
}
```

### `session.input_transcript.delta` — Transcripción del audio fuente

Texto del audio original (idioma fuente, auto-detectado).

```json
{
  "type": "session.input_transcript.delta",
  "delta": "Buenos días, ¿cómo estás?"
}
```

### `session.output_transcript.delta` — TRADUCCIÓN (texto)

El texto traducido al idioma destino configurado.

```json
{
  "type": "session.output_transcript.delta",
  "delta": "Good morning, how are you?"
}
```

### `session.output_audio.delta` — Audio traducido

Audio de la voz traducida, base64, PCM16 24kHz.

```json
{
  "type": "session.output_audio.delta",
  "delta": "UklGRiQAAABXQVZFZm10IBAAAA..."
}
```

> En demos sin reproducción de audio, ignorar silenciosamente estos eventos.

### `session.closed`

El servidor finalizó el procesamiento.

```json
{
  "type": "session.closed"
}
```

### `error`

```json
{
  "type": "error",
  "error": {
    "type": "invalid_request_error",
    "message": "..."
  }
}
```

---

## Formatos de audio

### Entrada (audio fuente)

| Parámetro | Valor |
|-----------|-------|
| Formato | PCM16 raw (sin header WAV) |
| Sample rate | 16,000 Hz |
| Canales | 1 (mono) |
| Bits | 16 bit signed little-endian |
| Bytes por segundo | 32,000 (1 segundo = 32,000 bytes) |

### Salida (audio traducido)

| Parámetro | Valor |
|-----------|-------|
| Formato | PCM16 raw base64 |
| Sample rate | 24,000 Hz |
| Canales | 1 (mono) |

### Generar audio de silencio (para pruebas)

```powershell
# 1 segundo de silencio PCM16 16kHz mono
$durationSec = 1
$sampleRate  = 16000
$audioBytes  = [byte[]]::new($durationSec * $sampleRate * 2)  # 2 bytes por muestra
```

> **Advertencia**: El silencio puro puede no producir transcripción textual. El modelo genera audio de respuesta (silencio traducido a silencio) pero sin `session.output_transcript.delta`.

### Generar audio de voz real con TTS

```powershell
# Usar el endpoint TTS para generar PCM16 con voz real
$ttsUrl  = "$($creds.azureOpenAIEndpoint)/audio/speech"
$ttsBody = @{
    model           = 'tts-1'
    input           = 'Buenos días, ¿cómo estás hoy?'
    voice           = 'alloy'
    response_format = 'pcm'   # PCM16 sin header
} | ConvertTo-Json

$audioBytes = (Invoke-WebRequest -Uri $ttsUrl -Method POST -Headers $headers -Body $ttsBody).Content
```

---

## Ejemplo completo en PowerShell

```powershell
$creds  = Get-Content 'credentials\ai-foundry.json' -Raw | ConvertFrom-Json
$wsUri  = [Uri]"$($creds.azureOpenAIEndpoint -replace '^https','wss')/realtime/translations?model=$($creds.models.realtimeTranslation)"

# Conexión
$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync($wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null

# Esperar session.created
$raw = Receive-WsMessage $ws   # session.created

# Configurar sesión (ES → EN)
Send-WsMessage $ws @{
    type    = 'session.update'
    session = @{ audio = @{ output = @{ language = 'en' } } }
}

# Cargar audio (PCM16 16kHz mono)
$audioFile  = 'assets\demo-audio.pcm'
$audioBytes = if (Test-Path $audioFile) {
    [IO.File]::ReadAllBytes($audioFile)
} else {
    [byte[]]::new(32000)   # 1s silencio
}

# Enviar audio en chunks
$chunkSize = 4096
for ($i = 0; $i -lt $audioBytes.Length; $i += $chunkSize) {
    $end   = [Math]::Min($i + $chunkSize, $audioBytes.Length)
    $b64   = [Convert]::ToBase64String($audioBytes[$i..($end - 1)])
    Send-WsMessage $ws @{ type = 'session.input_audio_buffer.append'; audio = $b64 }
}

# Señalar fin
Send-WsMessage $ws @{ type = 'session.close' }

# Recibir resultado
$translation = [System.Text.StringBuilder]::new()
$transcript  = [System.Text.StringBuilder]::new()
$audioDeltas = 0
$done        = $false

while (-not $done -and $ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $event = Receive-WsMessage $ws | ConvertFrom-Json
    switch ($event.type) {
        'session.output_audio.delta'      { $audioDeltas++ }
        'session.input_transcript.delta'  { $transcript.Append($event.delta) | Out-Null }
        'session.output_transcript.delta' { $translation.Append($event.delta) | Out-Null }
        'session.closed'                  { $done = $true }
        'error' {
            Write-Host "ERROR: $($event.error.message)"
            $done = $true
        }
    }
}

Write-Host "Transcripción (ES): $($transcript.ToString())"
Write-Host "Traducción    (EN): $($translation.ToString())"
Write-Host "Audio chunks: $audioDeltas"

$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'Fin',
    [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
```

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `HTTP 400` al conectar | Endpoint incorrecto (`/realtime` en vez de `/realtime/translations`) | Usar `/openai/v1/realtime/translations` |
| Sin `session.output_transcript.delta` | Audio de entrada es silencio o ininteligible | Usar audio de voz real (archivo PCM o TTS) |
| Muchos `session.output_audio.delta` sin fin | Loop sin límite esperando `session.closed` | Implementar timeout o detectar `session.closed` |
| `session.input_audio_buffer.commit not found` | Evento incorrecto para finalizar | Usar `session.close` en vez de `input_audio_buffer.commit` |
| `session.update` no reconocido | Formato incorrecto de configuración | Verificar que `session.audio.output.language` sea el path correcto |
