# Reference: ion-button

## Uso básico

```html
<!-- Variantes de expand -->
<ion-button>Default (inline)</ion-button>
<ion-button expand="block">Block (ancho completo)</ion-button>
<ion-button expand="full">Full (sin márgenes laterales)</ion-button>
```

```html
<!-- Fill (relleno) -->
<ion-button fill="solid">Solid (default)</ion-button>
<ion-button fill="outline">Outline</ion-button>
<ion-button fill="clear">Clear (solo texto)</ion-button>
```

```html
<!-- Tamaños -->
<ion-button size="small">Pequeño</ion-button>
<ion-button>Default</ion-button>
<ion-button size="large">Grande</ion-button>
```

```html
<!-- Forma redondeada -->
<ion-button shape="round">Redondeado</ion-button>
<ion-button shape="round" fill="outline">Outline Round</ion-button>
```

```html
<!-- Colores -->
<ion-button color="primary">Primario</ion-button>
<ion-button color="secondary">Secundario</ion-button>
<ion-button color="success">Éxito</ion-button>
<ion-button color="warning">Advertencia</ion-button>
<ion-button color="danger">Peligro</ion-button>
<ion-button color="light">Claro</ion-button>
<ion-button color="dark">Oscuro</ion-button>
```

```html
<!-- Deshabilitado -->
<ion-button [disabled]="isLoading">
  {{ isLoading ? 'Cargando...' : 'Enviar' }}
</ion-button>
```

```html
<!-- Botones con íconos -->
<!-- Icono a la izquierda -->
<ion-button>
  <ion-icon slot="start" name="save"></ion-icon>
  Guardar
</ion-button>

<!-- Icono a la derecha -->
<ion-button>
  Siguiente
  <ion-icon slot="end" name="arrow-forward"></ion-icon>
</ion-button>

<!-- Solo icono (debe tener aria-label) -->
<ion-button fill="clear" aria-label="Eliminar">
  <ion-icon slot="icon-only" name="trash"></ion-icon>
</ion-button>

<!-- Icono diferente por plataforma (iOS / MD) -->
<ion-button>
  <ion-icon slot="start" ios="logo-apple" md="logo-android"></ion-icon>
  App Store
</ion-button>
```

```html
<!-- Enlace de navegación con router -->
<ion-button routerLink="/detail/1" routerDirection="forward">Ver detalle</ion-button>
<ion-button routerLink="/home" routerDirection="back" fill="clear">Volver</ion-button>

<!-- Enlace externo -->
<ion-button href="https://ionic.io" target="_blank" rel="noopener">
  Documentación
  <ion-icon slot="end" name="open"></ion-icon>
</ion-button>
```

```html
<!-- Botón de submit en formulario -->
<form (ngSubmit)="onSubmit()">
  <ion-input label="Email" type="email" ngModel name="email"></ion-input>
  <ion-button type="submit" expand="block">Iniciar sesión</ion-button>
</form>
```

## Component TS

```typescript
import { IonButton, IonIcon } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { save, trash, arrowForward } from 'ionicons/icons';

@Component({ imports: [IonButton, IonIcon] })
export class ButtonsPage {
  isLoading = false;

  constructor() {
    // Registrar iconos usados en esta página
    addIcons({ save, trash, arrowForward });
  }

  async submit() {
    this.isLoading = true;
    try {
      await this.apiService.save();
    } finally {
      this.isLoading = false;
    }
  }
}
```

## Notas

- **expand**: `block` (ancho completo con margen), `full` (sin márgenes).
- **fill**: `solid` (default), `outline`, `clear`.
- **shape**: `round` (bordes muy redondeados).
- **size**: `small`, `default`, `large`.
- **routerDirection**: `forward` (default), `back`, `root`.
- `[strong]="true"` → texto en negrita (útil para confirmar en modales).
- `type`: `button` (default), `submit`, `reset` — para uso en `<form>`.
- **Slots**: default (texto), `start` (icono izq.), `end` (icono der.), `icon-only` (solo icono).
- Siempre usar `addIcons({...})` para registrar los iconos antes de usarlos con `IonIcon`.
- **CSS vars**: `--background`, `--color`, `--border-radius`, `--border-color`, `--border-width`, `--border-style`, `--box-shadow`, `--padding-top`, `--padding-bottom`, `--ripple-color`.
- **Imports TS**: `IonButton` | con iconos: `IonButton, IonIcon` (+ `addIcons` de `ionicons`)
