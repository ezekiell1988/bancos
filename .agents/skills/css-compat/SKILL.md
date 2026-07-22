---
name: css-compat
description: >
  Guía de compatibilidad y modernización CSS para este repo. Usar cuando aparezcan
  advertencias del linter de compatibilidad (Microsoft Edge Tools, Stylelint, VS Code
  compat-api/css), cuando se detecte una propiedad sin prefijo vendor, o cuando se
  quiera asegurar soporte cross-browser en Safari, Firefox y Chrome.
  Triggers: user-select, -webkit-, vendor prefix, safari compat, baseline limited,
  css compat warning, stylelint, animation, appearance, backdrop-filter, gap, grid.
applyTo: "**/*.{css,scss,less}"
---

# CSS Compat

Skill para documentar y corregir incompatibilidades CSS detectadas en este proyecto.
Cada `reference/` cubre un antipatrón real encontrado durante desarrollo o revisión de
herramientas de compatibilidad (Microsoft Edge Tools · compat-api/css · Baseline).

## Objetivo

- Eliminar advertencias de compatibilidad sin romper el diseño.
- Asegurar soporte en Safari 3+, Firefox 2+, Chrome 1+ para propiedades críticas.
- Registrar el patrón `prefijo-vendor + propiedad-estándar` como receta reutilizable.
- Mantener build en verde después de cada corrección.

## Cómo usar este skill

1. Identificar la advertencia: propiedad, browsers afectados y versión mínima requerida.
2. Buscar la reference correspondiente en `reference/`.
3. Aplicar el fix del patrón — siempre primero el prefijo vendor, luego el estándar.
4. Validar con `npm run build` y confirmar que la advertencia desaparece en el linter.
5. Registrar el antipatrón en una nueva reference si no existe.

## Referencias actuales

- [user-select sin -webkit-user-select — Safari 3+](./reference/01-user-select-webkit.md)
- [backdrop-filter sin -webkit-backdrop-filter — Safari 9+](./reference/02-backdrop-filter-webkit.md)

## Regla de oro

Siempre escribir el prefijo vendor **antes** de la propiedad estándar. El orden importa:
el browser aplica la última regla que entiende; si el estándar va primero y el prefixado
después, el prefixado sobreescribe el estándar en browsers que lo necesitan.

```css
/* ✅ CORRECTO — prefijo primero, estándar después */
-webkit-user-select: none;
user-select: none;

/* ❌ INCORRECTO — si el browser entiende ambos, el prefixado sobreescribe */
user-select: none;
-webkit-user-select: none;
```

## Propiedades frecuentes que requieren prefijo -webkit-

| Propiedad estándar        | Prefijo requerido                   | Desde Safari |
|---------------------------|-------------------------------------|-------------|
| `user-select`             | `-webkit-user-select`               | Safari 3    |
| `appearance`              | `-webkit-appearance`                | Safari 3    |
| `backdrop-filter`         | `-webkit-backdrop-filter`           | Safari 9    |
| `text-stroke`             | `-webkit-text-stroke`               | Safari 3    |
| `font-smoothing`          | `-webkit-font-smoothing`            | Safari 3    |
| `tap-highlight-color`     | `-webkit-tap-highlight-color`       | Safari 3    |
| `overflow-scrolling`      | `-webkit-overflow-scrolling`        | Safari 5    |
| `line-clamp`              | `-webkit-line-clamp` (requiere box) | Safari 5    |
