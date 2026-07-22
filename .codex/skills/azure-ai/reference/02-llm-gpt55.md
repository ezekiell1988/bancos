# gpt-5.5 — Chat Completions (Reasoning Model)

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/reasoning
> - https://learn.microsoft.com/azure/ai-services/openai/concepts/models#o-series-models
> - https://platform.openai.com/docs/guides/reasoning

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `gpt-5.5` |
| Familia | o-series / Reasoning models |
| Capacidades | Razonamiento complejo, análisis, código, explicaciones detalladas |
| Contexto máximo | ~128k tokens |

---

## Endpoint

```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/chat/completions
```

---

## Request body

```json
{
  "model": "gpt-5.5",
  "messages": [
    {
      "role": "system",
      "content": "Eres un asistente experto en certificaciones Microsoft Azure."
    },
    {
      "role": "user",
      "content": "Explica qué temas cubre el módulo 2 del examen AZ-204."
    }
  ],
  "max_completion_tokens": 2000
}
```

### Parámetros clave

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `model` | string | ✅ | Nombre del despliegue |
| `messages` | array | ✅ | Array de mensajes con `role` y `content` |
| `max_completion_tokens` | integer | ✅* | Límite de tokens en la respuesta |

> **⚠️ CRÍTICO — Diferencias con modelos GPT estándar**:
> - Usar `max_completion_tokens` en lugar de `max_tokens` — los modelos o-series usan este campo diferente
> - **NO incluir** `temperature` — los modelos de razonamiento no aceptan temperatura y retornan error
> - **NO incluir** `top_p`, `frequency_penalty`, `presence_penalty` — tampoco soportados
> - Valor mínimo recomendado: `2000` — valores menores pueden producir respuestas truncadas o error
> - Fuente: https://learn.microsoft.com/azure/ai-services/openai/how-to/reasoning#api-support

---

## Response body

```json
{
  "id": "chatcmpl-...",
  "object": "chat.completion",
  "created": 1748000000,
  "model": "gpt-5.5",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "El módulo 2 del examen AZ-204 cubre..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 45,
    "completion_tokens": 380,
    "total_tokens": 425
  }
}
```

### Extraer la respuesta en PowerShell

```powershell
$response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body ($body | ConvertTo-Json -Depth 10)
$text     = $response.choices[0].message.content
$tokens   = $response.usage.total_tokens
Write-Host $text
```

---

## Ejemplo completo en PowerShell

```powershell
$creds   = Get-Content (Join-Path $PSScriptRoot 'credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$url     = "$($creds.azureOpenAIEndpoint)/chat/completions"
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model    = $creds.models.llm   # 'gpt-5.5'
    messages = @(
        @{ role = 'system'; content = 'Eres un experto en certificaciones Microsoft.' }
        @{ role = 'user';   content = 'Explica qué cubre el módulo 2 del AZ-204.' }
    )
    max_completion_tokens = 2000
} | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body
Write-Host $response.choices[0].message.content
```

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `400 - max_tokens not supported` | Se usó `max_tokens` en vez de `max_completion_tokens` | Cambiar el nombre del parámetro |
| `400 - temperature not supported` | Se incluyó `temperature` | Eliminar `temperature` del body |
| `400 - unsupported_parameter` | `top_p`, `frequency_penalty`, etc. | Eliminar esos parámetros |
| Respuesta truncada | `max_completion_tokens` muy bajo | Usar mínimo 2000 |
| `400 - content_filter` | Contenido bloqueado por safety | Reformular el prompt |

---

## Roles de mensaje soportados

| Role | Uso |
|------|-----|
| `system` | Instrucciones del sistema / personalidad del asistente |
| `user` | Mensaje del usuario |
| `assistant` | Respuesta previa del modelo (para conversaciones multi-turno) |

> **Nota**: gpt-5.5 también admite `developer` como rol (equivalente a `system` con mayor prioridad).  
> Fuente: https://learn.microsoft.com/azure/ai-services/openai/how-to/reasoning#developer-messages
