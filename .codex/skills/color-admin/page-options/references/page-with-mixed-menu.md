# Reference: Page with Mixed Menu

Combina el sidebar lateral con un top menu horizontal. El sidebar permanece visible y además aparece la barra de navegación superior.

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appTopMenu = true;
  }

  ngOnDestroy() {
    this.appSettings.appTopMenu = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad       | Tipo    | Efecto                                               |
|----------------|---------|------------------------------------------------------|
| `appTopMenu`    | boolean | Muestra la barra de menú horizontal superior        |

## Diferencia con Top Menu puro

| Opción              | `appTopMenu` | `appSidebarNone` | Sidebar visible |
|--------------------|-------------|-----------------|-----------------|
| Mixed Menu          | `true`      | `false`          | ✅ Sí            |
| Top Menu (sin sidebar) | `true`   | `true`           | ❌ No            |

## Notas

- El mixed menu mantiene el sidebar y agrega el top menu encima.
- Para eliminar el sidebar y usar solo el top menu, combinar con `appSidebarNone = true` (ver `page-with-top-menu`).
