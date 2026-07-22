# gpt-image-2 — Generación de Imágenes

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/images
> - https://learn.microsoft.com/azure/ai-services/openai/reference#image-generation
> - https://platform.openai.com/docs/api-reference/images/create

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `gpt-image-2` |
| Familia | GPT Image |
| Capacidades | Generación de imágenes desde texto, edición de imágenes, variaciones |
| Formatos de salida | `b64_json` (por defecto), `url` (opcional, con expiración) |

---

## Endpoint

```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/images/generations
```

---

## Request body

```json
{
  "model": "gpt-image-2",
  "prompt": "Un diagrama de arquitectura de Azure con servicios conectados...",
  "n": 1,
  "size": "1024x1024",
  "quality": "medium",
  "response_format": "b64_json"
}
```

### Parámetros

| Parámetro | Tipo | Obligatorio | Valores | Default |
|-----------|------|-------------|---------|---------|
| `model` | string | ✅ | `"gpt-image-2"` | — |
| `prompt` | string | ✅ | Descripción de la imagen | — |
| `n` | integer | ❌ | `1`–`10` | `1` |
| `size` | string | ❌ | `"1024x1024"`, `"1792x1024"`, `"1024x1792"` | `"1024x1024"` |
| `quality` | string | ❌ | `"low"`, `"medium"`, `"high"`, `"auto"` | `"auto"` |
| `response_format` | string | ❌ | `"b64_json"`, `"url"` | `"b64_json"` |

> **⚠️ CRÍTICO**: El campo `quality` acepta `low`/`medium`/`high`/`auto` — **no** `standard`/`hd` (esos son de DALL-E 3).  
> Fuente verificada en producción: valor `"medium"` funciona correctamente.
>
> **⚠️ CRÍTICO**: El campo correcto es `output_format` (no `response_format`), y acepta `'png'` o `'jpeg'` — **no** `'b64_json'`.  
> La respuesta sigue llegando como `data[].b64_json` independientemente del valor. Verificado en producción ITQS mayo 2026.

---

## Response body

```json
{
  "created": 1748000000,
  "data": [
    {
      "b64_json": "iVBORw0KGgoAAAANSUhEUgAA...",
      "revised_prompt": "Un diagrama de arquitectura..."
    }
  ]
}
```

> **Nota**: `"url"` no estará presente cuando `response_format = "b64_json"`.  
> El campo `revised_prompt` contiene el prompt modificado por el modelo para mayor calidad.

---

## Decodificar y guardar la imagen en PowerShell

```powershell
$creds   = Get-Content (Join-Path $PSScriptRoot 'credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$url     = "$($creds.azureOpenAIEndpoint)/images/generations"
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model           = $creds.models.imageGeneration   # 'gpt-image-2'
    prompt          = 'Un diagrama de arquitectura Azure con servicios cloud'
    n               = 1
    size            = '1024x1024'
    quality         = 'medium'
    response_format = 'b64_json'
} | ConvertTo-Json -Depth 5

$response  = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body
$imageData = $response.data[0]

# Verificar si es b64_json (puede retornar url en otros casos)
if ($imageData.PSObject.Properties['b64_json']) {
    $bytes    = [Convert]::FromBase64String($imageData.b64_json)
    $outPath  = Join-Path $PSScriptRoot 'assets\demo-image-generated.png'
    [IO.File]::WriteAllBytes($outPath, $bytes)
    Write-Host "Imagen guardada: $outPath"
} elseif ($imageData.PSObject.Properties['url']) {
    Invoke-WebRequest -Uri $imageData.url -OutFile (Join-Path $PSScriptRoot 'assets\demo-image-generated.png')
}
```

> **Patrón de verificación**: usar `$obj.PSObject.Properties['campo']` en lugar de `$obj.campo -ne $null` para evitar falsos negativos en PowerShell cuando el campo no existe vs. cuando es null.

---

## Prompt Tips para mejores resultados

