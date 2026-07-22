# gpt-realtime — Chat de Voz en Tiempo Real (WebSocket)

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/realtime-audio
> - https://learn.microsoft.com/azure/ai-services/openai/realtime-audio-reference
> - https://platform.openai.com/docs/guides/realtime
> - https://platform.openai.com/docs/api-reference/realtime

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `gpt-realtime` |
| Protocolo | WebSocket (WSS) |
| Capacidades | Chat de voz bidireccional en tiempo real, respuesta en audio + transcripción |
| Latencia | Baja latencia (< 1s típicamente) |

---

## Conexión WebSocket

### URL

```
wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime?model=gpt-realtime
```

> El nombre del modelo va como **query parameter** `?model=`, no en el path ni en el body de conexión.

### Establecer conexión en PowerShell (.NET WebSockets)

```powershell
$wsBase  = 'wss://demo-itqs-resource.openai.azure.com/openai/v1'
$model   = 'gpt-realtime'
$wsUri   = [Uri]"$wsBase/realtime?model=$model"

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync($wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
```

> **⚠️ IMPORTANTE**: `.GetAwaiter().GetResult()` en PowerShell retorna `VoidTaskResult`. Agregar `| Out-Null` para suprimir esa salida en la consola. Aplica a **todos** los awaits (Connect, Send, Receive, Close).

---

## Flujo de eventos (secuencia completa)

```
Cliente → Servidor
  1. [connect]                              ← establecer WebSocket
  2. session.update                         ← configurar sesión (tipo + instrucciones)
  3. conversation.item.create               ← enviar mensaje del usuario
  4. response.create                        ← solicitar respuesta al modelo

Servidor → Cliente
  5. session.created                        ← ACK de conexión
  6. session.updated                        ← ACK de configuración
  7. conversation.item.created              ← ACK del mensaje
  8. response.created                       ← inicio de respuesta
  9. response.output_audio_transcript.delta ← fragmentos de transcripción (TEXTO)
 10. response.output_audio.delta            ← fragmentos de audio (base64 PCM16)
 11. response.done                          ← respuesta completada
```

---

## Eventos del cliente (mensajes a enviar)

### 1. `session.update` — Configurar la sesión

```json
{
  "type": "session.update",
  "session": {
    "type": "realtime",
    "instructions": "Eres un asistente experto en certificaciones Microsoft Azure."
  }
}
```

> **⚠️ CRÍTICO**: El campo `session.type = "realtime"` es **obligatorio**.  
> Sin él: `"Missing required parameter: 'session.type'"` (error confirmado en producción).  
> **NO incluir**: `turn_detection`, `modalities` — no son soportados para `type: "realtime"`.

### 2. `conversation.item.create` — Enviar mensaje del usuario

```json
{
  "type": "conversation.item.create",
  "item": {
    "type": "message",
    "role": "user",
    "content": [
      {
        "type": "input_text",
        "text": "¿Qué temas cubre el módulo 2 del AZ-204?"
      }
    ]
  }
}
```

### 3. `response.create` — Solicitar que el modelo responda

```json
{
  "type": "response.create"
}
```

> Enviar sin parámetros adicionales — no agregar `modalities`, `instructions` u otros.

---

## Eventos del servidor (mensajes a recibir)

### `session.created`

Primer evento recibido al conectar. Contiene configuración inicial.

```json
{
  "type": "session.created",
  "session": { "id": "sess_abc123", ... }
}
```

### `response.output_audio_transcript.delta` ← **TEXTO DE LA RESPUESTA**

Fragmentos de la transcripción del audio generado por el modelo. Este es el **texto de la respuesta**.

```json
{
  "type": "response.output_audio_transcript.delta",
  "delta": "Claro. El módulo 2 del examen AZ-204"
}
```

> **⚠️ CRÍTICO**: La respuesta textual viene en `response.output_audio_transcript.delta`, **no** en `response.text.delta`.  
> El modelo responde en formato audio — la transcripción es el texto de lo que dice.  
> Ignorar `response.output_audio.delta` si no se necesita reproducir audio.

### `response.done`

