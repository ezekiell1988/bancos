# Patrones PowerShell para Azure AI Foundry

> Patrones reutilizables verificados en producción para integrar con Azure AI Foundry v1 desde PowerShell.  
> Todos los ejemplos asumen credenciales cargadas desde `credentials\ai-foundry.json`.

---

## Cargar credenciales

```powershell
$credsPath = Join-Path $PSScriptRoot 'credentials\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json

# Campos disponibles:
# $creds.apiKey
# $creds.azureOpenAIEndpoint    → "https://demo-itqs-resource.openai.azure.com/openai/v1"
# $creds.projectEndpoint
# $creds.models.llm             → "gpt-5.5"
# $creds.models.imageGeneration → "gpt-image-2"
# $creds.models.videoGeneration → "sora-2"
# $creds.models.realtime        → "gpt-realtime"
# $creds.models.realtimeTranslation → "gpt-realtime-translate"
```

---

## Headers HTTP para REST

```powershell
$headers = @{
    'api-key'      = $creds.apiKey
    'Content-Type' = 'application/json'
}
```

---

## Llamada REST estándar

```powershell
$body = @{
    model  = 'gpt-5.5'
    # ... otros parámetros
} | ConvertTo-Json -Depth 10   # Depth 10 para objetos anidados

$response = Invoke-RestMethod `
    -Uri     $url `
    -Method  POST `
    -Headers $headers `
    -Body    $body
```

> **Usar `-Depth 10`** en `ConvertTo-Json` cuando hay arrays o hashtables anidados (p.ej. `messages`).  
> El default de depth 2 puede truncar objetos complejos silenciosamente.

---

## Verificar campo opcional en response

```powershell
# CORRECTO — verifica existencia del campo
if ($response.data[0].PSObject.Properties['b64_json']) {
    $bytes = [Convert]::FromBase64String($response.data[0].b64_json)
}

# INCORRECTO — puede fallar si el campo no existe
if ($response.data[0].b64_json -ne $null) { ... }
```

---

## WebSocket — Helpers reutilizables

```powershell
# Enviar mensaje JSON por WebSocket
function Send-WsMessage {
    param(
        [System.Net.WebSockets.ClientWebSocket]$ws,
        [hashtable]$obj
    )
    $json  = $obj | ConvertTo-Json -Depth 10
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $seg   = [ArraySegment[byte]]::new($bytes)
    $ws.SendAsync(
        $seg,
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [Threading.CancellationToken]::None
    ).GetAwaiter().GetResult() | Out-Null
}

# Recibir mensaje JSON por WebSocket (maneja mensajes largos con loop)
function Receive-WsMessage {
    param([System.Net.WebSockets.ClientWebSocket]$ws)
    $buffer = [byte[]]::new(65536)
    $sb     = [System.Text.StringBuilder]::new()
    do {
        $seg    = [ArraySegment[byte]]::new($buffer)
        $result = $ws.ReceiveAsync(
            $seg,
            [Threading.CancellationToken]::None
        ).GetAwaiter().GetResult()
        $sb.Append([Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count)) | Out-Null
    } while (-not $result.EndOfMessage)
    return $sb.ToString()
}
```

> **Buffer de 65536 bytes**: Suficiente para la mayoría de eventos. Para audio puede necesitar más.  
> El loop `do/while` con `EndOfMessage` maneja mensajes WebSocket fragmentados correctamente.

---

## WebSocket — Conexión con api-key header

```powershell
$wsUri = [Uri]"wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime?model=gpt-realtime"

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync($wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
# ↑ | Out-Null suprime "VoidTaskResult" que PowerShell imprime para tareas void
```

---

## WebSocket — Cierre limpio

```powershell
if ($ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $ws.CloseAsync(
        [System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure,
        'Fin de sesión',
        [Threading.CancellationToken]::None
    ).GetAwaiter().GetResult() | Out-Null
}
```

---

## Convertir URL HTTPS a WSS

```powershell
# Para construir URL WebSocket desde azureOpenAIEndpoint (que empieza con https)
$wsBase = $creds.azureOpenAIEndpoint -replace '^https', 'wss'
$wsUri  = [Uri]"$wsBase/realtime?model=$($creds.models.realtime)"
```

