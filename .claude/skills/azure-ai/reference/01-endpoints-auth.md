# Endpoints y Autenticación — Azure AI Foundry v1

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/reference
> - https://learn.microsoft.com/azure/ai-foundry/reference/reference-model-inference-api
> - https://platform.openai.com/docs/api-reference (para comparación)

---

## Estructura base de URLs

```
https://{resource-name}.openai.azure.com/openai/v1/{operation}
```

> **CRÍTICO**: La ruta es `/openai/v1/` — **no** `/openai/deployments/{modelo}/` como en la API clásica de Azure OpenAI.  
> **NO** agregar `?api-version=...` — la v1 no usa ese parámetro.

### Endpoints del proyecto ITQS

| Tipo | URL |
|------|-----|
| Base REST | `https://demo-itqs-resource.openai.azure.com/openai/v1` |
| Project API | `https://demo-itqs-resource.services.ai.azure.com/api/projects/demo-itqs` |
| WebSocket realtime | `wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime` |
| WebSocket translate | `wss://demo-itqs-resource.openai.azure.com/openai/v1/realtime/translations` |

---

## Autenticación

### Método: API Key (usado en demos ITQS)

```http
api-key: <valor de apiKey en src/demo/credentials/ai-foundry.json>
```

```powershell
$headers = @{
    'api-key'      = $creds.apiKey
    'Content-Type' = 'application/json'
}
```

### Método: Bearer Token (Entra ID / MSAL — alternativo)

```http
Authorization: Bearer {access_token}
```

Scope requerido: `https://cognitiveservices.azure.com/.default`

> **Fuente**: https://learn.microsoft.com/azure/ai-services/authentication

### WebSocket — cómo pasar la api-key

Para WebSockets en PowerShell/.NET, el header se agrega al `ClientWebSocket` antes de conectar:

```powershell
$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.SetRequestHeader('api-key', $creds.apiKey)
$ws.ConnectAsync([Uri]$wsUri, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
```

> **Nota**: `.GetAwaiter().GetResult()` retorna `VoidTaskResult` en PowerShell — agregar `| Out-Null` para suprimir salida.

---

## Credenciales del proyecto (archivo)

**Ruta**: `src/demo/credentials/ai-foundry.json` *(solo lectura — no modificar)*

```json
{
  "apiKey": "<leer de src/demo/credentials/ai-foundry.json>",
  "projectEndpoint": "https://demo-itqs-resource.services.ai.azure.com/api/projects/demo-itqs",
  "azureOpenAIEndpoint": "https://demo-itqs-resource.openai.azure.com/openai/v1",
  "models": {
    "realtimeTranslation": "gpt-realtime-translate",
    "realtime":            "gpt-realtime",
    "imageGeneration":     "gpt-image-2",
    "videoGeneration":     "sora-2",
    "llm":                 "gpt-5.5"
  }
}
```

```powershell
# Cargar credenciales
$credsPath = Join-Path $PSScriptRoot 'credentials\ai-foundry.json'
$creds     = Get-Content $credsPath -Raw | ConvertFrom-Json
```

---

## Tabla de operaciones REST

| Modelo | Método | Ruta |
|--------|--------|------|
| Chat LLM | POST | `/openai/v1/chat/completions` |
| Imágenes | POST | `/openai/v1/images/generations` |
| Video (crear) | POST | `/openai/v1/videos` |
| Video (estado) | GET | `/openai/v1/videos/{id}` |
| Video (descargar) | GET | `/openai/v1/videos/{id}/download` |
| TTS (voz) | POST | `/openai/v1/audio/speech` |
| STT (transcripción) | POST | `/openai/v1/audio/transcriptions` |
| Realtime (WS) | WSS | `/openai/v1/realtime?model={nombre}` |
| Translate (WS) | WSS | `/openai/v1/realtime/translations?model={nombre}` |

---

## Headers HTTP comunes

```powershell
$headers = @{
    'api-key'      = $creds.apiKey
    'Content-Type' = 'application/json'
}

# Para llamadas con Invoke-RestMethod:
$response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body ($body | ConvertTo-Json -Depth 10)
```

---

## Errores comunes de autenticación / endpoint

| Error | Causa | Solución |
|-------|-------|----------|
| `401 Unauthorized` | API key inválida o header incorrecto | Verificar que sea `api-key:` no `Authorization:` |
| `404 Not Found` | Ruta incorrecta (`/deployments/` en vez de `/v1/`) | Usar rutas v1 sin nombre de despliegue |
| `400 Bad Request` al conectar WS | Endpoint incorrecto (e.g., `/realtime` para translate) | Usar `/realtime/translations` para gpt-realtime-translate |
| `api-version required` | URL incluye `?api-version=...` | Eliminar — v1 no usa api-version |