- Incluir estilo: `"estilo minimalista"`, `"flat design"`, `"isometric illustration"`
- Especificar color: `"paleta azul y blanco corporativo"`
- Especificar propósito: `"para presentación ejecutiva"`, `"para documentación técnica"`
- Evitar texto en la imagen (gpt-image-2 puede generarlo pero con errores tipográficos)

---

## Endpoint: Edición con imagen de referencia (`/images/edits`)

Usar cuando se quiere **editar o variar** una imagen existente (ej: cambiar la pose de Cleo manteniendo su estilo).

```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/images/edits
Content-Type: multipart/form-data
```

### Campos del form-data

| Campo | Tipo | Descripción |
|---|---|---|
| `model` | string | `"gpt-image-2"` |
| `image[]` | file (PNG) | Imagen de referencia (ej: `cleo-avatar.png`) |
| `prompt` | string | Descripción de la edición |
| `n` | int | Número de variaciones (default `1`) |
| `size` | string | `"1024x1024"` |
| `quality` | string | `"low"` para mascota — evita timeout |

### Patrón Python (multipart/form-data)

```python
import http.client, json, base64, pathlib, mimetypes

def generate_cleo_halfbody(prompt: str, output_path: str, reference_path: str, api_key: str, endpoint_host: str, endpoint_base: str):
    reference = pathlib.Path(reference_path).read_bytes()
    boundary = "----AzureBoundary7f3a9b"
    crlf = b"\r\n"

    def part_field(name: str, value: str) -> bytes:
        return (f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n').encode()

    def part_file(name: str, filename: str, data: bytes) -> bytes:
        mime = mimetypes.guess_type(filename)[0] or "image/png"
        header = f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"; filename="{filename}"\r\nContent-Type: {mime}\r\n\r\n'
        return header.encode() + data + crlf

    body = (
        part_field("model", "gpt-image-2")
        + part_field("prompt", prompt)
        + part_field("n", "1")
        + part_field("size", "1024x1024")
        + part_field("quality", "low")
        + part_file("image[]", pathlib.Path(reference_path).name, reference)
        + f"--{boundary}--\r\n".encode()
    )

    conn = http.client.HTTPSConnection(endpoint_host)
    conn.request("POST", f"{endpoint_base}/images/edits", body, {
        "Content-Type": f"multipart/form-data; boundary={boundary}",
        "api-key": api_key,
    })
    resp = conn.getresponse()
    data = json.loads(resp.read())
    img_bytes = base64.b64decode(data["data"][0]["b64_json"])
    pathlib.Path(output_path).write_bytes(img_bytes)
    print(f"Guardado: {output_path}")
```

---

## Identidad visual de Cleo (mascota ClickEat)

> Fuente canónica más detallada: `.agents/skills/openai-image-2/reference/cleo-identity.md`

| Rasgo | Descripción |
|---|---|
| **Tipo** | Niña humana joven — ⚠️ nunca un animal/gato |
| **Cabello** | Castaño oscuro en coleta alta |
| **Ojos** | Grandes, oscuros (café/negro) |
| **Mejillas** | Rosadas/ruborizadas |
| **Expresión** | Sonrisa amplia y amigable |
| **Piel** | Clara |
| **Uniforme** | Polo rojo con cuello amarillo, pantalón cargo rojo, cinturón amarillo con hebilla blanca, gorra de béisbol roja con visera |
| **Paleta** | Rojo, amarillo, blanco — sin azul ni verde |
| **Estilo** | Flat 2D cartoon, contornos negros gruesos |

### Bloque de identidad (incluir en TODO prompt de Cleo)

```
A cute cartoon pizza delivery girl mascot named Cleo,
young girl with big dark eyes, rosy cheeks, wide smile, light skin,
dark brown hair in a high ponytail, red baseball cap with visor,
full red uniform with yellow trim: red polo with yellow collar,
red cargo pants, yellow belt with white buckle,
flat 2D illustration style, bold black outlines,
vibrant colors red yellow white, TRANSPARENT BACKGROUND with no fill,
alpha channel, no background color, centered composition,
no text, no shadow, no ground
```

