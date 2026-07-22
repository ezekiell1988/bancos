# Reference: Page with Light Sidebar

Sidebar con fondo claro en lugar del oscuro por defecto. Se combina con header inverso para mantener contraste.

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarLight = true;
    this.appSettings.appHeaderInverse = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarLight = false;
    this.appSettings.appHeaderInverse = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad           | Tipo    | Efecto                                          |
|--------------------|---------|--------------------------------------------------|
| `appSidebarLight`   | boolean | Aplica tema claro al sidebar (fondo blanco/gris)|
| `appHeaderInverse`  | boolean | Invierte los colores del header (fondo oscuro)  |

## Notas

- `appHeaderInverse` se activa junto con `appSidebarLight` para que el header contraste con el sidebar claro.
- Siempre resetear ambas propiedades en `ngOnDestroy`.
