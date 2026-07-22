#!/usr/bin/env bash
# Prepara un proyecto Node aislado (fuera del repo) con Playwright + Chromium para
# testear Web2 en un navegador real, sin tocar package.json del proyecto.
#
# USO:
#   .agents/skills/m1on1-web2-browser-test/examples/setup-playwright.sh /ruta/al/scratchpad/pw
#
# Deja el proyecto listo para copiar login-and-test.mjs y correrlo con:
#   node /ruta/al/scratchpad/pw/tu-test.mjs

set -euo pipefail

DIR="${1:?Uso: setup-playwright.sh <directorio-scratchpad>}"
mkdir -p "$DIR"
cd "$DIR"

if [ ! -f package.json ]; then
  npm init -y >/dev/null
fi

npm install playwright@1.61.1 --no-audit --no-fund

# Los browsers de Playwright suelen ya estar cacheados en ~/Library/Caches/ms-playwright
# (o el equivalente de la plataforma) de una instalación previa; este comando es
# no-op rápido en ese caso, o descarga chromium si hace falta.
npx playwright install chromium

echo "Listo. Copia login-and-test.mjs a $DIR y ejecuta con: node $DIR/tu-test.mjs"