### ADR-76 — Regla de referencia por encuadre

| Encuadre deseado | `image[]` (referencia) |
|---|---|
| **Medio cuerpo** (busto, cabeza + hombros hasta cintura) | `src/VoiceBot.Web/src/assets/cleo/cleo-avatar.png` |
| **Cuerpo completo** (de pie, brazos extendidos) | `src/VoiceBot.Web/src/assets/cleo/cleo.png` |

> **Nunca usar `happy.png`, `pointing-right.png` ni ninguna otra pose como referencia de estilo** — solo `cleo.png` o `cleo-avatar.png`.

### Assets pendientes de regenerar (TASK-ASSETS-CLEO-01)

Todos los siguientes deben regenerarse con `cleo-avatar.png` como referencia (medio cuerpo):

| Asset | Prompt de pose (agregar al bloque de identidad) |
|---|---|
| `idle.png` | `relaxed neutral pose, arms slightly at sides, calm friendly smile, looking forward. HALF-BODY only, NO LEGS, cropped at waist` |
| `happy.png` | `both fists raised in celebration and joy, big happy smile, cheeks flushed. HALF-BODY only, NO LEGS, cropped at waist` |
| `talking.png` | `one hand raised open palm gesturing while speaking, friendly expression. HALF-BODY only, NO LEGS, cropped at waist` |
| `configuring.png` | `holding a clipboard checklist in one hand and a pencil in the other, smiling. HALF-BODY only, NO LEGS, cropped at waist` |
| `curious.png` | `both hands clasped together at chest height, excited curious expression with slight head tilt. HALF-BODY only, NO LEGS, cropped at waist` |
| `pointing-left.png` | `right arm extended pointing to the left, other hand on hip, confident smile. HALF-BODY only, NO LEGS, cropped at waist` |
| `pointing-right.png` | `left arm extended pointing to the right, other hand on hip, confident smile. HALF-BODY only, NO LEGS, cropped at waist` |
| `waving.png` | `one hand raised waving hello, cheerful smile. HALF-BODY only, NO LEGS, cropped at waist` |

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `400 - invalid quality value` | Usar `"standard"` o `"hd"` | Cambiar a `"low"`, `"medium"`, `"high"` o `"auto"` |
| `400 - content_policy_violation` | Prompt con contenido sensible | Reformular sin referencias a personas reales, violencia, etc. |
| `$null` en `b64_json` | Respuesta como URL | Verificar con `PSObject.Properties` antes de acceder |
| Imagen en blanco | Prompt demasiado vago | Agregar más detalle descriptivo al prompt |

---

## Edición de Imágenes (Image Edit API)

> **Fuente oficial**: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/dall-e#call-the-image-edit-api

`gpt-image-2` soporta edición con **inpainting y variaciones** — con capacidad mejorada sobre versiones anteriores.

### Endpoint de edición

```
POST https://{resource}.openai.azure.com/openai/v1/images/edits?api-version=preview
```

> **⚠️ CRÍTICO**: El endpoint de edición usa el mismo patrón `/openai/v1/` que generación,
> pero con `?api-version=preview` — **verificado en producción** para el recurso `demo-itqs-resource`.

Para este proyecto:
```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/images/edits?api-version=preview
```

Constructor: `"$($creds.azureOpenAIEndpoint)/images/edits?api-version=preview"` (sin modificar el endpoint base).

### Headers

```
Content-Type: multipart/form-data
api-key: <apiKey>
```

