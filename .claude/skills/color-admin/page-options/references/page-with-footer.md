# Reference: Page with Footer

Muestra un footer estándar (no fijo) al final del contenido de la página.

## Resultado en HTML

```html
<div id="content" class="app-content">
  ...contenido de la página...
  <div id="footer" class="app-footer mx-0 px-0">
    &copy; 2024 Mi App — Todos los derechos reservados
  </div>
</div>
```

## Component TS

```typescript
import { Component } from '@angular/core';

@Component({ selector: 'my-page', templateUrl: './my-page.html' })
export class MyPage {}
```

## Clases CSS del footer

| Clase          | Efecto                                              |
|---------------|-----------------------------------------------------|
| `app-footer`   | Estilos base del footer de Color Admin             |
| `mx-0`         | Sin margen horizontal                               |
| `px-0`         | Sin padding horizontal                              |

## Notas

- A diferencia de `page-with-fixed-footer`, este footer fluye con el contenido (no es sticky).
- No requiere cambios en `AppSettings` para renderizarse.
- El footer simple se coloca directamente dentro del `#content`.
