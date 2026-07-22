# Reference: Page with Top Menu

Navegación solo con barra horizontal superior. El sidebar lateral queda oculto completamente.

## Resultado en HTML

```html
<div id="app" class="app app-with-top-menu">
  <div id="header" class="app-header">
    <div class="navbar-header">
      <a href="index.html" class="navbar-brand">...</a>
      <button type="button" class="navbar-mobile-toggler" data-toggle="app-top-menu-mobile">
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
      </button>
    </div>
  </div>
  <!-- Sin sidebar -->
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
    this.appSettings.appSidebarNone = true;
    this.appSettings.appTopMenu = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarNone = false;
    this.appSettings.appTopMenu = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad        | Tipo    | Efecto                                          |
|-----------------|---------|--------------------------------------------------|
| `appSidebarNone` | boolean | Oculta completamente el sidebar lateral         |
| `appTopMenu`     | boolean | Muestra la barra de menú horizontal superior    |

## Notas

- Usar ambas propiedades juntas para el layout de top menu puro.
- Solo `appTopMenu = true` (sin `appSidebarNone`) produce el "Mixed Menu" (sidebar + top).
