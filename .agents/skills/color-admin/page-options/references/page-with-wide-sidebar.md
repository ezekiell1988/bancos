# Reference: Page with Wide Sidebar

Sidebar más ancho de lo normal (usa más espacio horizontal).

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarWide = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarWide = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad        | Tipo    | Efecto                                    |
|-----------------|---------|-------------------------------------------|
| `appSidebarWide` | boolean | Aumenta el ancho del sidebar              |

## Notas

- El sidebar wide es útil cuando los ítems del menú tienen etiquetas largas.
- Siempre resetear en `ngOnDestroy`.
