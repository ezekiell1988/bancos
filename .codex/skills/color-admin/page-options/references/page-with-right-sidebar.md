# Reference: Page with Right Sidebar

El sidebar se muestra en el lado derecho de la pantalla en lugar del izquierdo (end sidebar).

## Resultado en HTML

```html
<div id="app" class="app app-with-end-sidebar">
  ...
</div>
```

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarEnd = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarEnd = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad       | Tipo    | Efecto                                        |
|----------------|---------|-----------------------------------------------|
| `appSidebarEnd` | boolean | Mueve el sidebar al lado derecho (end)        |

## Notas

- Útil en layouts RTL o cuando el contenido principal debe estar a la izquierda.
- Siempre resetear en `ngOnDestroy`.
