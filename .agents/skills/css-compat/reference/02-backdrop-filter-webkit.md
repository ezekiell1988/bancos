# Antipatrón 02: `backdrop-filter` sin `-webkit-backdrop-filter` — Safari 9+

## Contexto observado

En los archivos SCSS del template Color Admin se usa `backdrop-filter` para efectos
de blur en `.bg-blur-*`, tablas sticky y el header móvil. Microsoft Edge Tools /
compat-api/css reporta la advertencia:

```
'backdrop-filter' is not supported by Safari, Safari on iOS.
Add '-webkit-backdrop-filter' to support Safari 9+, Safari on iOS 9+.
```

## Por qué ocurre

`backdrop-filter` fue marcado como "Newly available" (Baseline 2024).
Safari requirió el prefijo `-webkit-backdrop-filter` hasta versiones recientes;
sin él, el efecto de blur es ignorado silenciosamente en Safari/iOS.

## Impacto

Sin el prefijo, usuarios de Safari/iOS no ven el efecto de blur en headers,
tablas sticky y overlays — comprometiendo la apariencia del layout transparente.

## Fix

Agregar siempre `-webkit-backdrop-filter` **antes** de `backdrop-filter`:

```scss
/* ❌ Antes — blur invisible en Safari/iOS */
backdrop-filter: blur(6px);

/* ✅ Después — funciona en Safari 9+, Safari on iOS 9+, Chrome, Firefox */
-webkit-backdrop-filter: blur(6px);
backdrop-filter: blur(6px);
```

> **Nota:** el valor es idéntico en ambas declaraciones. Basta con duplicar la
> declaración añadiendo el prefijo en la línea anterior.

## Archivos afectados en este proyecto (sesión — 29 abril 2026)

### Clases utilitarias `.bg-blur-*` (6 archivos)
- `src/VoiceBot.Web/src/scss/apple/_helper.scss`
- `src/VoiceBot.Web/src/scss/default/_helper.scss`
- `src/VoiceBot.Web/src/scss/facebook/_helper.scss`
- `src/VoiceBot.Web/src/scss/google/_helper.scss`
- `src/VoiceBot.Web/src/scss/material/_helper.scss`
- `src/VoiceBot.Web/src/scss/transparent/_helper.scss`

```scss
/* Fix aplicado */
.bg-blur-1 { -webkit-backdrop-filter: blur(3px) !important; backdrop-filter: blur(3px) !important; }
.bg-blur-2 { -webkit-backdrop-filter: blur(6px) !important; backdrop-filter: blur(6px) !important; }
.bg-blur-3 { -webkit-backdrop-filter: blur(9px) !important; backdrop-filter: blur(9px) !important; }
```

### Tablas sticky
- `src/VoiceBot.Web/src/scss/transparent/ui/_table.scss`
  - `.table-thead-sticky thead` → `backdrop-filter: blur(8px)`
  - `.table-tfoot-sticky tfoot` → `backdrop-filter: blur(8px)`

```scss
/* Fix aplicado */
-webkit-backdrop-filter: blur(8px);
backdrop-filter: blur(8px);
```

### Header móvil
- `src/VoiceBot.Web/src/scss/transparent/app/_app-header.scss`
  - `.app-header.navbar .navbar-collapse` (mobile) → `backdrop-filter: blur(6px)`

```scss
/* Fix aplicado */
-webkit-backdrop-filter: blur(6px);
backdrop-filter: blur(6px);
```

## Validación

```bash
npm run build   # debe terminar sin warnings de compatibilidad
```

## Referencias externas

- [MDN backdrop-filter](https://developer.mozilla.org/en-US/docs/Web/CSS/backdrop-filter)
- [Can I Use backdrop-filter](https://caniuse.com/css-backdrop-filter)
- [Baseline 2024 — backdrop-filter](https://web.dev/baseline)
