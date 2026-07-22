# Axe button-name: Buttons must have discernible text

## Regla

`axe/name-role-value` (button-name) — WCAG 4.1.2  
MDN: *"The `<button>` element represents a button labeled by its contents."*  
Todo `<button>` debe tener un nombre accesible.

## Anti-patrones

```html
<!-- MAL: solo barras visuales (hamburger), sin texto -->
<button type="button" class="navbar-mobile-toggler">
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
</button>

<!-- MAL: solo icono fa, sin texto -->
<button type="submit" class="btn btn-search">
  <i class="fa fa-search"></i>
</button>
```

## Soluciones (orden de preferencia según MDN)

### 1. Texto visible en el contenido (mejor práctica)

```html
<button type="button">
  <i class="fa fa-search"></i> Search
</button>
```

### 2. `visually-hidden` span — texto oculto visualmente

Cuando el diseño no permite texto visible pero se quiere mantener el texto
en el contenido del botón (semánticamente más robusto que `aria-label`):

```html
<button type="button">
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
  <span class="visually-hidden">Toggle sidebar</span>
</button>
```

### 3. `aria-label` — para icon-only sin cambiar el diseño

Cuando no es posible añadir texto visible ni hidden al contenido:

```html
<button type="button" aria-label="Toggle sidebar">
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
  <span class="icon-bar"></span>
</button>
```

## Aplicacion en este repo (header.component.html)

| Línea | Botón | aria-label |
|---|---|---|
| 6 | `toggleAppSidebarEndMobile()` | `"Toggle right sidebar"` |
| 14 | `toggleAppTopMenuMobile()` (cog icon) | `"Toggle top menu"` |
| 22 | `toggleAppHeaderMegaMenuMobile()` (cog icon) | `"Toggle mega menu"` |
| 30 | `toggleAppSidebarMobile()` | `"Toggle sidebar"` |
| 37 | `toggleAppTopMenuMobile()` (icon bars) | `"Toggle top menu"` |
| 93 | submit search | `"Search"` |

## Referencias

- MDN `<button>` — accessibility concerns:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/button#accessibility_concerns
- axe rule button-name:
  https://dequeuniversity.com/rules/axe/4.11/button-name
- WCAG 4.1.2 Name, Role, Value:
  https://www.w3.org/WAI/WCAG21/Understanding/name-role-value.html
