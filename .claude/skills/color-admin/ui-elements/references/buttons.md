# Reference: Buttons

## Colores de botones (clases)

```html
<!-- Variantes sólidas Color Admin -->
<button class="btn btn-white">White</button>
<button class="btn btn-default">Default</button>
<button class="btn btn-gray">Gray</button>
<button class="btn btn-purple">Purple</button>
<button class="btn btn-indigo">Indigo</button>
<button class="btn btn-primary">Primary</button>
<button class="btn btn-info">Info</button>
<button class="btn btn-yellow">Yellow</button>
<button class="btn btn-warning">Warning</button>
<button class="btn btn-pink">Pink</button>
<button class="btn btn-danger">Danger</button>
<button class="btn btn-success">Success</button>
<button class="btn btn-green">Green</button>
<button class="btn btn-lime">Lime</button>
<button class="btn btn-inverse">Inverse</button>
<button class="btn btn-link">Link</button>

<!-- Outline -->
<button class="btn btn-outline-primary">Outline Primary</button>
```

## Tamaños

```html
<a class="btn btn-primary btn-lg">Large</a>
<a class="btn btn-primary">Default</a>
<a class="btn btn-default btn-sm">Small</a>
<a class="btn btn-default btn-xs">Extra Small</a>
```

## Estados

```html
<a class="btn btn-default disabled">Disabled</a>
<a class="btn btn-default active">Active</a>
<a class="btn btn-primary d-block">Block Button</a>
```

## Button con ícono

```html
<a class="btn btn-default me-5px"><i class="fa fa-comment"></i> Comment</a>
<a class="btn btn-default"><i class="fa fa-cog"></i></a>
<a class="btn btn-default d-block"><i class="fa fa-list"></i> Button block con ícono</a>
```

## Button con contenido complejo (icono grande + texto)

```html
<a class="btn btn-lg btn-primary">
  <span class="d-flex align-items-center text-start">
    <i class="fab fa-twitter fa-3x me-3 text-black"></i>
    <span>
      <span class="d-block"><b>Twitter Bootstrap</b></span>
      <span class="d-block fs-12px opacity-7">Version 5.0</span>
    </span>
  </span>
</a>
```

## Button Groups y Toolbars

```html
<!-- Grupo básico -->
<div class="btn-group">
  <button class="btn btn-white">Left</button>
  <button class="btn btn-white">Middle</button>
  <button class="btn btn-white">Right</button>
</div>

<!-- Toolbar -->
<div class="btn-toolbar">
  <div class="btn-group me-1">
    <button class="btn btn-white">1</button>
    <button class="btn btn-white">2</button>
    <button class="btn btn-white">3</button>
  </div>
  <div class="btn-group">
    <button class="btn btn-white">4</button>
    <button class="btn btn-white">5</button>
  </div>
</div>
```

## Dropdown Buttons

```html
<!-- Dropdown (hacia abajo) -->
<div class="btn-group me-1 mb-1">
  <button class="btn btn-default">Dropdown</button>
  <button class="btn btn-default dropdown-toggle" data-bs-toggle="dropdown">
    <i class="fa fa-caret-down"></i>
  </button>
  <div class="dropdown-menu dropdown-menu-end">
    <a href="javascript:;" class="dropdown-item">Action 1</a>
    <a href="javascript:;" class="dropdown-item">Action 2</a>
    <div class="dropdown-divider"></div>
    <a href="javascript:;" class="dropdown-item">Action 4</a>
  </div>
</div>

<!-- Dropup (hacia arriba) -->
<div class="btn-group dropup me-1 mb-1">
  <button class="btn btn-primary">Dropup</button>
  <button class="btn btn-primary dropdown-toggle" data-bs-toggle="dropdown">
    <i class="fa fa-caret-up"></i>
  </button>
  <div class="dropdown-menu">
    <a href="javascript:;" class="dropdown-item">Action 1</a>
  </div>
</div>
```

## Clases de espaciado típicas para botones

- `me-1 mb-1` — margen derecho y abajo entre botones en fila
- `me-5px` — margen derecho pequeño entre botones inline
- `d-flex flex-wrap` — envolver botones en múltiples líneas
