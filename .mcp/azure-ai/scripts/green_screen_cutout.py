#!/usr/bin/env python3
"""Corta el fondo verde chroma-key de una imagen mediante keying determinístico por color
(no segmentación ML) y despilla el verde residual en los bordes.

Uso:
    python3 green_screen_cutout.py <input.png> <output.png> [--report report.json]

Por qué chroma-key por color y no rembg (segmentación por IA):
rembg (u2net) puntualmente le asigna alfa PARCIAL a zonas del sujeto que su modelo de
saliencia considera "menos seguras" (p.ej. zonas con sombreado 3D fuerte cerca del fondo),
lo que produce tenis/guantes semi-transparentes y oscurecidos (RGB premultiplicado por un
alfa bajo) aunque no sean verdes en absoluto. Como el fondo es garantizado un verde sólido
uniforme (se pide explícitamente en el prompt), un keying determinístico por distancia de
color es más confiable: cualquier píxel que NO sea verde queda 100% opaco sin importar cuánta
sombra tenga el render.

Pipeline:
1. Chroma key: por píxel, "exceso de verde" = G - max(R, B). Por encima de un umbral alto es
   fondo puro (alfa 0); por debajo de un umbral bajo es sujeto puro (alfa 255); en el rango
   intermedio se interpola (banda de antialiasing en el borde).
2. Despill: en los píxeles con algo de exceso de verde (banda de borde), se reduce el canal
   verde al máximo de R/B para quitar la fuga de verde sin afectar el resto del sujeto.
3. Validación: se reporta el % de píxeles "verde puro" restantes y el % de alfa opaco.
"""
import sys
import json
import argparse
from PIL import Image
import numpy as np

LOW_THRESHOLD = 10   # exceso de verde por debajo de esto -> sujeto 100% opaco
HIGH_THRESHOLD = 50  # exceso de verde por encima de esto -> fondo 100% transparente


def chroma_key_alpha(rgb: np.ndarray) -> np.ndarray:
    r = rgb[..., 0].astype(np.float32)
    g = rgb[..., 1].astype(np.float32)
    b = rgb[..., 2].astype(np.float32)

    green_excess = g - np.maximum(r, b)

    # alpha=255 cuando green_excess <= LOW, alpha=0 cuando green_excess >= HIGH, interpolado entre medio.
    t = np.clip((green_excess - LOW_THRESHOLD) / (HIGH_THRESHOLD - LOW_THRESHOLD), 0.0, 1.0)
    alpha = ((1.0 - t) * 255.0).astype(np.uint8)
    return alpha, green_excess


def despill_green(rgb: np.ndarray, green_excess: np.ndarray) -> np.ndarray:
    r = rgb[..., 0].astype(np.int16)
    g = rgb[..., 1].astype(np.int16)
    b = rgb[..., 2].astype(np.int16)

    spill = np.clip(green_excess, 0, None)
    g_fixed = np.clip(g - spill, 0, 255).astype(np.uint8)

    out = rgb.copy()
    out[..., 1] = g_fixed
    return out


def green_residue_pct(rgba: np.ndarray) -> float:
    r = rgba[..., 0].astype(np.int16)
    g = rgba[..., 1].astype(np.int16)
    b = rgba[..., 2].astype(np.int16)
    a = rgba[..., 3]
    visible = a > 10
    if visible.sum() == 0:
        return 0.0
    strong_green = (g - np.maximum(r, b) > 30) & visible
    return 100.0 * strong_green.sum() / visible.sum()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("input")
    parser.add_argument("output")
    parser.add_argument("--report", default=None)
    args = parser.parse_args()

    img = Image.open(args.input).convert("RGB")
    rgb = np.array(img)

    alpha, green_excess = chroma_key_alpha(rgb)
    despilled_rgb = despill_green(rgb, green_excess)

    rgba = np.dstack([despilled_rgb, alpha])

    residue_after = green_residue_pct(rgba)
    opaque_pct = 100.0 * (alpha > 10).sum() / alpha.size

    out_img = Image.fromarray(rgba, mode="RGBA")
    out_img.save(args.output)

    report = {
        "input": args.input,
        "output": args.output,
        "size": list(out_img.size),
        "greenResiduePctAfterDespill": round(residue_after, 3),
        "opaquePixelPct": round(opaque_pct, 2),
    }

    if args.report:
        with open(args.report, "w") as f:
            json.dump(report, f, indent=2)

    print(json.dumps(report))


if __name__ == "__main__":
    main()
