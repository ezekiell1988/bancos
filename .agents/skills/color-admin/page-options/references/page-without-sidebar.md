# Reference: Page without Sidebar

Oculta completamente el sidebar lateral. El contenido ocupa todo el ancho disponible.

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appSidebarNone = true;
  }

  ngOnDestroy() {
    this.appSettings.appSidebarNone = false;
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad        | Tipo    | Efecto                                        |
|-----------------|---------|-----------------------------------------------|
| `appSidebarNone` | boolean | Oculta el sidebar (el contenido ocupa todo el ancho) |

## Diferencias entre opciones sin sidebar

| Opción                | `appSidebarNone` | `appTopMenu` | Navegación disponible     |
|----------------------|-----------------|-------------|---------------------------|
| Page Without Sidebar  | `true`          | `false`      | Solo header               |
| Page with Top Menu    | `true`          | `true`       | Header + barra top menu   |

## Notas

- Útil para páginas de login, landing pages o pantallas de configuración que no necesitan navegación lateral.
- Siempre resetear en `ngOnDestroy`.
