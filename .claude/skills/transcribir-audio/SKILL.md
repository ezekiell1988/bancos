---
name: transcribir-audio
description: >
  Transcribe archivos de audio (m4a, mp3, wav, etc.) usando Azure Speech Fast Transcription API
  con diarización automática de hablantes. Genera un Markdown con timestamps por hablante.
  Usar cuando se pida transcribir audio, transcribir reunión, transcribir grabación,
  convertir audio a texto, diarización, identificar hablantes.
  Triggers: transcribir, transcripción, transcribe, audio a texto, m4a a texto,
  meeting transcript, reunión a texto, identificar hablantes, diarización.
---

# Skill: transcribir-audio

Transcribe archivos de audio usando **Azure Speech Fast Transcription API** con diarización.
El resultado es un archivo Markdown con timestamps y etiquetas de hablante (Speaker A, Speaker B…).

## Cuándo usarlo

Activar cuando el usuario pida:
- Transcribir un archivo de audio (m4a, mp3, wav, etc.)
- Convertir una reunión grabada a texto
- Identificar quién habló en una grabación
- Generar un transcript con timestamps

## Prerrequisitos

- **ffmpeg** instalado y en el PATH (`brew install ffmpeg` en macOS / `winget install ffmpeg` en Windows)
- Cuenta de Azure con el servicio **Azure AI Speech** habilitado
- Credenciales configuradas en `examples/.local-secrets` (ver plantilla abajo)

## Configurar credenciales

El script busca `.local-secrets/azure_speech.json` subiendo desde su directorio hasta la raíz del repo.

```bash
# Copiar el ejemplo y rellenar los valores reales
cp .local-secrets/azure_speech.example.json .local-secrets/azure_speech.json
```

El archivo `.local-secrets/azure_speech.json` **nunca debe subirse al repositorio** (está en `.gitignore`).

## Uso del script

Ver el script completo en [examples/transcribir.ps1](./examples/transcribir.ps1).

```powershell
# Forma básica — idioma por defecto es-CR, salida junto al audio
pwsh examples/transcribir.ps1 -AudioPath "mi_reunion.m4a"

# Con idioma y ruta de salida personalizados
pwsh examples/transcribir.ps1 `
    -AudioPath  "recordings/reunion.m4a" `
    -Language   "en-US" `
    -OutputPath "output/reunion_transcript.md"
```

### Parámetros

| Parámetro | Obligatorio | Default | Descripción |
|-----------|-------------|---------|-------------|
| `-AudioPath` | Sí | — | Ruta al archivo de audio |
| `-Language` | No | `es-CR` | Código BCP-47 del idioma |
| `-OutputPath` | No | `{audio}_transcript.md` junto al audio | Ruta del Markdown de salida |

## Pipeline interno

```
Audio (m4a/mp3/wav…)
        │
        ▼
  ffmpeg → WAV 16kHz mono PCM
        │
        ▼
  Azure Speech Fast Transcription API
  (diarización, hasta 35 hablantes, hasta 2 GB)
        │
        ▼
  Markdown con Speaker A/B/C [HH:MM:SS]
```

## Formato de salida

```markdown
# Transcripción — 2026-06-24 18:52

**Fuente:** reunion.m4a
**Hablantes detectados:** 2

---

**Speaker A** [00:00:00]
Texto del primer hablante...

**Speaker B** [00:00:06]
Respuesta del segundo hablante...
```

## Archivos del skill

- [examples/transcribir.ps1](./examples/transcribir.ps1) — script principal
- Credenciales: `.local-secrets/azure_speech.json` en la raíz del repo (gitignored)
