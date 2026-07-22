# Reference: Page with Two Sidebars

Muestra dos sidebars: uno principal a la izquierda y uno secundario (end) a la derecha, que comienza toggled (visible).

## Resultado en HTML

```html
<div id="app" class="app app-with-two-sidebar app-sidebar-end-toggled">
  <div id="header" class="app-header">
    <div class="navbar-header">
      <!-- Botón para el sidebar derecho en mobile -->
      <button type="button" class="navbar-mobile-toggler" data-toogle="app-sidebar-end-mobile">
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
      </button>
    </div>
  </div>
  <!-- Sidebar izquierdo principal -->
  <div id="sidebar" class="app-sidebar">...</div>
  <!-- Sidebar derecho secundario -->
  <div id="sidebar-end" class="app-sidebar app-sidebar-end">...</div>
  <div id="content" class="app-content">...</div>
</div>
```

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarTwo = true;
    this.appSettings.appSidebarEndToggled = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarTwo = false;
    this.appSettings.appSidebarEndToggled = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad                | Tipo    | Efecto                                                      |
|-------------------------|---------|-------------------------------------------------------------|
| `appSidebarTwo`          | boolean | Activa el layout de dos sidebars                           |
| `appSidebarEndToggled`   | boolean | Abre el sidebar derecho al cargar (visible por defecto)    |

## Notas

- `appSidebarEndToggled = true` en el constructor lo muestra abierto desde el inicio.
- Para el sidebar derecho en mobile, el botón usa `data-toogle="app-sidebar-end-mobile"` (no `data-toggle`).
- Siempre resetear ambas propiedades en `ngOnDestroy`.
