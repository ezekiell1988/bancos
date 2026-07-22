---
name: azure-ai
description: >
  Conocimiento exhaustivo de la Azure AI Foundry API v1 con todos los modelos disponibles en el proyecto
  ITQS: gpt-5.5 (LLM reasoning), gpt-image-2 (generación de imágenes), sora-2 (generación de video),
  gpt-realtime (chat de voz WebSocket) y gpt-realtime-translate (traducción de voz WebSocket).
  Usar cuando se trabaje con cualquiera de estos modelos, se escriban scripts de integración,
  se diseñen llamadas REST/WebSocket, se depuren errores de API, o se configure autenticación.
  Triggers: azure ai foundry, gpt-5.5, gpt-image-2, sora-2, gpt-realtime, gpt-realtime-translate,
  azure openai v1, realtime websocket, image generation azure, video generation sora, ai foundry endpoint,
  api-key azure, azure openai chat completion, openai v1 endpoint,
  cleo assets azure, regenerar cleo azure, half-body cleo, medio cuerpo cleo, cleo identity, images edits azure.
---

# Azure AI Foundry — Guía completa de integración (ITQS)

> **Base endpoint**: `https://{resource}.openai.azure.com/openai/v1`  
> **Project endpoint**: `https://{resource}.services.ai.azure.com/api/projects/{project}`  
> Los valores reales de `{resource}` y `{project}` están en `src/demo/credentials/ai-foundry.json`.

Este skill cubre los 5 modelos desplegados en el proyecto ITQS de Azure AI Foundry.
Cada sección de referencia detallada está en la carpeta `reference/` (ver enlaces abajo).

---

## Archivos de referencia

| Archivo | Contenido |
|---------|-----------|
| [reference/01-endpoints-auth.md](./reference/01-endpoints-auth.md) | URLs completas, autenticación, headers HTTP |
| [reference/02-llm-gpt55.md](./reference/02-llm-gpt55.md) | gpt-5.5 — Chat Completions (reasoning) |
| [reference/03-image-gpt-image-2.md](./reference/03-image-gpt-image-2.md) | gpt-image-2 — Generación de imágenes |
| [reference/04-video-sora2.md](./reference/04-video-sora2.md) | sora-2 — Generación de video |
| [reference/05-realtime-websocket.md](./reference/05-realtime-websocket.md) | gpt-realtime — Chat de voz WebSocket |
| [reference/06-translate-websocket.md](./reference/06-translate-websocket.md) | gpt-realtime-translate — Traducción WebSocket |
| [reference/07-powershell-patterns.md](./reference/07-powershell-patterns.md) | Patrones PowerShell para WebSocket y REST |

---

## Reglas generales (siempre aplicar)

1. **Usar API v1**: todas las URLs son `…/openai/v1/…` — **sin** parámetro `api-version` en el query string.
2. **Autenticación**: header `api-key: <key>` — no usar `Authorization: Bearer` ni OAuth para estas llamadas directas.
3. **Modelo en el body**: el nombre del modelo va en el campo `"model"` del request body (no en la URL de operación).
4. **Credenciales**: leer siempre de `src/demo/credentials/ai-foundry.json` — no hardcodear.
   El archivo tiene esta estructura:
   ```json
   {
     "azureOpenAIEndpoint": "https://{resource}.openai.azure.com/openai/v1",
     "apiKey": "...",
     "models": {
       "llm":                "gpt-5.5",
       "imageGeneration":    "gpt-image-2",
       "videoGeneration":    "sora-2",
       "realtime":           "gpt-realtime",
       "realtimeTranslation":"gpt-realtime-translate"
     }
   }
   ```
   En PowerShell: `$creds = Get-Content 'src/demo/credentials/ai-foundry.json' -Raw | ConvertFrom-Json`  
   En JavaScript: `const creds = await fetch('./credentials/ai-foundry.json').then(r => r.json())`

---

## Decisiones clave aprendidas en producción

| Decisión | Razón |
|----------|-------|
| `max_completion_tokens` en gpt-5.5, no `max_tokens` | Modelos de razonamiento usan parámetro diferente |
| No enviar `temperature` con gpt-5.5 | Modelos o1/reasoning no aceptan temperatura |
| Mínimo 2000 tokens en gpt-5.5 | Valor menor falla o produce respuestas truncadas |
| `session.type = 'realtime'` obligatorio | Sin este campo: error "Missing required parameter: session.type" |
| Endpoint separado `/realtime/translations` | El endpoint `/realtime` standard devuelve HTTP 400 para el modelo translate |
| Cerrar sesión translate con `session.close` | El evento `input_audio_buffer.commit` no existe para translation sessions |
| `sora-2` recibe `seconds` como **string** | `'4'`, `'8'`, `'12'` — no como número entero |
| `gpt-image-2` devuelve `b64_json` por defecto | No `url` — hay que decodificar y guardar manualmente |

---

## Ejemplos funcionales (código listo para adaptar)

Los archivos de la carpeta `examples/` son scripts PowerShell completos y comentados,
sin credenciales hardcodeadas, que el agente puede leer directamente para entender
el patrón exacto de cada endpoint.

| Modelo | Script de ejemplo | Puntos clave |
|--------|-------------------|--------------|
| gpt-5.5 (LLM) | [examples/demo-01-llm.ps1](./examples/demo-01-llm.ps1) | `max_completion_tokens`, sin `temperature`, mínimo 2000 tokens |
| gpt-image-2 (imágenes) | [examples/demo-02-image.ps1](./examples/demo-02-image.ps1) | respuesta `b64_json`, decodificar con `[Convert]::FromBase64String` |
| sora-2 (video) | [examples/demo-03-video.ps1](./examples/demo-03-video.ps1) | `seconds` como string, polling GET `/videos/{id}`, descarga `/videos/{id}/content` |
| gpt-realtime (WebSocket voz) | [examples/demo-04-realtime.ps1](./examples/demo-04-realtime.ps1) | `session.type = 'realtime'` obligatorio, eventos `response.text.delta` |
| gpt-realtime-translate (WebSocket traducción) | [examples/demo-05-translate.ps1](./examples/demo-05-translate.ps1) | endpoint `/realtime/translations`, cerrar con `session.close`, evento `session.output_transcript.delta` |
