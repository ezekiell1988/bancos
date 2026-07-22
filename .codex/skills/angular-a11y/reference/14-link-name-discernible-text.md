# Axe link-name: Links must have discernible text

## Regla

`axe/name-role-value` (link-name) — WCAG 2.4.4 y 4.1.2  
Todo `<a href>` debe tener nombre accesible. MDN: *"If the `<a>` element has an `href` attribute, it represents a hyperlink labeled by its **contents**."*

## Anti-patrones

```html
<!-- MAL: solo icono <i>, sin texto ni aria-label -->
<a href="javascript:;"><i class="fa fa-cog"></i></a>

<!-- MAL: solo &nbsp;, no es texto discernible -->
<a href="javascript:;" class="theme-list-link">&nbsp;</a>

<!-- MAL: solo <span> con CSS background-image, sin texto -->
<a href="..."><span style="background-image: url(...);" class="cover"></span></a>

<!-- MAL: enlace vacío (stretched-link backdrop) -->
<a href="javascript:;" class="stretched-link"></a>

<!-- MAL: [attr.aria-label] binding — no se resuelve en análisis estático -->
<a href="javascript:;" [attr.aria-label]="expr + ' theme'">&nbsp;</a>
```

## Patrones correctos

### 1. `aria-label` literal — para texto fijo

Válido y reconocido por axe tanto en análisis estático como en DOM en vivo.

```html
<a href="javascript:;" aria-label="Toggle theme panel">
  <i class="fa fa-cog"></i>
</a>

<!-- enlaces vacíos (stretched-link): aria-label es la única opción -->
<a href="javascript:;" class="stretched-link" aria-label="Close sidebar"></a>
```

### 2. `visually-hidden` span — para texto dinámico Angular

Cuando el nombre accesible depende de una expresión Angular, `[attr.aria-label]`
**no es fiable**: algunos analizadores (Edge Tools / axe) lo leen como atributo
desconocido antes de que change detection lo resuelva. La solución correcta es
poner el texto en el **contenido** del enlace usando Bootstrap `.visually-hidden`:

```html
<!-- MAL: binding que el analizador no resuelve -->
<a href="javascript:;" [attr.aria-label]="theme + ' theme'">&nbsp;</a>

<!-- BIEN: texto en el contenido del enlace, oculto visualmente -->
<a href="javascript:;" class="theme-list-link" ...>
  <span class="visually-hidden">{{ (theme === 'teal') ? 'Default' : (theme.charAt(0).toUpperCase() + theme.slice(1)) }} theme</span>
</a>
```

> **Regla de oro**: `aria-label` literal = OK siempre. `[attr.aria-label]` dinámico = NO, usar `visually-hidden`.

### 3. `aria-label` en links de imagen CSS

Para links cuyo único contenido es un `<span>` con `background-image`:

```html
<a href="..." class="theme-version-link" aria-label="Material admin theme">
  <span style="background-image: url(...);" class="theme-version-cover"></span>
</a>
```

## Aplicacion en este repo

| Archivo | Línea | Tipo | Fix aplicado |
|---|---|---|---|
| theme-panel.component.html | 2 | icono cog | `aria-label="Toggle theme panel"` |
| theme-panel.component.html | 11–18 | color swatch dinámico | `<span class="visually-hidden">{{ expr }} theme</span>` |
| theme-panel.component.html | 107–228 | CSS background links | `aria-label` descriptivo en cada enlace |
| sidebar.component.html | 180 | icono minimize | `aria-label="Minimize sidebar"` |
| sidebar.component.html | 188 | stretched-link vacío | `aria-label="Close sidebar"` |
| sidebar-right.component.html | 111 | stretched-link vacío | `aria-label="Close right sidebar"` |
| top-menu.component.html | 75 | icono prev | `aria-label="Previous menu items"` |
| top-menu.component.html | 78 | icono next | `aria-label="Next menu items"` |
| panel.component.html | 12–15 | iconos control panel | `aria-label` en Expand/Reload/Collapse/Remove |
| header.component.html | 98 | icono bell | `aria-label="Notifications"` |
| header.component.html | 155 | icono th | `aria-label="Toggle right sidebar"` |

## Por qué no usar `title`

`title` no se reconoce como nombre accesible en la mayoría de los contextos axe.
`aria-label` sobreescribe el nombre accesible de forma explícita y tiene soporte
universal en lectores de pantalla.

## Referencias

- axe rule link-name (Deque):
  https://dequeuniversity.com/rules/axe/4.11/link-name
- MDN `<a>` element:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/a
- WCAG 2.4.4 Link Purpose:
  https://www.w3.org/WAI/WCAG21/Understanding/link-purpose-in-context.html
