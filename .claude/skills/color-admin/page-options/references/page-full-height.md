# Reference: Page Full Height

El contenido ocupa el 100% de la altura de la ventana (sin padding lateral). Útil para mapas, editores o dashboards que deben llenar la pantalla.

## Resultado en HTML

```html
<div id="app" class="app app-content-full-height">
  ...
  <div id="content" class="app-content p-0">
    <div class="overflow-hidden h-100">
      <div data-scrollbar="true" data-height="100%" data-skip-mobile="true" class="app-content-padding">
        ...
      </div>
    </div>
  </div>
</div>
```

## Component TS

```typescript
import { Component, OnDestroy } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(public appSettings: AppSettings) {
    this.appSettings.appContentFullHeight = true;
    this.appSettings.appContentClass = 'p-0';
  }

  ngOnDestroy() {
    this.appSettings.appContentFullHeight = false;
    this.appSettings.appContentClass = '';
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad                         | Tipo     | Efecto                                               |
|----------------------------------|----------|------------------------------------------------------|
| `appContentFullHeight`            | boolean  | Añade clase `app-content-full-height` al wrapper    |
| `appContentClass`                 | string   | Clases CSS adicionales para `#content`. Usar `'p-0'` para quitar padding |

## Notas

- Siempre resetear en `ngOnDestroy`.
- Combinar con `data-scrollbar="true"` en el contenedor interior para mantener scroll personalizado.