---

## Enviar audio en chunks

```powershell
$audioBytes = [IO.File]::ReadAllBytes('assets\demo-audio.pcm')
$chunkSize  = 4096

for ($i = 0; $i -lt $audioBytes.Length; $i += $chunkSize) {
    $end   = [Math]::Min($i + $chunkSize, $audioBytes.Length)
    $chunk = $audioBytes[$i..($end - 1)]
    $b64   = [Convert]::ToBase64String($chunk)
    Send-WsMessage $ws @{
        type  = 'session.input_audio_buffer.append'
        audio = $b64
    }
}
```

---

## Guardar bytes base64 como archivo

```powershell
# Imagen (PNG)
$bytes   = [Convert]::FromBase64String($response.data[0].b64_json)
$outPath = Join-Path $PSScriptRoot 'assets\imagen.png'
[IO.File]::WriteAllBytes($outPath, $bytes)

# Audio (PCM o WAV)
$bytes   = [Convert]::FromBase64String($audioBase64)
$outPath = Join-Path $PSScriptRoot 'assets\audio.pcm'
[IO.File]::WriteAllBytes($outPath, $bytes)
```

---

## Loop de eventos WebSocket hasta condición

```powershell
$done = $false

while (-not $done -and $ws.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
    $raw   = Receive-WsMessage $ws
    $event = $raw | ConvertFrom-Json

    switch ($event.type) {
        'algún.evento.final'  { $done = $true }
        'error' {
            Write-Host "[ERROR] $($event.error.message)" -ForegroundColor Red
            $done = $true
        }
        # Ignorar eventos de audio ruidosos:
        'response.output_audio.delta'    { <# ignorar #> }
        'session.output_audio.delta'     { <# ignorar #> }
    }
}
```

> **Patrón recomendado**: verificar `$ws.State` en la condición del while para evitar llamar `Receive-WsMessage` en un WebSocket cerrado.

---

## Polling asíncrono con intervalo

```powershell
$maxRetries = 30
$retryDelay = 15   # segundos

for ($i = 0; $i -lt $maxRetries; $i++) {
    $status = Invoke-RestMethod -Uri "$url/$jobId" -Headers $headers -Method GET
    Write-Host "[$($i+1)/$maxRetries] Estado: $($status.status)"

    if ($status.status -in @('completed', 'failed', 'cancelled')) { break }
    if ($i -lt $maxRetries - 1) { Start-Sleep -Seconds $retryDelay }
}
```

---

## Manejo de errores en REST

```powershell
try {
    $response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body
} catch [System.Net.WebException] {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $body       = $_.Exception.Response | Get-Member   # para inspeccionar
    Write-Host "HTTP $statusCode — $($_.Exception.Message)"
} catch {
    Write-Host "Error inesperado: $($_.Exception.Message)"
}
```

---

## Mostrar progreso en consola

```powershell
# Texto streaming sin salto de línea (para deltas)
Write-Host $delta -NoNewline

# Colores para diferente información
Write-Host "OK"     -ForegroundColor Green
Write-Host "AVISO"  -ForegroundColor Yellow
Write-Host "ERROR"  -ForegroundColor Red
Write-Host "DEBUG"  -ForegroundColor DarkGray

# Salto de línea explícito tras streaming
Write-Host ""
```

---

## Antipatrones conocidos

| Antipatrón | Problema | Corrección |
|------------|----------|------------|
| `ConvertTo-Json` sin `-Depth` | Arrays/objects anidados se truncan | Usar `-Depth 10` |
| `$ws.ConnectAsync(...) \| Out-Null` omitido | Imprime `VoidTaskResult` en consola | Agregar `\| Out-Null` |
| `$obj.campo -ne $null` para verificar campo | Falla si el campo no existe | Usar `PSObject.Properties['campo']` |
| `input_audio_buffer.commit` en translate | Evento no existe en translation session | Usar `session.close` |
| `max_tokens` con gpt-5.5 | Parámetro no soportado en o-series | Usar `max_completion_tokens` |
| `temperature` con gpt-5.5 | No soportado en reasoning models | Eliminar el parámetro |
| Buffer WebSocket fijo sin loop | Mensajes largos se truncan | Loop con `EndOfMessage` |
