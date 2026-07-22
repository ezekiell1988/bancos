# Reference: ion-menu

## Uso básico

```html
<!-- Menú lateral básico -->
<ion-menu contentId="main-content">
  <ion-header>
    <ion-toolbar color="primary">
      <ion-title>Menú</ion-title>
    </ion-toolbar>
  </ion-header>
  <ion-content class="ion-padding">
    <ion-list>
      <ion-item button routerLink="/home" (click)="closeMenu()">
        <ion-icon name="home" slot="start"></ion-icon>
        <ion-label>Inicio</ion-label>
      </ion-item>
      <ion-item button routerLink="/profile" (click)="closeMenu()">
        <ion-icon name="person" slot="start"></ion-icon>
        <ion-label>Perfil</ion-label>
      </ion-item>
    </ion-list>
  </ion-content>
</ion-menu>

<!-- Página principal con id coincidente -->
<div class="ion-page" id="main-content">
  <ion-header>
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-menu-button></ion-menu-button>
      </ion-buttons>
      <ion-title>Mi App</ion-title>
    </ion-toolbar>
  </ion-header>
  <ion-content class="ion-padding">
    <p>Contenido principal.</p>
  </ion-content>
</div>
```

```html
<!-- Tipos de menú -->
<!-- overlay: se superpone (default iOS), reveal: empuja el contenido, push: desliza junto al contenido -->
<ion-menu type="push" contentId="main-content">...</ion-menu>
<ion-menu type="reveal" contentId="main-content">...</ion-menu>
<ion-menu type="overlay" contentId="main-content">...</ion-menu>
```

```html
<!-- Menú en el lado derecho -->
<ion-menu side="end" contentId="main-content">
  <ion-content class="ion-padding">Menú derecho</ion-content>
</ion-menu>
```

```html
<!-- ion-menu-toggle en cualquier elemento para abrir/cerrar -->
<ion-menu-toggle>
  <ion-button>Abrir/Cerrar Menú</ion-button>
</ion-menu-toggle>
```

```html
<!-- Múltiples menús -->
<ion-menu menuId="start-menu" side="start" contentId="main-content">...</ion-menu>
<ion-menu menuId="end-menu" side="end" contentId="main-content">...</ion-menu>
```

## Component TS

```typescript
import { MenuController } from '@ionic/angular/standalone';
import { IonMenu, IonMenuButton, IonMenuToggle } from '@ionic/angular/standalone';

@Component({ imports: [IonMenu, IonMenuButton, IonMenuToggle] })
export class AppComponent {
  constructor(private menuCtrl: MenuController) {}

  openMenu() {
    this.menuCtrl.open(); // abre el menú por defecto
  }

  openSpecificMenu() {
    this.menuCtrl.open('end-menu'); // abre por menuId
  }

  closeMenu() {
    this.menuCtrl.close();
  }

  toggleMenu() {
    this.menuCtrl.toggle();
  }
}
```

## Notas

- `ion-menu-button` abre/cierra automáticamente el menú `side="start"` más cercano. No requiere lógica extra.
- `ion-menu-toggle` puede envolver cualquier elemento para hacer toggle del menú.
- `contentId` en `ion-menu` debe coincidir con el `id` del elemento raíz de la página.
- **Tipos**: `overlay` (por defecto), `reveal` (iOS reveal), `push` (desliza contenido).
- **CSS vars**: `--background`, `--width`, `--min-width`, `--max-width`.
- **CSS parts**: `backdrop`, `container`.
- **Imports TS**: `IonMenu, IonMenuButton, IonMenuToggle, IonButtons, IonContent, IonHeader, IonTitle, IonToolbar` | controller: `MenuController`
