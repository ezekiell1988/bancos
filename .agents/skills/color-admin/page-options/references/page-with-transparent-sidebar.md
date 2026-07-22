# Reference: Page with Transparent Sidebar

Sidebar con fondo transparente; el wallpaper/imagen del body se ve a través del sidebar.

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarTransparent = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarTransparent = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad                | Tipo    | Efecto                                       |
|-------------------------|---------|----------------------------------------------|
| `appSidebarTransparent`  | boolean | Elimina el fondo sólido del sidebar          |

## Notas

- Funciona mejor en combinación con un `appCover` o imagen de fondo en `body`.
- Siempre resetear en `ngOnDestroy`.
