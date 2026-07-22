# Reference: Typography

## Headings

```html
<h1>h1. Heading 1</h1>
<h2>h2. Heading 2</h2>
<h3>h3. Heading 3</h3>
<h4>h4. Heading 4</h4>
<h5>h5. Heading 5</h5>
<h6>h6. Heading 6</h6>
```

## Texto especial

```html
<small>Fine print / texto pequeño</small>
<em>rendered as italicized text</em>
<span class="semi-bold">rendered as semi bold text</span>
<strong>rendered as bold text</strong>
<p class="lead">Texto lead / destacado</p>
```

## Alineación

```html
<p class="text-start">Left aligned text.</p>
<p class="text-center">Center aligned text.</p>
<p class="text-end">Right aligned text.</p>
```

## Clases de énfasis / color de texto

```html
<p class="text-muted">Texto apagado/gris</p>
<p class="text-warning">Texto warning</p>
<p class="text-danger">Texto danger</p>
<p class="text-info">Texto info</p>
<p class="text-success">Texto success</p>
<p class="text-primary">Texto primary</p>
<p class="text-dark">Texto dark</p>
<p class="text-white">Texto white</p>
```

## Listas

```html
<!-- Lista desordenada estándar -->
<ul>
  <li>Item 1</li>
  <li>Item con hijos
    <ul>
      <li>Sub-item</li>
    </ul>
  </li>
</ul>

<!-- Lista ordenada -->
<ol>
  <li>Item 1</li>
  <li>Item 2</li>
</ol>

<!-- Sin estilo -->
<ul class="list-unstyled">
  <li>Item sin bullets</li>
</ul>

<!-- Inline -->
<ul class="list-inline">
  <li class="list-inline-item">Item 1</li>
  <li class="list-inline-item">Item 2</li>
</ul>
```

## Código

```html
<code>código inline</code>
<pre><code>bloque de código</code></pre>
<kbd>Ctrl</kbd> + <kbd>S</kbd>
```

## Clases de tamaño de fuente (utilidades Color Admin)

```html
<span class="fs-10px">10px</span>
<span class="fs-11px">11px</span>
<span class="fs-12px">12px</span>
<span class="fs-13px">13px</span>
<span class="fs-14px">14px</span>
<span class="fs-16px">16px</span>
<span class="fs-18px">18px</span>
<span class="fs-20px">20px</span>
<span class="fs-24px">24px</span>
```

## Blockquote

```html
<blockquote class="blockquote">
  <p>Lorem ipsum dolor sit amet.</p>
</blockquote>
<figcaption class="blockquote-footer">
  Fuente <cite title="Source">Source</cite>
</figcaption>
```

## Tabla de descripción

```html
<dl class="row">
  <dt class="col-sm-3">Término</dt>
  <dd class="col-sm-9">Descripción del término</dd>
  <dt class="col-sm-3">Otro término</dt>
  <dd class="col-sm-9">
    Descripción más larga que puede<br/>
    ocupar múltiples líneas
  </dd>
</dl>
```

## Truncar texto

```html
<p class="text-truncate" style="max-width: 200px;">
  Texto muy largo que se trunca con puntos suspensivos
</p>
```
