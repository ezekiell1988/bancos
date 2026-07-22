# Reference: ion-modal

## Uso básico

```html
<!-- Modal con isOpen (control reactivo) -->
<ion-button (click)="openModal()">Abrir modal</ion-button>

<ion-modal [isOpen]="isModalOpen" (didDismiss)="isModalOpen = false">
  <ng-template>
    <ion-header>
      <ion-toolbar>
        <ion-title>Modal</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="isModalOpen = false">Cerrar</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      Contenido del modal aquí.
    </ion-content>
  </ng-template>
</ion-modal>
```

```html
<!-- Modal con trigger (sin TypeScript) -->
<ion-button id="open-modal">Abrir</ion-button>

<ion-modal trigger="open-modal" (willDismiss)="onWillDismiss($event)">
  <ng-template>
    <ion-header>
      <ion-toolbar>
        <ion-title>Título</ion-title>
        <ion-buttons slot="start">
          <ion-button (click)="cancelModal()">Cancelar</ion-button>
        </ion-buttons>
        <ion-buttons slot="end">
          <ion-button (click)="confirmModal()" [strong]="true">Confirmar</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <ion-input label="Nombre" labelPlacement="stacked" placeholder="Escribe aquí"
        #inputEl></ion-input>
    </ion-content>
  </ng-template>
</ion-modal>
```

```html
<!-- Sheet modal (se desliza desde abajo / bottom sheet) -->
<ion-button id="open-sheet">Abrir Sheet</ion-button>

<ion-modal
  trigger="open-sheet"
  [initialBreakpoint]="0.25"
  [breakpoints]="[0, 0.25, 0.5, 0.75, 1]"
  [handleBehavior]="'cycle'">
  <ng-template>
    <ion-content class="ion-padding">
      <h3>Bottom Sheet</h3>
      <p>Desliza hacia arriba para expandir.</p>
    </ion-content>
  </ng-template>
</ion-modal>
```

## Component TS

```typescript
import { ViewChild } from '@angular/core';
import { IonModal } from '@ionic/angular/standalone';
import { ModalController } from '@ionic/angular/standalone';
import { OverlayEventDetail } from '@ionic/core/components';

// Opción A: controlando el modal desde la vista con @ViewChild
@Component({ imports: [IonModal, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent] })
export class ParentPage {
  @ViewChild(IonModal) modal!: IonModal;

  cancelModal() {
    this.modal.dismiss(null, 'cancel');
  }

  confirmModal() {
    this.modal.dismiss({ name: 'Juan' }, 'confirm');
  }

  onWillDismiss(event: Event) {
    const ev = event as CustomEvent<OverlayEventDetail<{ name: string }>>;
    if (ev.detail.role === 'confirm') {
      console.log('Datos recibidos:', ev.detail.data);
    }
  }
}

// Opción B: controlador programático (abre un componente separado como modal)
@Component({...})
export class SomePage {
  constructor(private modalCtrl: ModalController) {}

  async openModal() {
    const modal = await this.modalCtrl.create({
      component: DetailComponent,
      componentProps: { id: 42 },
    });
    await modal.present();

    const { data, role } = await modal.onWillDismiss();
    if (role === 'confirm') {
      console.log('Retornó:', data);
    }
  }
}
```

## Notas

- `[initialBreakpoint]` + `[breakpoints]` → sheet modal (bottom sheet). El array debe incluir `0` para poder cerrar.
- `[presentingElement]` → card modal estilo iOS (el contenido sube para que se vea el modal encima): `presentingElement = document.querySelector('.ion-page')`.
- `canDismiss`: boolean o función async `(data?, role?) => Promise<boolean>` para controlar si se puede cerrar.
- `(willDismiss)` → antes de cerrar; `(didDismiss)` → después de cerrar.
- **CSS vars**: `--height`, `--min-height`, `--max-height`, `--border-radius`, `--box-shadow`, `--backdrop-opacity`.
- **Imports TS** (inline): `IonModal, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent`
- **Controller**: `ModalController` (importar de `@ionic/angular/standalone`)
