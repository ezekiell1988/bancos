# Reference: Media Object

## Media Object básico (usando Flexbox)

```html
<!-- Media con imagen izquierda -->
<div class="d-flex">
  <a class="w-60px" href="javascript:;">
    <img src="/assets/img/user/user-1.jpg" alt="" class="mw-100 rounded" />
  </a>
  <div class="ps-3 flex-1">
    <h5 class="mb-1">Media heading</h5>
    <p class="mb-0">Contenido del media object...</p>
  </div>
</div>
```

## Media anidado

```html
<div class="d-flex">
  <a class="w-60px" href="javascript:;">
    <img src="/assets/img/user/user-2.jpg" alt="" class="mw-100 rounded" />
  </a>
  <div class="ps-3 flex-1">
    <h5 class="mb-1">Media heading</h5>
    <p>Texto del media padre...</p>

    <hr class="bg-gray-500" />

    <!-- Media anidado -->
    <div class="d-flex">
      <a class="w-60px" href="javascript:;">
        <img src="/assets/img/user/user-3.jpg" alt="" class="mw-100 rounded" />
      </a>
      <div class="ps-3 flex-1">
        <h5 class="mb-1">Nested media heading</h5>
        <p class="mb-0">Texto del media hijo...</p>
      </div>
    </div>
  </div>
</div>
```

## Media con imagen a la derecha

```html
<div class="d-flex">
  <div class="flex-1 pe-3">
    <h5 class="mb-1">Media heading</h5>
    <p class="mb-0">Texto del media...</p>
  </div>
  <a class="w-60px" href="javascript:;">
    <img src="/assets/img/user/user-9.jpg" alt="" class="mw-100 rounded-pill" />
  </a>
</div>
```

## Imágenes con bordes redondeados

```html
<!-- Cuadrado con esquinas redondeadas -->
<img src="..." class="mw-100 rounded" />

<!-- Circular (pill) -->
<img src="..." class="mw-100 rounded-pill" />
```

## Separador entre media objects

```html
<hr class="bg-gray-500" />
```

## Tamaños de contenedor de imagen

```html
<!-- Ancho fijo pequeño -->
<a class="w-60px" href="javascript:;">
  <img src="..." class="mw-100 rounded" />
</a>

<!-- Ancho responsivo: grande en lg, pequeño en mobile -->
<a class="w-lg-250px w-100px" href="javascript:;">
  <img src="..." class="mw-100 rounded" />
</a>
<a class="w-lg-200px w-80px" href="javascript:;">
  <img src="..." class="mw-100 rounded" />
</a>
<a class="w-lg-150px w-60px" href="javascript:;">
  <img src="..." class="mw-100 rounded" />
</a>
```

## Lista de media (múltiples items)

```html
<div class="d-flex mb-3">
  <!-- imagen + contenido -->
</div>
<hr class="bg-gray-500" />
<div class="d-flex mb-3">
  <!-- imagen + contenido -->
</div>
<hr class="bg-gray-500" />
<div class="d-flex">
  <!-- imagen + contenido -->
</div>
```

## Clases clave

| Clase          | Descripción                                              |
|----------------|----------------------------------------------------------|
| `d-flex`       | Contenedor flex para alinear imagen + contenido          |
| `flex-1`       | Haz que el texto ocupe el espacio restante               |
| `ps-3`         | Padding izquierdo para separar imagen del texto          |
| `pe-3`         | Padding derecho (cuando imagen está a la derecha)        |
| `w-60px`       | Ancho de 60px para el contenedor de la imagen            |
| `w-80px`       | Ancho de 80px                                            |
| `w-100px`      | Ancho de 100px                                           |
| `w-lg-200px`   | Ancho 200px en pantallas grandes (lg)                    |
| `mw-100`       | max-width: 100% — imagen responsiva dentro del contenedor|
| `rounded`      | Bordes redondeados en la imagen                          |
| `rounded-pill` | Imagen circular                                          |
| `bg-gray-500`  | Color del separador `<hr>`                               |
