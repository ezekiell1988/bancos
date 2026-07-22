# Axe image-alt en templates Angular: img sin texto alternativo

## Anti-patron

Tener etiquetas `img` sin atributo `alt`, o usar binding `[attr.alt]` en lugar de interpolacion literal.

```html
<!-- MAL: sin alt -->
<img src="{{ menu.img }}" />

<!-- MAL: [attr.alt] puede no ser detectado por analizadores estaticos -->
<img src="{{ menu.img }}" [attr.alt]="menu.title ? '' : 'Menu item image'" />
```

## Sintoma

- Microsoft Edge Tools / axe: `Images must have alternative text`
- Regla: `axe/text-alternatives` (image-alt)
- WCAG 2.1 A, criterio 1.1.1 (Non-text Content)

## Causa del falso negativo con `[attr.alt]`

Herramientas como Edge Tools y axe en ciertos contextos evaluan el HTML estatico antes
de que Angular resuelva los bindings. Con `[attr.alt]` el atributo no existe en el
markup inicial, lo cual dispara la alerta aunque en runtime el valor se asigne.
Con interpolacion literal `alt="{{ ... }}"` el atributo **si** aparece en el markup
renderizado y los analizadores lo reconocen correctamente.

## Regla correcta

1. Imagen informativa: `alt` descriptivo.
2. Imagen decorativa (acompana texto visible): `alt=""`.
3. No usar `title` como reemplazo de `alt`.
4. Preferir interpolacion `alt="{{ expr }}"` sobre binding `[attr.alt]="expr"`.

## Aplicacion en este repo

Ambos componentes de menu renderizan iconos de imagen (`menu.img`) junto a texto
visible (`menu.title`), por lo que el icono es decorativo cuando hay titulo.

### top-menu.component.html:9

```html
<div class="menu-icon-img"><img src="{{ menu.img }}" alt="{{ menu.title ? '' : 'Menu item image' }}" /></div>
```

### sidebar.component.html:69

```html
<div class="menu-icon-img"><img src="{{ menu.img }}" alt="{{ menu.title ? '' : 'Menu item image' }}" /></div>
```

- Si existe `menu.title`, `alt` queda vacio (`""`) — decorativo, sin redundancia para lectores de pantalla.
- Si no existe `menu.title`, se aplica fallback corto para evitar violacion de accesibilidad.

## Referencias

- MDN `img`: alt es obligatorio para accesibilidad:
  https://developer.mozilla.org/en-US/docs/Web/HTML/Element/img
- MDN: `title` no sustituye `alt`:
  https://developer.mozilla.org/en-US/docs/Web/HTML/Element/img#the_title_attribute
- axe rule image-alt:
  https://dequeuniversity.com/rules/axe/4.10/image-alt
