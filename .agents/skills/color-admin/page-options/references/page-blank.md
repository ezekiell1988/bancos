# Reference: Page Blank

Página vacía sin ninguna opción especial de layout. Estructura mínima con breadcrumb + page-header + panel.

## Uso básico

```html
<!-- BEGIN breadcrumb -->
<ol class="breadcrumb float-xl-end">
  <li class="breadcrumb-item"><a href="javascript:;">Home</a></li>
  <li class="breadcrumb-item active">Mi Página</li>
</ol>
<!-- END breadcrumb -->
<!-- BEGIN page-header -->
<h1 class="page-header">Título de la página <small>subtítulo opcional</small></h1>
<!-- END page-header -->

<!-- BEGIN panel -->
<panel title="Título del Panel">
  Contenido aquí
</panel>
<!-- END panel -->
```

## Component TS

```typescript
@Component({
  selector: 'my-page',
  templateUrl: './my-page.html'
})
export class MyPage {}
```

## Notas

- No requiere cambios en `AppSettings` ni en `document.body.className`.
- Es la plantilla base sobre la que se aplican todas las demás opciones de layout.
