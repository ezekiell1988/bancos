---
name: rembg-remove-bg
description: >
  Eliminar el fondo de imágenes PNG (blanco, de color o complejo) usando rembg con IA (U2Net).
  Produce PNGs con canal alfa (transparencia). Procesamiento in-place o a carpeta de salida.
  Usar cuando se reciban assets con fondo blanco de ChatGPT/DALL-E/diseñador, cuando se quiera
  transparencia en personajes, logos o ilustraciones, o cuando `filter: drop-shadow` no funcione
  bien sobre un fondo blanco.
  Triggers: quitar fondo, remove background, fondo blanco, transparencia png, rembg, remove bg,
  fondo de imagen, alpha channel, drop-shadow no funciona, imagen con fondo blanco, white background,
  fondo de color, ilustracion sin fondo.
---

# rembg — Remove Background

## Purpose

Eliminar el fondo de PNGs con IA usando el modelo **U2Net** vía la librería `rembg` de Python.
Produce PNGs con canal alfa (RGBA) listos para usar sobre cualquier fondo en web/mobile.

---

## Prerrequisitos

### Verificar instalación de rembg

```bash
python3 -c "import rembg; print('OK')"
```

### Instalar si no está disponible

```bash
# En el entorno Python del proyecto
pip install rembg[cpu]

# Si hay conflicto de entorno virtual
pip install --user rembg[cpu]
```

> **Nota:** La primera ejecución descarga el modelo U2Net (~170 MB) a `~/.u2net/`.
> Las ejecuciones siguientes son instantáneas porque el modelo queda cacheado.

---

## Uso rápido

### Un solo archivo (in-place)

```powershell
pwsh .agents/skills/rembg-remove-bg/examples/remove_bg.ps1 /ruta/imagen.png
```

### Un directorio completo (in-place)

```powershell
pwsh .agents/skills/rembg-remove-bg/examples/remove_bg.ps1 /ruta/assets/
```

### Guardar copia en otra carpeta

```powershell
pwsh .agents/skills/rembg-remove-bg/examples/remove_bg.ps1 /ruta/assets/ -Out /ruta/output/
```

### Solo ciertos archivos con glob

```powershell
pwsh .agents/skills/rembg-remove-bg/examples/remove_bg.ps1 /ruta/assets/cleo/*.png
```

---

## Referencia de parámetros del script

| Parámetro | Descripción | Ejemplo |
|---|---|---|
| `Path` (posicional) | Archivo PNG, carpeta o glob | `/ruta/assets/cleo/` |
| `-Out` | Carpeta de salida (no sobreescribe originals) | `-Out tmp/sin-fondo/` |
| `-Model` | Modelo alternativo (default: `u2net`) | `-Model isnet-general-use` |
| `-DryRun` | Muestra qué archivos procesa sin tocarlos | `-DryRun` |

### Modelos disponibles

| Modelo | Uso ideal |
|---|---|
| `u2net` (default) | Uso general — personajes, objetos, logos |
| `u2net_human_seg` | Personas / personajes humanos |
| `isnet-general-use` | Bordes más nítidos en objetos complejos |
| `silueta` | Siluetas simples, muy rápido |

---

## Workflow recomendado

### 1. Verificar antes de procesar

```bash
# Ver dimensiones y confirmar que son PNGs válidos
sips -g pixelWidth -g pixelHeight /ruta/assets/*.png
```

### 2. Procesar

```powershell
pwsh .agents/skills/rembg-remove-bg/examples/remove_bg.ps1 /ruta/assets/
```

### 3. Verificar resultado visualmente

```bash
# Abrir en Preview (macOS) para confirmar transparencia
open /ruta/assets/imagen.png
```

El damero gris en Preview confirma que el fondo es transparente.

### 4. Si los archivos ya están en dist (Angular), copiarlos

```bash
cp /ruta/src/assets/cleo/*.png /ruta/dist/browser/assets/cleo/
```

O hacer rebuild completo de Angular para que quede permanente:

```bash
cd src/VoiceBot.Web && npm run ng -- build --configuration development
```

---

## Casos de uso en este repo

| Asset | Carpeta | Notas |
|---|---|---|
| Personaje Cleo (19 PNGs) | `src/VoiceBot.Web/src/assets/cleo/` | Procesados en sesión 132 |
| Logos externos recibidos | Cualquier carpeta de assets | Verificar con `sips` antes de procesar |
| Imágenes de productos | `src/VoiceBot.Web/src/assets/` | Si llegan con fondo blanco de diseñador |

---

## Problemas frecuentes

### El modelo corta bordes del personaje

Usar modelo más preciso:
```bash
python3 .agents/skills/rembg-remove-bg/scripts/remove_bg.py /ruta/imagen.png --model isnet-general-use
```

### Queda "halo" blanco alrededor del objeto

Es artefacto del anti-aliasing original. Solución: post-procesar con erosión de 1px:
```python
from PIL import Image, ImageFilter
img = Image.open("imagen.png")
# Erode la máscara alfa levemente
r, g, b, a = img.split()
a = a.filter(ImageFilter.MinFilter(3))
img = Image.merge("RGBA", (r, g, b, a))
img.save("imagen.png")
```

### `rembg` no instalado / error de import

```bash
pip install rembg[cpu]
# Si el entorno virtual del proyecto lo necesita:
source .venv/bin/activate && pip install rembg[cpu]
```

### Primera ejecución muy lenta

Normal: descarga el modelo U2Net (~170 MB). Las siguientes son rápidas.

---

## Referencia adicional

Ver [references/modelos.md](references/modelos.md) para comparativa visual de modelos y cuándo usarlos.
