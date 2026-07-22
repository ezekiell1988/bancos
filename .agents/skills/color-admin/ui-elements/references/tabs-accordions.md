# Reference: Tabs & Accordions

## Tabs estilo Default (nav-tabs)

```html
<!-- Pestañas -->
<ul class="nav nav-tabs">
  <li class="nav-item">
    <a href="#tab-1" data-bs-toggle="tab" class="nav-link active">
      <span class="d-sm-none">Tab 1</span>
      <span class="d-sm-block d-none">Default Tab 1</span>
    </a>
  </li>
  <li class="nav-item">
    <a href="#tab-2" data-bs-toggle="tab" class="nav-link">
      <span class="d-sm-none">Tab 2</span>
      <span class="d-sm-block d-none">Default Tab 2</span>
    </a>
  </li>
</ul>

<!-- Contenido de pestañas -->
<div class="tab-content panel rounded-0 p-3 m-0">
  <div class="tab-pane fade active show" id="tab-1">
    <h3 class="mt-10px">Contenido Tab 1</h3>
    <p>Lorem ipsum...</p>
    <p class="text-end mb-0">
      <a href="javascript:;" class="btn btn-white me-5px">Default</a>
      <a href="javascript:;" class="btn btn-primary">Primary</a>
    </p>
  </div>
  <div class="tab-pane fade" id="tab-2">
    <p>Contenido Tab 2</p>
  </div>
</div>
```

> **Nota**: El `<div class="tab-content panel rounded-0 p-3 m-0">` crea el panel visual que contiene el contenido de la pestaña activa, integrado con el estilo Color Admin.

## Tabs estilo Pills (nav-pills)

```html
<!-- Pills -->
<ul class="nav nav-pills mb-2">
  <li class="nav-item">
    <a href="#pills-1" data-bs-toggle="tab" class="nav-link active">Pills Tab 1</a>
  </li>
  <li class="nav-item">
    <a href="#pills-2" data-bs-toggle="tab" class="nav-link">Pills Tab 2</a>
  </li>
</ul>

<!-- Contenido -->
<div class="tab-content p-3 rounded-top panel rounded-0 m-0">
  <div class="tab-pane fade active show" id="pills-1">
    <h3 class="mt-10px">Nav Pills Tab 1</h3>
    <p>Contenido...</p>
  </div>
  <div class="tab-pane fade" id="pills-2">
    <p>Contenido...</p>
  </div>
</div>
```

## Tabs verticales (nav-tabs en columna)

```html
<div class="row">
  <div class="col-md-3">
    <ul class="nav nav-tabs flex-column">
      <li class="nav-item">
        <a href="#vert-tab-1" data-bs-toggle="tab" class="nav-link active">Tab 1</a>
      </li>
      <li class="nav-item">
        <a href="#vert-tab-2" data-bs-toggle="tab" class="nav-link">Tab 2</a>
      </li>
    </ul>
  </div>
  <div class="col-md-9">
    <div class="tab-content panel p-3 m-0">
      <div class="tab-pane fade active show" id="vert-tab-1">Contenido 1</div>
      <div class="tab-pane fade" id="vert-tab-2">Contenido 2</div>
    </div>
  </div>
</div>
```

## Accordion (Bootstrap nativo)

```html
<div class="accordion" id="accordionExample">
  <!-- Item 1 (abierto por defecto) -->
  <div class="accordion-item">
    <h2 class="accordion-header" id="heading1">
      <button class="accordion-button" type="button"
        data-bs-toggle="collapse" data-bs-target="#collapse1"
        aria-expanded="true" aria-controls="collapse1">
        Accordion Item #1
      </button>
    </h2>
    <div id="collapse1" class="accordion-collapse collapse show"
      aria-labelledby="heading1" data-bs-parent="#accordionExample">
      <div class="accordion-body">
        Contenido del accordion...
      </div>
    </div>
  </div>

  <!-- Item 2 (cerrado) -->
  <div class="accordion-item">
    <h2 class="accordion-header" id="heading2">
      <button class="accordion-button collapsed" type="button"
        data-bs-toggle="collapse" data-bs-target="#collapse2"
        aria-expanded="false" aria-controls="collapse2">
        Accordion Item #2
      </button>
    </h2>
    <div id="collapse2" class="accordion-collapse collapse"
      aria-labelledby="heading2" data-bs-parent="#accordionExample">
      <div class="accordion-body">
        Contenido del accordion...
      </div>
    </div>
  </div>
</div>
```

## Accordion estilo Panel (Color Admin)

```html
<div id="accordion" class="accordion">
  <div class="card">
    <div class="card-header" id="headingPanel1">
      <h5 class="mb-0">
        <a class="btn btn-link" data-bs-toggle="collapse"
          data-bs-target="#collapsePanel1" aria-expanded="true">
          Collapsible Group Item #1
        </a>
      </h5>
    </div>
    <div id="collapsePanel1" class="collapse show"
      aria-labelledby="headingPanel1" data-bs-parent="#accordion">
      <div class="card-body">
        Contenido...
      </div>
    </div>
  </div>
</div>
```

## Fa-stack en tabs (icono apilado sobre texto)

```html
<span class="fa-stack fa-4x float-start me-10px">
  <i class="fa fa-square fa-stack-2x"></i>
  <i class="fab fa-twitter text-white fa-stack-1x"></i>
</span>
```

## Clases clave

| Clase                          | Descripción                                   |
|-------------------------------|-----------------------------------------------|
| `nav nav-tabs`                | Pestañas estilo tab horizontal                |
| `nav nav-pills`               | Pestañas estilo pill                          |
| `nav-item`                    | Cada elemento de la navegación                |
| `nav-link active`             | Pestaña activa                                |
| `tab-content`                 | Contenedor del contenido                      |
| `tab-pane fade`               | Pane oculto con animación                     |
| `tab-pane fade active show`   | Pane visible                                  |
| `panel rounded-0 p-3 m-0`    | Estilo Color Admin para envolver tab-content  |
| `accordion-button`            | Botón de accordion Bootstrap 5                |
| `accordion-collapse collapse show` | Accordion abierto                        |
| `mt-10px`                     | Utilidad Color Admin: margin-top 10px         |
