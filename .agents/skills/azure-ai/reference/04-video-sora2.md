# sora-2 — Generación de Video

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/video-generation
> - https://learn.microsoft.com/azure/ai-services/openai/reference#video-generation
> - https://openai.com/sora (descripción del modelo)

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `sora-2` |
| Familia | Sora (video generation) |
| Capacidades | Generación de video desde texto (text-to-video) |
| Duraciones soportadas | 4s, 8s, 12s |
| Resoluciones soportadas | 1280×720, 720×1280, 1024×1024 (y otras) |
| Latencia típica | 1–5 minutos (operación asíncrona) |

---

## Flujo de operación (asíncrono en 3 pasos)

```
1. POST /openai/v1/videos          → retorna { id, status: "queued" }
2. GET  /openai/v1/videos/{id}     → polling hasta status = "completed"
3. GET  /openai/v1/videos/{id}/download → descarga el archivo de video
```

---

## Paso 1 — Crear trabajo de video

### Endpoint
```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/videos
```

### Request body

```json
{
  "model": "sora-2",
  "prompt": "Una presentación dinámica de certificaciones Microsoft Azure en una oficina moderna",
  "seconds": "8",
  "size": "1280x720",
  "n": 1
}
```

### Parámetros

| Parámetro | Tipo | Obligatorio | Valores | Notas |
|-----------|------|-------------|---------|-------|
| `model` | string | ✅ | `"sora-2"` | — |
| `prompt` | string | ✅ | Descripción del video | Máx ~500 palabras |
| `seconds` | string | ✅ | `"4"`, `"8"`, `"12"` | **⚠️ DEBE ser string, no número** |
| `size` | string | ✅ | `"1280x720"`, `"720x1280"`, `"1024x1024"` | Landscape / Portrait / Square |
| `n` | integer | ❌ | `1` | Solo 1 por ahora |

> **⚠️ CRÍTICO**: `seconds` debe ser un **string** (`"8"`) — si se pasa como número (`8`) la API retorna 400.  
> Verificado en producción ITQS mayo 2026.

### Response (creación)

```json
{
  "id": "video_abc123xyz",
  "status": "queued",
  "created_at": 1748000000
}
```

---

## Paso 2 — Polling del estado

### Endpoint
```
GET https://demo-itqs-resource.openai.azure.com/openai/v1/videos/{id}
```

### Response (en progreso)

```json
{
  "id": "video_abc123xyz",
  "status": "running",
  "created_at": 1748000000
}
```

### Response (completado)

```json
{
  "id": "video_abc123xyz",
  "status": "completed",
  "created_at": 1748000000,
  "completed_at": 1748000300,
  "prompt": "Una presentación dinámica...",
  "size": "1280x720",
  "seconds": "8"
}
```

### Estados posibles

| Estado | Significado |
|--------|-------------|
| `queued` | En cola, esperando procesamiento |
| `running` | Generando el video |
| `completed` | Listo para descargar |
| `failed` | Error en la generación |
| `cancelled` | Cancelado por el usuario |

### Polling en PowerShell

```powershell
do {
    Start-Sleep -Seconds 10
    $status = Invoke-RestMethod -Uri "$url/$jobId" -Headers $headers
    Write-Host "Estado: $($status.status)"
} while ($status.status -notin @('completed', 'failed', 'cancelled'))
```

---

## Paso 3 — Descargar el video

### Endpoint
```
GET https://demo-itqs-resource.openai.azure.com/openai/v1/videos/{id}/content
```

> **⚠️ CRÍTICO**: La ruta correcta es `/content` (sin `/video` al final).  
> - ✅ `/openai/v1/videos/{id}/content` → retorna `video/mp4` (CORRECTO)  
> - ❌ `/openai/v1/videos/{id}/content/video` → 404 Resource not found  
> - ❌ `/openai/v1/videos/{id}/download` → 404 Resource not found  
> Verificado en producción ITQS mayo 2026.

```powershell
Invoke-WebRequest -Uri "$url/$jobId/content" -Headers $headers -OutFile 'assets\demo-video.mp4'
Write-Host "Video guardado."
```

---

## Ejemplo completo en PowerShell

```powershell
$creds   = Get-Content (Join-Path $PSScriptRoot 'credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$url     = "$($creds.azureOpenAIEndpoint)/videos"
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

# Paso 1: Crear trabajo
$body = @{
    model   = $creds.models.videoGeneration   # 'sora-2'
    prompt  = 'Una demostración de certificaciones Microsoft Azure'
    seconds = '8'          # DEBE ser string
    size    = '1280x720'
    n       = 1
} | ConvertTo-Json -Depth 5

$job   = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body
$jobId = $job.id
Write-Host "Job ID: $jobId — Estado: $($job.status)"

# Paso 2: Polling
do {
    Start-Sleep -Seconds 15
    $status = Invoke-RestMethod -Uri "$url/$jobId" -Headers $headers -Method GET
    Write-Host "Estado: $($status.status)"
} while ($status.status -notin @('completed', 'failed', 'cancelled'))

# Paso 3: Descargar (con manejo de 404)
if ($status.status -eq 'completed') {
    try {
        Invoke-WebRequest -Uri "$url/$jobId/download" -Headers $headers -OutFile 'assets\video.mp4'
    } catch {
        Write-Host "Video generado (ID: $jobId) pero descarga no disponible en este tier."
    }
}
```

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `400 - invalid seconds` | `seconds` pasado como número | Usar string: `"8"` no `8` |
| `400 - invalid size` | Formato incorrecto | Usar `"1280x720"` no `"1280 x 720"` |
| `404` en `/content/video` o `/download` | Rutas incorrectas | Usar `/content` (sin sufijo) |
| Timeout en polling | Video muy largo o carga del servicio | Aumentar tiempo entre polls y max intentos |
