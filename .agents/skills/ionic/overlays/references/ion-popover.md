# Reference: ion-popover

## Uso básico

```html
<!-- Popover con trigger (click por defecto) -->
<ion-button id="popover-trigger">Mostrar popover</ion-button>

<ion-popover trigger="popover-trigger" triggerAction="click">
  <ng-template>
    <ion-content class="ion-padding">
      <p>Contenido del popover.</p>
    </ion-content>
  </ng-template>
</ion-popover>
```

```html
<!-- triggerAction: hover (tooltip) -->
<ion-button id="hover-trigger">Pasa el cursor</ion-button>

<ion-popover trigger="hover-trigger" triggerAction="hover" side="top" alignment="center">
  <ng-template>
    <ion-content class="ion-padding">Tooltip de ayuda</ion-content>
  </ng-template>
</ion-popover>
```

```html
<!-- triggerAction: context-menu (clic derecho) -->
<ion-button id="ctx-trigger">Clic derecho</ion-button>

<ion-popover trigger="ctx-trigger" triggerAction="context-menu">
  <ng-template>
    <ion-content>
      <ion-list lines="none">
        <ion-item [button]="true"><ion-label>Copiar</ion-label></ion-item>
        <ion-item [button]="true"><ion-label>Pegar</ion-label></ion-item>
      </ion-list>
    </ion-content>
  </ng-template>
</ion-popover>
```

```html
<!-- Popover como menú dropdown con dismissOnSelect -->
<ion-button id="menu-button">
  Menú
  <ion-icon slot="end" name="chevron-down"></ion-icon>
</ion-button>

<ion-popover trigger="menu-button" [dismissOnSelect]="true" size="cover">
  <ng-template>
    <ion-content>
      <ion-list lines="none">
        <ion-item [button]="true" [detail]="false" (click)="editItem()">
          <ion-icon name="pencil" slot="start"></ion-icon>
          <ion-label>Editar</ion-label>
        </ion-item>
        <ion-item [button]="true" [detail]="false" (click)="duplicateItem()">
          <ion-icon name="copy" slot="start"></ion-icon>
          <ion-label>Duplicar</ion-label>
        </ion-item>
        <ion-item [button]="true" [detail]="false" (click)="deleteItem()">
          <ion-icon name="trash" slot="start" color="danger"></ion-icon>
          <ion-label color="danger">Eliminar</ion-label>
        </ion-item>
      </ion-list>
    </ion-content>
  </ng-template>
</ion-popover>
```

```html
<!-- Con isOpen reactivo -->
<ion-button (click)="openPopover($event)">Abrir</ion-button>

<ion-popover [isOpen]="isPopoverOpen" [event]="popoverEvent" (didDismiss)="isPopoverOpen = false">
  <ng-template>
    <ion-content class="ion-padding">Contenido</ion-content>
  </ng-template>
</ion-popover>
```

## Component TS

```typescript
import { IonPopover, IonButton, IonContent } from '@ionic/angular/standalone';
import { PopoverController } from '@ionic/angular/standalone';

@Component({ imports: [IonPopover, IonButton, IonContent, IonList, IonItem, IonLabel, IonIcon] })
export class PopoverPage {
  isPopoverOpen = false;
  popoverEvent?: Event;

  openPopover(event: Event) {
    this.popoverEvent = event;
    this.isPopoverOpen = true;
  }

  editItem() { /* lógica */ }
  duplicateItem() { /* lógica */ }
  deleteItem() { /* lógica */ }
}

// Programático con controlador
@Component({...})
export class ProgrammaticPopover {
  constructor(private popoverCtrl: PopoverController) {}

  async showMenu(event: Event) {
    const popover = await this.popoverCtrl.create({
      component: PopoverMenuComponent,
      event,
      translucent: true,
    });
    await popover.present();

    const { data } = await popover.onDidDismiss();
    console.log('Retornó:', data);
  }
}
```

## Notas

- **triggerAction**: `click` (default), `hover`, `context-menu`.
- **side**: `top`, `bottom`, `left`, `right`, `start`, `end`.
- **alignment**: `start`, `center`, `end` — alineación relativa al trigger.
- **reference**: `trigger` (default), `event` — desde dónde se calcula la posición.
- **size**: `auto` (default), `cover` — `cover` hace el popover del mismo ancho que el trigger.
- `[dismissOnSelect]="true"` → cierra automáticamente al seleccionar un ion-item.
- **CSS vars**: `--background`, `--backdrop-opacity`, `--box-shadow`, `--width`, `--offset-x`, `--offset-y`.
- **Imports TS**: `IonPopover, IonButton, IonContent` | con menú lista: `+ IonList, IonItem, IonLabel, IonIcon` | controller: `PopoverController`
