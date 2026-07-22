# Reference: Page with Boxed Layout

Restringe el ancho de la aplicación a un contenedor centrado. Se activa añadiendo la clase `boxed-layout` a `document.body`.

## Resultado en HTML

```html
<body class="boxed-layout">
  <div id="app" class="app">
    ...
  </div>
</body>
```

## Component TS

```typescript
import { Component, OnDestroy, OnInit } from '@angular/core';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage implements OnInit, OnDestroy {

  constructor() {
    document.body.className = document.body.className + ' boxed-layout';
  }

  ngOnDestroy() {
    document.body.className = document.body.className.replace('boxed-layout', '').trim();
  }
}
```

## Variante: Boxed Layout + Mixed Menu (Top + Sidebar)

Combina `boxed-layout` en body con `appTopMenu = true` en AppSettings.

```typescript
import { AppSettings } from '../service/app-settings.service';

constructor(public appSettings: AppSettings) {
  this.appSettings.appTopMenu = true;
  document.body.className = document.body.className + ' boxed-layout';
}

ngOnDestroy() {
  this.appSettings.appTopMenu = false;
  document.body.className = document.body.className.replace('boxed-layout', '').trim();
}
```

## Tabla de clases / propiedades

| Propiedad / Clase          | Efecto                                           |
|---------------------------|--------------------------------------------------|
| `body.boxed-layout`        | Restringe el ancho del contenedor principal      |
| `appSettings.appTopMenu`   | Muestra el menú horizontal superior (top menu)  |

## Notas

- Siempre limpiar `ngOnDestroy` para que no afecte a otras rutas.
- Usar `.trim()` al hacer `.replace()` para evitar espacios extra.
