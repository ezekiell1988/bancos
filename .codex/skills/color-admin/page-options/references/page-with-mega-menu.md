# Reference: Page with Mega Menu

Activa el mega-menu en el header (menú desplegable a pantalla completa).

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appHeaderMegaMenu = true;
  }

  ngOnDestroy() {
    this.appSettings.appHeaderMegaMenu = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad            | Tipo    | Efecto                                            |
|---------------------|---------|---------------------------------------------------|
| `appHeaderMegaMenu`  | boolean | Activa el mega-menu en el header                 |

## Notas

- El mega-menu no requiere cambios en el sidebar.
- Siempre resetear en `ngOnDestroy`.
