# Reference: Modal & Notification

## Modal Básico (Bootstrap `data-bs-toggle`)

```html
<!-- Trigger -->
<a href="#modal-dialog" class="btn btn-sm btn-success w-100px" data-bs-toggle="modal">
  Abrir Modal
</a>

<!-- Modal estándar con animación fade -->
<div class="modal fade" id="modal-dialog">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h4 class="modal-title">Título del Modal</h4>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-hidden="true"></button>
      </div>
      <div class="modal-body">
        <p>Contenido del modal aquí...</p>
      </div>
      <div class="modal-footer">
        <a href="javascript:;" class="btn btn-white" data-bs-dismiss="modal">Cerrar</a>
        <a href="javascript:;" class="btn btn-success">Acción</a>
      </div>
    </div>
  </div>
</div>
```

## Modal Sin Animación

```html
<!-- Sin clase "fade": no hay animación al abrir -->
<div class="modal" id="modal-without-animation">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h4 class="modal-title">Sin animación</h4>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-hidden="true"></button>
      </div>
      <div class="modal-body">Contenido...</div>
      <div class="modal-footer">
        <a href="javascript:;" class="btn btn-white" data-bs-dismiss="modal">Cerrar</a>
      </div>
    </div>
  </div>
</div>
```

## Modal Message (fondo blanco full-width, Color Admin)

```html
<div class="modal modal-message fade" id="modal-message">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h4 class="modal-title">Modal Message Header</h4>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-hidden="true"></button>
      </div>
      <div class="modal-body">
        <p>¿Deseas continuar con la operación?</p>
      </div>
      <div class="modal-footer">
        <a href="javascript:;" class="btn btn-white" data-bs-dismiss="modal">Cerrar</a>
        <a href="javascript:;" class="btn btn-primary">Guardar</a>
      </div>
    </div>
  </div>
</div>
```

## Modal con Alert dentro

```html
<div class="modal fade" id="modal-alert">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h4 class="modal-title">Alert Header</h4>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-hidden="true"></button>
      </div>
      <div class="modal-body">
        <div class="alert alert-danger">
          <h5><i class="fa fa-info-circle"></i> Título de alerta</h5>
          <p>Descripción del problema o advertencia...</p>
        </div>
      </div>
      <div class="modal-footer">
        <a href="javascript:;" class="btn btn-white" data-bs-dismiss="modal">Cerrar</a>
        <a href="javascript:;" class="btn btn-danger" data-bs-dismiss="modal">Confirmar</a>
      </div>
    </div>
  </div>
</div>
```

## Tamaños de Modal

```html
<div class="modal-dialog modal-sm">...</div>   <!-- Pequeño -->
<div class="modal-dialog">...</div>             <!-- Por defecto -->
<div class="modal-dialog modal-lg">...</div>   <!-- Grande -->
<div class="modal-dialog modal-xl">...</div>   <!-- Extra grande -->
<div class="modal-dialog modal-fullscreen">...</div> <!-- Pantalla completa -->
```

## Clases clave

| Clase              | Descripción                                              |
|--------------------|----------------------------------------------------------|
| `modal fade`       | Modal con animación de entrada/salida                    |
| `modal`            | Modal sin animación                                      |
| `modal-message`    | Clase Color Admin para modal de mensaje (fondo blanco)   |
| `modal-dialog`     | Contenedor del diálogo                                   |
| `modal-content`    | Cuerpo visual del modal                                  |
| `modal-header`     | Cabecera con título y botón cerrar                       |
| `modal-body`       | Contenido principal                                      |
| `modal-footer`     | Pie de página con acciones                               |
| `modal-title`      | Título del modal                                         |
| `btn-close`        | Botón X de cierre (Bootstrap 5)                          |
| `data-bs-dismiss="modal"` | Cierra el modal al hacer click              |
| `w-100px`          | Ancho fijo de 100px (utilidad Color Admin)               |
