# Reference: ion-alert

## Uso básico

```html
<!-- Alert con isOpen y botones reactivos -->
<ion-button (click)="isAlertOpen = true">Mostrar alerta</ion-button>

<ion-alert
  [isOpen]="isAlertOpen"
  header="Confirmar acción"
  message="¿Estás seguro de continuar?"
  [buttons]="alertButtons"
  (didDismiss)="isAlertOpen = false">
</ion-alert>
```

```html
<!-- Alert con trigger (sin TypeScript) -->
<ion-button id="confirm-button">Eliminar elemento</ion-button>

<ion-alert
  trigger="confirm-button"
  header="Eliminar"
  subHeader="Esta acción no se puede deshacer"
  message="¿Confirmas la eliminación del elemento seleccionado?"
  [buttons]="deleteButtons">
</ion-alert>
```

```html
<!-- Alert con inputs (formulario rápido) -->
<ion-button id="name-alert">Cambiar nombre</ion-button>

<ion-alert
  trigger="name-alert"
  header="Cambiar nombre"
  [inputs]="alertInputs"
  [buttons]="alertWithInputButtons">
</ion-alert>
```

## Component TS

```typescript
import { IonAlert } from '@ionic/angular/standalone';
import { AlertController } from '@ionic/angular/standalone';

@Component({ imports: [IonAlert, IonButton] })
export class AlertPage {
  isAlertOpen = false;

  alertButtons = [
    { text: 'Cancelar', role: 'cancel' },
    {
      text: 'Confirmar',
      role: 'confirm',
      handler: () => { console.log('Confirmado'); },
    },
  ];

  deleteButtons = [
    { text: 'No', role: 'cancel' },
    { text: 'Sí, eliminar', role: 'destructive', handler: () => this.deleteItem() },
  ];

  alertInputs = [
    { type: 'text' as const, placeholder: 'Escribe el nuevo nombre', value: 'Nombre actual' },
  ];

  alertWithInputButtons = [
    { text: 'Cancelar', role: 'cancel' },
    {
      text: 'Guardar',
      handler: (data: { 0: string }) => { console.log('Nuevo nombre:', data[0]); }
    },
  ];

  deleteItem() { /* lógica de eliminación */ }
}

// Programático con controlador
@Component({...})
export class ProgrammaticAlert {
  constructor(private alertCtrl: AlertController) {}

  async showConfirm() {
    const alert = await this.alertCtrl.create({
      header: 'Confirmación',
      message: '¿Deseas continuar?',
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        { text: 'Aceptar', handler: () => this.proceed() },
      ],
    });
    await alert.present();
  }

  async showWithRadio() {
    const alert = await this.alertCtrl.create({
      header: 'Seleccionar opción',
      inputs: [
        { type: 'radio', label: 'Opción A', value: 'a', checked: true },
        { type: 'radio', label: 'Opción B', value: 'b' },
      ],
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        { text: 'OK', handler: (val) => console.log('Elegido:', val) },
      ],
    });
    await alert.present();
  }

  proceed() { /* acción */ }
}
```

## Notas

- **Roles de botones**: `cancel` (cierra sin acción), `destructive` (resalta en rojo en iOS), `confirm`.
- **Tipos de input** en el alert: `text`, `number`, `email`, `password`, `textarea`, `radio`, `checkbox`.
- `subHeader` → subtítulo debajo del header, encima del message.
- El handler de los botones recibe los valores de los inputs como object.
- `(didDismiss)` emite `{ data, role }`.
- **Imports TS**: `IonAlert, IonButton` | controller: `AlertController`
