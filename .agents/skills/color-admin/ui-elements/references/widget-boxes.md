# Reference: Widget Boxes (Panels)

## Panel básico

```html
<panel title="Panel Default">
  <p>Contenido del panel aquí</p>
</panel>
```

## Panel con header personalizado (badge, dropdown, botón)

```html
<!-- Con badge -->
<panel>
  <ng-container header>
    <h4 class="panel-title">
      Panel Header <span class="badge bg-success ms-1">NEW</span>
    </h4>
  </ng-container>
  <p>Contenido...</p>
</panel>

<!-- Con dropdown en header -->
<panel noButton="true">
  <ng-container header>
    <h4 class="panel-title">Panel con Dropdown</h4>
    <div class="btn-group my-n1">
      <button class="btn btn-success btn-xs">Action</button>
      <button class="btn btn-success btn-xs dropdown-toggle" data-bs-toggle="dropdown">
        <b class="caret"></b>
      </button>
      <div class="dropdown-menu dropdown-menu-end">
        <a href="javascript:;" class="dropdown-item">Action</a>
        <a href="javascript:;" class="dropdown-item">Another action</a>
        <div class="dropdown-divider"></div>
        <a href="javascript:;" class="dropdown-item">Separated link</a>
      </div>
    </div>
  </ng-container>
  <p>Contenido...</p>
</panel>
```

## Panel con Radio Buttons en header

```html
<panel noButton="true">
  <ng-container header>
    <h4 class="panel-title">Panel con Radio Button</h4>
    <div class="btn-group btn-group-toggle my-n1" data-toggle="buttons">
      <input type="radio" name="options" class="btn-check" id="option1" autocomplete="off" checked />
      <label class="btn btn-success btn-xs" for="option1">Option 1</label>
      <input type="radio" name="options" class="btn-check" id="option2" autocomplete="off" />
      <label class="btn btn-success btn-xs" for="option2">Option 2</label>
    </div>
  </ng-container>
  <p>Contenido...</p>
</panel>
```

## Panel con Progress Bar en header

```html
<panel noButton="true">
  <ng-container header>
    <h4 class="panel-title">Panel con Progress Bar</h4>
    <div class="progress h-10px bg-gray-700 w-150px">
      <div class="progress-bar progress-bar-striped bg-success progress-bar-animated fw-bold"
        style="width: 40%">40%</div>
    </div>
  </ng-container>
  <p>Contenido...</p>
</panel>
```

## Panel con Alert Box integrada (sin padding de body)

```html
<panel title="Panel with Alert Box" bodyClass="p-0">
  <div class="alert alert-success alert-dismissible fade show rounded-0 mb-0">
    <div class="d-flex">
      <i class="fa fa-check fa-2x me-1"></i>
      <div class="mb-0 ps-2">Mensaje de éxito...</div>
    </div>
    <button type="button" class="btn-close ms-3" data-bs-dismiss="alert"></button>
  </div>
  <div class="panel-body">
    <p>Contenido del panel aquí</p>
  </div>
</panel>
```

## Panel con icono flotante al hacer hover

```html
<panel panelClass="panel-hover-icon" title="Hover View Icon">
  <p>Contenido...</p>
</panel>
```

## Panel con scroll interno (ng-scrollbar)

```html
<panel title="Panel con Scrollbar">
  <ng-scrollbar style="height: 280px">
    <p>Contenido largo que genera scroll vertical...</p>
    <p>Más contenido...</p>
  </ng-scrollbar>
</panel>
```

## Panel con tabla (sin body padding)

```html
<panel title="Panel con Tabla" noBody="true">
  <ng-container outsideBody>
    <div class="table-responsive">
      <table class="table table-striped table-condensed text-nowrap m-0 table-panel">
        <thead>
          <tr><th>Col 1</th><th>Col 2</th></tr>
        </thead>
        <tbody>
          <tr>
            <td class="align-middle">dato</td>
            <td class="with-btn">
              <a class="btn btn-sm btn-primary w-100px">Ver</a>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </ng-container>
</panel>
```

## Props del componente `<panel>`

| Prop          | Tipo      | Descripción                                              |
|---------------|-----------|----------------------------------------------------------|
| `title`       | string    | Título del panel (aparece en header automáticamente)     |
| `noButton`    | boolean   | Oculta los botones de acción del panel (expand, reload…) |
| `noBody`      | boolean   | Elimina el padding del body del panel                    |
| `bodyClass`   | string    | Clase CSS adicional para el body del panel               |
| `panelClass`  | string    | Clase CSS adicional para el elemento raíz del panel      |

## Slots de proyección de contenido

| Slot           | Descripción                                              |
|----------------|----------------------------------------------------------|
| `header`       | `<ng-container header>` — contenido personalizado en el header |
| `outsideBody`  | `<ng-container outsideBody>` — se renderiza fuera del body (tabla, hljs-wrapper) |
| `footer`       | `<div footer>` — contenido del footer del panel         |

## Patrones de panel en grilla

```html
<!-- Dos paneles en columnas iguales -->
<div class="row">
  <div class="col-xl-6">
    <panel title="Izquierda">...</panel>
  </div>
  <div class="col-xl-6">
    <panel title="Derecha">...</panel>
  </div>
</div>
```
