# Reference: Page with Minified Sidebar

Sidebar colapsado a íconos solamente (sin texto de los ítems de menú).

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarMinified = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarMinified = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad             | Tipo    | Efecto                                         |
|----------------------|---------|------------------------------------------------|
| `appSidebarMinified`  | boolean | Colapsa el sidebar a solo íconos (sin texto)   |

## Notas

- Al minificar el sidebar, el área de contenido se expande automáticamente.
- Siempre resetear en `ngOnDestroy` para no afectar otras páginas.
