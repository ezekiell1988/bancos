# Antipatron 08: HTML meta elements — duplicado charset y viewport restrictivo

## Contexto

Moderno HTML y best practices de accesibilidad requieren:
1. **Máximo una declaración de charset** por documento (WHATWG spec)
2. **Viewport sin restricciones extremas** (`maximum-scale`, `user-scalable=no`)

## Síntoma

**En index.html:**
```html
<!-- Error 1: Duplicado charset -->
<meta charset="utf-8" />
<meta http-equiv="content-type" content="text/html; charset=UTF8">

<!-- Error 2: Viewport restrictivo -->
<meta content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" name="viewport" />
```

**Errores reportados por Edge Tools / W3C validators:**
- `'charset' meta element is not needed as one was already specified`
- `The 'viewport' meta element 'content' attribute value should not contain 'maximum-scale'`
- `The 'viewport' meta element 'content' attribute value should not contain 'user-scalable'`

## Impacto

- **Duplicado charset**: Confusión en parsers HTML, redundancia innecesaria
- **maximum-scale**: Viola WCAG 2.1 (impide zoom del usuario); muchos navegadores lo ignoran
- **user-scalable=no**: Accesibilidad degradada; usuarios no pueden hacer zoom si lo necesitan

## Regla de detección

- `<meta charset="...">` + `<meta http-equiv="content-type" ...charset=...>` en el mismo documento
- Viewport contiene `maximum-scale=` o `user-scalable=`

## Fix recomendado

### Opción A: Una sola declaración charset

**Antes:**
```html
<meta charset="utf-8" />
<meta http-equiv="content-type" content="text/html; charset=UTF8">
```

**Después:**
```html
<meta charset="utf-8" />
<!-- Remover la línea de http-equiv="content-type" -->
```

**Nota:** `charset` es el método moderno (HTML5+). `http-equiv="content-type"` es legacy.

### Opción B: Viewport accesible y moderno

**Antes:**
```html
<meta content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" name="viewport" />
```

**Después:**
```html
<meta content="width=device-width, initial-scale=1.0" name="viewport" />
```

**Propiedades permitidas y recomendadas:**
- `width=device-width` — ancho = width del dispositivo
- `initial-scale=1.0` — zoom inicial = 100%
- `viewport-fit=cover` (opcional, para notch/safe areas en iOS)

**Propiedades NO recomendadas:**
- ~~`maximum-scale`~~ — impide que el usuario haga zoom (accesibilidad ❌)
- ~~`minimum-scale`~~ — similar problema
- ~~`user-scalable=no`~~ — igual

## Validación

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Referencias

- [WHATWG HTML Spec - Meta charset](https://html.spec.whatwg.org/multipage/semantics.html#charset)
- [MDN - Meta viewport](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta#viewport)
- [WCAG 2.1 - Resize text](https://www.w3.org/WAI/WCAG21/Understanding/resize-text.html)
- [Web.dev - Viewport best practices](https://web.dev/articles/responsive-web-design-basics)

## Notas

- HTML5 spec permite solo una declaración charset.
- La mayoría de navegadores modernos **ignoran o desalientan** `maximum-scale` y `user-scalable` por razones de accesibilidad.
- El viewport moderno recomendado es simplemente: `width=device-width, initial-scale=1.0`.
