# Reference: Page with Search Sidebar

El sidebar incluye un campo de búsqueda en la parte superior para filtrar los ítems del menú.

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarSearch = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarSearch = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad          | Tipo    | Efecto                                           |
|-------------------|---------|--------------------------------------------------|
| `appSidebarSearch` | boolean | Muestra campo de búsqueda en la parte superior del sidebar |

## Notas

- Solo activa el input de búsqueda; la lógica de filtrado del menú ya está integrada en el sidebar de Color Admin.
- Siempre resetear en `ngOnDestroy`.