Indica que la respuesta está completa. Contiene estadísticas de uso.

```json
{
  "type": "response.done",
  "response": {
    "usage": {
      "input_tokens": 45,
      "output_tokens": 380,
      "total_tokens": 425
    }
  }
}
```

### Otros eventos (informacionales)

| Evento | Descripción |
|--------|-------------|
| `session.updated` | ACK de `session.update` |
| `conversation.item.created` | ACK de `conversation.item.create` |
| `response.created` | Inicio de procesamiento |
| `response.output_item.added` | Nuevo item de respuesta iniciado |
| `response.content_part.added` | Nueva parte de contenido |
| `response.output_audio.delta` | Audio PCM16 base64 (opcional, ignorar si no se reproduce) |
| `error` | Error de la API |

---

## Cerrar la sesión

```powershell
$ws.CloseAsync(
    [System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure,
    'Fin',
    [Threading.CancellationToken]::None
).GetAwaiter().GetResult() | Out-Null
```

---

## Ejemplo completo en PowerShell

```powershell
# Helpers de WebSocket
function Send-WsMessage($ws, $obj) {
    $json  = $obj | ConvertTo-Json -Depth 10
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $seg   = [ArraySegment[byte]]::new($bytes)
    $ws.SendAsync($seg, [System.Net.WebSockets.WebSocketMessageType]::Text, $true,
        [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
}

function Receive-WsMessage($ws) {
    $buffer = [byte[]]::new(65536)
    $sb     = [System.Text.StringBuilder]::new()
    do {
        $seg    = [ArraySegment[byte]]::new($buffer)
        $result = $ws.ReceiveAsync($seg, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
        $sb.Append([Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count)) | Out-Null
    } while (-not $result.EndOfMessage)
    return $sb.ToString()
}

# Conexión
$creds  = Get-Content 'credentials\ai-foundry.json' -Raw | ConvertFrom-Json
$wsUri  = [Uri]"$($creds.azureOpenAIEndpoint -replace '^https', 'wss')/realtime?model=$($creds.models.realtime)"
$ws     = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync($wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null

# Esperar session.created
$raw = Receive-WsMessage $ws   # session.created

# Configurar sesión
Send-WsMessage $ws @{
    type    = 'session.update'
    session = @{
        type         = 'realtime'
        instructions = 'Eres un experto en certificaciones Microsoft Azure.'
    }
}

# Enviar mensaje
Send-WsMessage $ws @{
    type = 'conversation.item.create'
    item = @{
        type    = 'message'
        role    = 'user'
        content = @(@{ type = 'input_text'; text = '¿Qué cubre el módulo 2 del AZ-204?' })
    }
}

# Solicitar respuesta
Send-WsMessage $ws @{ type = 'response.create' }

# Recibir y mostrar respuesta
$response = [System.Text.StringBuilder]::new()
$done     = $false
while (-not $done) {
    $event = Receive-WsMessage $ws | ConvertFrom-Json
    switch ($event.type) {
        'response.output_audio_transcript.delta' {
            $response.Append($event.delta) | Out-Null
            Write-Host $event.delta -NoNewline
        }
        'response.done' { $done = $true }
        'error' {
            Write-Host "`n[ERROR] $($event.error.message)"
            $done = $true
        }
    }
}

Write-Host ""
$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'Fin',
    [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
```

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `"Missing required parameter: 'session.type'"` | Falta `type = 'realtime'` en session | Agregar `session.type = "realtime"` en `session.update` |
| `"Unknown parameter: 'session.turn_detection'"` | `turn_detection` no soportado | Eliminar `turn_detection` de session |
| Respuesta vacía / no texto | Capturando `response.text.delta` | Usar `response.output_audio_transcript.delta` |
| `VoidTaskResult` en consola | Await sin `| Out-Null` | Agregar `| Out-Null` a todos los `.GetAwaiter().GetResult()` |
| `401 Unauthorized` | Header de auth incorrecto | Usar `api-key:` header, no `Authorization: Bearer` |
| Timeout / sin respuesta | Buffer de 65536 insuficiente | Aumentar buffer o procesar con loop de chunks |
