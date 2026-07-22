---
name: google-gemini-video-research
description: >
  Investiga y prueba generación, edición, extensión e interpolación de videos Google Gemini
  usando de forma segura la configuración por customer de tbClientCustomerGemini. Usar al
  evaluar Gemini Omni Flash, Veo 3.1, Interactions API, videos Google, imágenes de referencia,
  primer/último frame, parámetros de video o edición conversacional. Incluye ejemplos PowerShell
  que no imprimen ni guardan API keys.
---

# Google Gemini Video Research

Usar para investigación aislada antes de integrar un proveedor Google Video en aplicación. Los scripts leen `apiKey` solo en memoria desde `tbClientCustomerGemini`; nunca aceptar, mostrar, registrar ni escribir una key.

## Modelo y ruta recomendada

1. `gemini-omni-flash-preview` + Interactions API: ruta inicial para texto/imagen a video y edición conversacional de un resultado previo o video propio.
2. `veo-3.1-generate-preview` + `predictLongRunning`: usar cuando se necesite extensión de video Veo, interpolación con primer/último frame o hasta tres imágenes de referencia.
3. Ambos son preview. Antes de producción, verificar disponibilidad del customer, región, coste, retención y modelo vigente.

## Seguridad

* Ejecutar solo contra `idClientCustomer` autorizado.
* Los scripts seleccionan solamente `apiKey` en una consulta parametrizada y nunca lo devuelven a consola ni a archivos.
* Guardar videos y JSON de respuesta únicamente en directorio local de pruebas; no subirlos ni agregarlos a Git.
* Una ejecución genera coste. Empezar por `Test-GoogleGeminiVideoAccess.ps1`; usar generación real solo con autorización.

## Ejemplos

* [GoogleGeminiVideo.Common.ps1](./examples/GoogleGeminiVideo.Common.ps1): lectura segura de configuración, MIME, HTTP y extracción de resultados.
* [Test-GoogleGeminiVideoAccess.ps1](./examples/Test-GoogleGeminiVideoAccess.ps1): valida API/modelos disponibles sin generar video.
* [Invoke-GeminiOmniVideo.ps1](./examples/Invoke-GeminiOmniVideo.ps1): texto/imágenes a video y edición por `PreviousInteractionId` o archivo MP4.
* [Invoke-Veo31Video.ps1](./examples/Invoke-Veo31Video.ps1): Veo 3.1 con prompt, imagen inicial, último frame, hasta tres referencias y extensión de un video Veo.
* [Invoke-GoogleVideoSdk.ps1](./examples/Invoke-GoogleVideoSdk.ps1): ruta probada con SDK oficial para imagen a video. Preferirla: la REST legacy de Veo no acepta el mismo contrato de media en todos los tenants.

## Ejecución

```powershell
pwsh .agents/skills/google-gemini-video-research/examples/Test-GoogleGeminiVideoAccess.ps1 -IdClientCustomer 123

pwsh .agents/skills/google-gemini-video-research/examples/Invoke-GoogleVideoSdk.ps1 `
  -IdClientCustomer 123 -Engine Omni -ImagePath .\product.png `
  -Prompt 'Producto sobre fondo de estudio, giro lento' -OutputPath .\out\omni.mp4

pwsh .agents/skills/google-gemini-video-research/examples/Invoke-GoogleVideoSdk.ps1 `
  -IdClientCustomer 123 -Engine Veo31 -ImagePath .\product.png `
  -Prompt 'Seguimiento cinematográfico del producto, sonido ambiente' `
  -AspectRatio 16:9 -Resolution 720p -DurationSeconds 8 -OutputPath .\out\veo.mp4
```

`Veo 3.1` admite máximo tres referencias `asset`; la extensión requiere un video generado por Veo y disponible recientemente. Para editar un video existente usar Omni Flash; si la región/API rechaza el video propio, registrar el error y no hacer fallback silencioso.

## Fuentes oficiales

* [Video generation overview](https://ai.google.dev/gemini-api/docs/video)
* [Gemini Omni Flash generation and editing](https://ai.google.dev/gemini-api/docs/omni)
* [Veo 3.1 generation and controls](https://ai.google.dev/gemini-api/docs/veo)
