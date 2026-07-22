---
name: png-resize
description: >
  Verificar dimensiones de PNG y redimensionarlos con sips (macOS nativo) y Pillow (Python).
  Usar cuando se necesite revisar o ajustar el tamaño de assets PNG, iconos de app,
  imágenes de UI o cualquier archivo PNG antes de incluirlo en un build.
  Incluye recorte de whitespace/transparencia con Pillow (-trim equivalente).
  Triggers: png dimensions, resize png, check image size, sips, imagemagick, pillow, icon size,
  asset too large, ajustar tamaño imagen, verificar dimensiones, redimensionar png, recortar png,
  trim whitespace, espaciado transparente, logo padding, crop transparent.
---

# PNG Resize — Quick Reference

## Archivos de este skill

| Archivo | Contenido |
|---------|-----------|
| [examples/resize.ps1](./examples/resize.ps1) | Script principal — check, resize y trim en un solo comando |

---

## Uso rápido

### Verificar dimensiones (sin modificar)

```powershell
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/assets/
```

### Redimensionar in-place

```powershell
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Width 128 -Height 128
```

### Redimensionar a copia (mantiene original)

```powershell
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Width 128 -Height 128 -Out /ruta/output/
```

### Recortar transparencia/whitespace (trim)

```powershell
# Trim in-place
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Trim

# Trim con padding de 8px alrededor del contenido
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Trim -Padding 8
```

### Trim + resize en un solo paso

```powershell
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Trim -Padding 4 -Width 128 -Height 128
```

---

## Referencia de parámetros

| Parámetro | Descripción | Default |
|-----------|-------------|---------|
| `Path` (posicional) | Archivo PNG, carpeta o glob | — |
| `-Width` | Ancho destino en px | 0 (sin resize) |
| `-Height` | Alto destino en px | 0 (sin resize) |
| `-Out` | Archivo o carpeta de salida (no sobreescribe original) | — (in-place) |
| `-Trim` | Recortar transparencia/whitespace con Pillow | false |
| `-Padding` | Píxeles de margen a mantener tras el trim | 0 |

> Si solo se indica `-Width` o solo `-Height`, se usa el mismo valor para ambos (cuadrado).  
> `-Trim` usa `PIL.Image.getbbox()` sobre el canal alfa — requiere `pip install Pillow`.  
> El resize usa `sips` (macOS built-in), no requiere instalación adicional.

---

## Tamaños recomendados para este proyecto

| Uso | Tamaño recomendado | Notas |
|-----|--------------------|-------|
| Iconos circulares mobile (Ionic) | 128×128 px | Se renderizan a ~52px CSS, 2× para retina |
| Splash screen Capacitor | 2732×2732 px | Mínimo para todas las densidades |
| Logo login/header | sin resize fijo | Recortar whitespace primero con `-Trim` |
| Favicon web | 32×32 px | Usar `demo-06-png-to-ico.ps1` para ICO |
| OG image (share) | 1200×630 px | |

---

## Cuándo usar `-Trim`

- PNGs generados por `gpt-image-2` / DALL-E: canvas 1024×1024 con el contenido centrado y mucho padding
- Logos recibidos de diseñadores con whitespace decorativo
- Iconos con bordes transparentes excesivos que obligan a compensar con `height` grande en CSS

---

## Checklist antes de incluir un PNG en el build

```powershell
# 1. Verificar dimensiones y alpha
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png

# 2. Trim si viene de IA (canvas 1024×1024)
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Trim -Padding 4

# 3. Resize al tamaño final
pwsh .agents/skills/png-resize/examples/resize.ps1 /ruta/archivo.png -Width 128 -Height 128

# 4. Confirmar (el script muestra dimensiones finales automáticamente)
```

---

## Notas específicas de este proyecto

- Los PNG generados por `gpt-image-2` vienen en 1024×1024 — siempre hacer `-Trim` antes de commitear
- Assets de MarketingOneOnOneWeb2 van en `src/MarketingOneOnOneWeb2/public/` (Angular los sirve desde `/`)
- Para convertir PNG a `favicon.ico` usar [`demo-06-png-to-ico.ps1`](../.agents/skills/azure-ai/examples/demo-06-png-to-ico.ps1)
