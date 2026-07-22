# Reference: ion-toast

## Uso básico

```html
<!-- Toast con trigger (sin TypeScript) -->
<ion-button id="open-toast">Mostrar notificación</ion-button>

<ion-toast
  trigger="open-toast"
  message="Los cambios se han guardado correctamente."
  [duration]="3000">
</ion-toast>
```

```html
<!-- Toast con posición y botón de cierre -->
<ion-button id="status-toast">Enviar</ion-button>

<ion-toast
  trigger="status-toast"
  message="Formulario enviado."
  [duration]="5000"
  position="top"
  icon="checkmark-circle"
  color="success"
  [buttons]="toastButtons"
  (didDismiss)="onToastDismiss($event)">
</ion-toast>
```

```html
<!-- Toast con isOpen reactivo -->
<ion-toast
  [isOpen]="isToastOpen"
  message="Operación completada."
  [duration]="2000"
  position="bottom"
  (didDismiss)="isToastOpen = false">
</ion-toast>
```

```html
<!-- Toast anclado a un elemento (positionAnchor) -->
<ion-header id="main-header">
  <ion-toolbar><ion-title>Página</ion-title></ion-toolbar>
</ion-header>
<ion-content>
  <ion-button id="anchor-toast">Notificar</ion-button>
  <ion-toast
    trigger="anchor-toast"
    message="¡Nuevo mensaje!"
    position="top"
    positionAnchor="main-header"
    [duration]="3000">
  </ion-toast>
</ion-content>
```

## Component TS

```typescript
import { IonToast, IonButton } from '@ionic/angular/standalone';
import { ToastController } from '@ionic/angular/standalone';

@Component({ imports: [IonToast, IonButton] })
export class ToastPage {
  isToastOpen = false;

  toastButtons = [
    {
      text: 'Deshacer',
      role: 'cancel',
      handler: () => { this.undoAction(); },
    },
  ];

  onToastDismiss(event: Event) {
    const { role } = (event as CustomEvent).detail;
    if (role === 'cancel') {
      console.log('Usuario deshizo la acción');
    }
  }

  undoAction() { /* lógica de deshacer */ }
}

// Programático con controlador
@Component({...})
export class ProgrammaticToast {
  constructor(private toastCtrl: ToastController) {}

  async showSuccess() {
    const toast = await this.toastCtrl.create({
      message: 'Guardado exitosamente.',
      duration: 2000,
      position: 'bottom',
      color: 'success',
      icon: 'checkmark-circle',
    });
    await toast.present();
  }

  async showError() {
    const toast = await this.toastCtrl.create({
      message: 'Ocurrió un error. Inténtalo de nuevo.',
      position: 'top',
      color: 'danger',
      icon: 'alert-circle',
      buttons: [{ text: 'OK', role: 'cancel' }],
    });
    await toast.present();
  }
}
```

## Notas

- **position**: `top`, `middle`, `bottom` (default).
- `positionAnchor`: id de un elemento HTML para anclar el toast relativo a ese elemento.
- **color**: cualquier color Ionic (`success`, `danger`, `warning`, `primary`, etc.).
- `icon`: nombre de ionicon que aparece antes del mensaje.
- `swipeGesture="vertical"` → permite cerrar el toast deslizando.
- `layout="stacked"` → botones debajo del mensaje en lugar de al lado.
- Los botones con `role: 'cancel'` cierran el toast.
- **CSS vars**: `--background`, `--color`, `--border-radius`, `--box-shadow`, `--button-color`, `--start`, `--end`.
- **Imports TS**: `IonToast, IonButton` | controller: `ToastController`