### Form data (multipart/form-data)

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `image[]` | file (PNG/JPG) | ✅ | Imagen a editar, < 50 MB |
| `prompt` | string | ✅ | Descripción del resultado deseado |
| `model` | string | ✅ | `"gpt-image-2"` |
| `mask` | file (PNG) | ❌ | PNG con áreas transparentes (alpha=0) = zonas a editar (inpainting) |
| `size` | string | ❌ | Resoluciones arbitrarias (múltiplos de 16px, max 3840px) |
| `quality` | string | ❌ | `"low"`, `"medium"`, `"high"` — default `"high"` |
| `n` | integer | ❌ | `1`–`10` imágenes — default `1` |
| `input_fidelity` | string | ❌ | `"high"` / `"low"` — qué tanto preservar rasgos/estilo del original |
| `stream` | boolean | ❌ | `true` para streaming de imágenes parciales |
| `partial_images` | integer | ❌ | `0`–`3` imágenes parciales durante streaming |

> **`input_fidelity`**: `"high"` preserva caras y estilo del original más fielmente; `"low"` permite mayor libertad creativa.

### Response body

```json
{
  "created": 1748000000,
  "data": [
    {
      "b64_json": "iVBORw0KGgoAAAANSUhEUgAA..."
    }
  ]
}
```

> La edición siempre devuelve `b64_json` — no hay opción de URL en el endpoint de edición.

### Ejemplo PowerShell 7 (multipart/form-data)

```powershell
$creds    = Get-Content (Join-Path $PSScriptRoot 'credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$model   = $creds.models.imageGeneration   # 'gpt-image-2'
$editUrl = "$($creds.azureOpenAIEndpoint)/images/edits?api-version=preview"
$headers  = @{ 'api-key' = $creds.apiKey }

$imagePath = 'C:\ruta\imagen-original.png'
$imageStream = [System.IO.File]::OpenRead($imagePath)

# Usar HttpClient para controlar MIME type (FileInfo con -Form envía octet-stream
# pero la API requiere image/png o image/jpeg)
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.DefaultRequestHeaders.Add('api-key', $apiKey)

$multipart  = [System.Net.Http.MultipartFormDataContent]::new()
$imgBytes   = [System.IO.File]::ReadAllBytes($imagePath)
$imgContent = [System.Net.Http.ByteArrayContent]::new($imgBytes)
$imgContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
$multipart.Add($imgContent, 'image[]', 'image.png')
$multipart.Add([System.Net.Http.StringContent]::new('Add a glowing Azure logo in the sky'), 'prompt')
$multipart.Add([System.Net.Http.StringContent]::new($model),  'model')
$multipart.Add([System.Net.Http.StringContent]::new('high'),  'quality')
$multipart.Add([System.Net.Http.StringContent]::new('high'),  'input_fidelity')
$multipart.Add([System.Net.Http.StringContent]::new('1'),     'n')

$resp      = $httpClient.PostAsync($editUrl, $multipart).GetAwaiter().GetResult()
$jsonStr   = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
$httpClient.Dispose()
$response  = $jsonStr | ConvertFrom-Json

$bytes   = [Convert]::FromBase64String($response.data[0].b64_json)
$outPath = Join-Path $PSScriptRoot 'assets\demo-image-edited.png'
[IO.File]::WriteAllBytes($outPath, $bytes)
Write-Host "Imagen editada guardada: $outPath"
```

> **⚠️ MIME type**: `[System.IO.FileInfo]` en `-Form` de `Invoke-RestMethod` envía `application/octet-stream`.
> Usar `HttpClient` + `ByteArrayContent` con `ContentType = image/png` para que la API lo acepte.

### Errores específicos de edición

| Error | Causa | Solución |
|-------|-------|----------|
| `415 - Unsupported Media Type` | Enviar JSON en vez de multipart | Usar `-Form` (PS) o `FormData` (JS) |
| `400 - image too large` | Imagen > 50 MB | Comprimir o reducir resolución |
| `400 - invalid image format` | Formato distinto a PNG/JPG | Convertir a PNG o JPG |
| `404 - deployment not found` | URL de deployment incorrecta | Verificar nombre del deployment en Azure Foundry |
