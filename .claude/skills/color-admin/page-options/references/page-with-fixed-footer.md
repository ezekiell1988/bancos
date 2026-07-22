# Reference: Page with Fixed Footer

Footer fijo al fondo del viewport mientras el contenido superior hace scroll. Requiere estructura flex en el componente host.

## Resultado en HTML

```html
<div id="app" class="app app-content-full-height">
  <div id="content" class="app-content d-flex flex-column p-0">
    <!-- Área scrollable -->
    <div class="app-content-padding flex-grow-1 overflow-hidden"
         data-scrollbar="true" data-height="100%">
      ...contenido...
    </div>
    <!-- Footer fijo al fondo -->
    <div id="footer" class="app-footer">
      &copy; 2024 Mi App
    </div>
  </div>
</div>
```

## Component TS

```typescript
import { Component, OnDestroy, ElementRef } from '@angular/core';
import { AppSettings } from '../service/app-settings.service';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnDestroy {

  constructor(private elRef: ElementRef, public appSettings: AppSettings) {
    this.appSettings.appContentFullHeight = true;
    this.appSettings.appContentClass = 'p-0';
    // Hace que el host sea flex columna para que el footer quede al fondo
    this.elRef.nativeElement.classList.add('d-flex', 'flex-column', 'h-100');
  }

  ngOnDestroy() {
    this.appSettings.appContentFullHeight = false;
    this.appSettings.appContentClass = '';
  }
}
```

## Tabla de propiedades AppSettings

| Propiedad              | Tipo    | Efecto                                           |
|-----------------------|---------|--------------------------------------------------|
| `appContentFullHeight` | boolean | Añade `app-content-full-height` al wrapper      |
| `appContentClass`      | string  | Clases extra para `#content`. Usar `'p-0'`      |

## Clases en el host component

| Clase            | Efecto                                              |
|-----------------|-----------------------------------------------------|
| `d-flex`         | Activa flexbox en el componente host               |
| `flex-column`    | Dirección vertical (header + contenido + footer)   |
| `h-100`          | El host ocupa el 100% de la altura disponible      |

## Notas

- `flex-grow-1` en el área de contenido hace que el footer quede siempre al fondo.
- `data-scrollbar="true" data-height="100%"` activa el scroll personalizado en el área de contenido.
