# Reference: ion-action-sheet

## Uso básico

```html
<!-- Con trigger (sin TypeScript) -->
<ion-button id="open-action-sheet">Opciones</ion-button>

<ion-action-sheet
  trigger="open-action-sheet"
  header="Acciones disponibles"
  [buttons]="actionSheetButtons"
  (didDismiss)="onDismiss($event)">
</ion-action-sheet>
```

```html
<!-- Con isOpen reactivo -->
<ion-button (click)="isActionSheetOpen = true">Abrir</ion-button>

<ion-action-sheet
  [isOpen]="isActionSheetOpen"
  header="Compartir"
  subHeader="Elige cómo compartir"
  [buttons]="shareButtons"
  (didDismiss)="isActionSheetOpen = false">
</ion-action-sheet>
```

## Component TS

```typescript
import { IonActionSheet, IonButton } from '@ionic/angular/standalone';
import { ActionSheetController } from '@ionic/angular/standalone';

@Component({ imports: [IonActionSheet, IonButton] })
export class ActionSheetPage {
  isActionSheetOpen = false;

  actionSheetButtons = [
    {
      text: 'Eliminar',
      role: 'destructive',
      icon: 'trash',
      handler: () => { console.log('Eliminar clickeado'); },
    },
    {
      text: 'Compartir',
      icon: 'share',
      data: { action: 'share' },
    },
    {
      text: 'Mover',
      icon: 'move',
      data: { action: 'move' },
    },
    {
      text: 'Cancelar',
      role: 'cancel',
      icon: 'close',
    },
  ];

  onDismiss(event: Event) {
    const ev = event as CustomEvent;
    const { data, role } = ev.detail;
    if (role !== 'cancel') {
      console.log('Acción seleccionada:', data);
    }
  }
}

// Programático con controlador
@Component({...})
export class ProgrammaticActionSheet {
  constructor(private actionSheetCtrl: ActionSheetController) {}

  async showOptions() {
    const sheet = await this.actionSheetCtrl.create({
      header: 'Opciones del documento',
      subHeader: 'Selecciona una acción',
      buttons: [
        { text: 'Eliminar', role: 'destructive', icon: 'trash', handler: () => this.delete() },
        { text: 'Compartir', icon: 'share', handler: () => this.share() },
        { text: 'Cancelar', role: 'cancel' },
      ],
    });
    await sheet.present();

    const { data, role } = await sheet.onDidDismiss();
    console.log('Cerrado con rol:', role);
  }

  delete() { /* lógica */ }
  share() { /* lógica */ }
}
```

## Notas

- **Roles de botones**: `cancel` (siempre se muestra abajo separado), `destructive` (rojo en iOS), `selected` (con checkmark).
- `icon` en los botones muestra un icono a la izquierda del texto.
- El botón con `role: 'cancel'` se agrupa visualmente separado del resto.
- `(didDismiss)` recibe `{ data, role }` donde `data` proviene del campo `data` del botón presionado.
- **Imports TS**: `IonActionSheet, IonButton` | controller: `ActionSheetController`
