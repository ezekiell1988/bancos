# Antipatrón 01: `user-select` sin `-webkit-user-select` — Safari 3+

## Contexto observado

En CSS de componentes Angular se usa `user-select: none` para deshabilitar la
selección de texto en botones y controles interactivos. Microsoft Edge Tools /
compat-api/css reporta la advertencia:

```
'user-select' is not supported by Safari, Safari on iOS.
Add '-webkit-user-select' to support Safari 3+, Safari on iOS 3+.
```

## Por qué ocurre

`user-select` es una propiedad de disponibilidad limitada (Baseline "Limited").
Safari nunca implementó la versión sin prefijo como estándar hasta versiones muy
recientes. En Safari < 15.4 e iOS Safari, solo funciona `-webkit-user-select`.

## Impacto

Sin el prefijo, los usuarios de Safari/iOS pueden seleccionar texto en botones,
chips y controles de voz — rompiendo la experiencia táctil esperada.

## Fix

Agregar siempre `-webkit-user-select` **antes** de `user-select`:

```css
/* ❌ Antes — solo funciona en Chrome/Firefox */
-webkit-user-select: none;
user-select: none;

/* ✅ Después — funciona en Safari 3+, Safari on iOS 3+, Chrome, Firefox */
-webkit-user-select: none;
user-select: none;
```

> **Nota:** en la mayoría de los casos el valor ya coincide (`none`, `text`, `auto`).
> Basta con duplicar la declaración con el prefijo en la línea anterior.

## Valores soportados

| Valor  | Descripción                                      |
|--------|--------------------------------------------------|
| `none` | Prohíbe selección — usar en botones y controles  |
| `text` | Permite selección normal de texto                |
| `auto` | Comportamiento por defecto del browser           |
| `all`  | Selecciona todo el elemento con un solo clic     |

## Archivos afectados en este proyecto (detectados sesión 17)

- `src/VoiceBot.WebApp/src/app/features/voice-bot/voice-bot.page.css`
  - `.btn` → `user-select: none`
  - `#btn-call.btn-call-main` → `user-select` (hereda de `.btn`)
- `src/VoiceBot.WebApp/src/app/features/voice-bot/mobile/voice-bot.page.css`
  - `.btn` → `user-select: none`

## Fix aplicado

```css
/* Patrón correcto usado en este proyecto desde sesión 17 */
.btn {
  -webkit-user-select: none;
  user-select: none;
}
```

## Validación

```bash
npm run build   # debe terminar sin warnings de compatibilidad
```

## Referencias externas

- [MDN user-select](https://developer.mozilla.org/en-US/docs/Web/CSS/user-select)
- [Can I Use user-select](https://caniuse.com/css-user-select)
- [Baseline Limited — user-select](https://web.dev/baseline)
