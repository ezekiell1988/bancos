# Reference: General UI Elements

## Alerts

```html
<!-- Alert básica con dismiss -->
<div class="alert alert-success alert-dismissible fade show mb-0">
  <strong>Success!</strong> Mensaje con <a href="#" class="alert-link">enlace</a>.
  <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
</div>
```

## Colores de Alert disponibles

| Clase                  | Color        |
|------------------------|--------------|
| `alert-primary`        | Azul primario |
| `alert-info`           | Info          |
| `alert-purple`         | Púrpura       |
| `alert-indigo`         | Índigo        |
| `alert-success`        | Verde éxito   |
| `alert-green`          | Verde         |
| `alert-lime`           | Lima          |
| `alert-warning`        | Naranja advertencia |
| `alert-yellow`         | Amarillo      |
| `alert-danger`         | Rojo danger   |
| `alert-pink`           | Rosa          |
| `alert-dark`           | Oscuro        |
| `alert-secondary`      | Secundario    |
| `alert-light`          | Claro         |
| `alert-gray-500`       | Gris 500      |

## Notes (alertas con ícono lateral)

```html
<!-- Note con icono izquierdo -->
<div class="note alert-primary mb-2">
  <div class="note-icon"><i class="fab fa-facebook-f"></i></div>
  <div class="note-content">
    <h4><b>Note with icon!</b></h4>
    <p>Contenido de la nota aquí.</p>
  </div>
</div>

<!-- Note con icono derecho -->
<div class="note alert-warning note-with-end-icon mb-2">
  <div class="note-content text-end">
    <h4><b>Note with end icon!</b></h4>
    <p>Contenido de la nota aquí.</p>
  </div>
  <div class="note-icon"><i class="fa fa-lightbulb"></i></div>
</div>

<!-- Note sin icono -->
<div class="note alert-gray-500 mb-0">
  <div class="note-content">
    <h4><b>Note without icon!</b></h4>
    <p>Contenido de la nota aquí.</p>
  </div>
</div>
```

## Badges

```html
<!-- Colores de badge -->
<span class="badge bg-danger">Danger</span>
<span class="badge bg-warning">Warning</span>
<span class="badge bg-yellow text-black">Yellow</span>
<span class="badge bg-lime">Lime</span>
<span class="badge bg-green">Green</span>
<span class="badge bg-success">Success</span>
<span class="badge bg-primary">Primary</span>
<span class="badge bg-info">Info</span>
<span class="badge bg-purple">Purple</span>
<span class="badge bg-indigo">Indigo</span>
<span class="badge bg-pink">Pink</span>
<span class="badge bg-dark">Dark</span>
<span class="badge bg-white text-dark">White</span>
<span class="badge bg-light text-dark">Light</span>
<span class="badge bg-gray-200 text-dark">Gray 200</span>
<span class="badge bg-gray-500">Gray 500</span>
<span class="badge bg-secondary">Secondary</span>

<!-- Badge redondeado -->
<span class="badge rounded-pill bg-danger">Pill Danger</span>

<!-- Badge en panel title -->
<h4 class="panel-title">Panel <span class="badge bg-success ms-1">NEW</span></h4>
```

## Progress Bars

```html
<!-- Barra básica -->
<div class="progress mb-2">
  <div class="progress-bar" style="width: 40%"></div>
</div>

<!-- Con color y texto -->
<div class="progress mb-2">
  <div class="progress-bar bg-success fw-bold" style="width: 40%">40%</div>
</div>

<!-- Rayada animada -->
<div class="progress h-10px bg-gray-700 w-150px">
  <div class="progress-bar progress-bar-striped bg-success progress-bar-animated fw-bold" style="width: 40%">40%</div>
</div>

<!-- Múltiples barras -->
<div class="progress">
  <div class="progress-bar bg-success" style="width: 35%"></div>
  <div class="progress-bar bg-warning" style="width: 20%"></div>
  <div class="progress-bar bg-danger" style="width: 10%"></div>
</div>
```

## Colores de progress bar disponibles
`bg-primary` · `bg-info` · `bg-success` · `bg-warning` · `bg-danger` · `bg-purple` · `bg-inverse`

## Pagination

```html
<ul class="pagination">
  <li class="page-item disabled"><a class="page-link" href="#">&laquo;</a></li>
  <li class="page-item active"><a class="page-link" href="#">1</a></li>
  <li class="page-item"><a class="page-link" href="#">2</a></li>
  <li class="page-item"><a class="page-link" href="#">3</a></li>
  <li class="page-item"><a class="page-link" href="#">&raquo;</a></li>
</ul>

<!-- Tamaños -->
<ul class="pagination pagination-lg">...</ul>
<ul class="pagination pagination-sm">...</ul>
```

## Labels (label component Color Admin)

```html
<span class="label label-success">NEW</span>
<span class="label label-danger">HOT</span>
<span class="label label-primary">Info</span>
<span class="label label-warning">Warning</span>
<span class="label label-inverse">Inverse</span>
<span class="label label-default">Default</span>
```

## Blockquote

```html
<blockquote class="blockquote">
  <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
</blockquote>
<figcaption class="blockquote-footer">
  Someone famous in <cite title="Source Title">Source Title</cite>
</figcaption>
```

## Tables

```html
<!-- Tabla estándar en panel -->
<table class="table table-panel mb-0">
  <thead>
    <tr><th>Columna 1</th><th>Columna 2</th></tr>
  </thead>
  <tbody>
    <tr>
      <td class="align-middle">Contenido</td>
      <td class="with-btn">
        <a class="btn btn-sm btn-success w-100px">Acción</a>
      </td>
    </tr>
  </tbody>
</table>

<!-- Con rayas y texto sin wrap -->
<table class="table table-striped table-condensed text-nowrap m-0 table-panel">
```

## Well / Card básica

```html
<div class="well">Contenido del well</div>
<div class="well well-sm">Well pequeño</div>
<div class="well well-lg">Well grande</div>
```
