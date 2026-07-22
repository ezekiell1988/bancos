# Reference: ion-content

## Uso básico

```html
<!-- Básico con padding -->
<ion-content class="ion-padding">
  <h1>Título</h1>
  <p>Contenido scrollable.</p>
</ion-content>
```

```html
<!-- Fullscreen: contenido se desplaza detrás del header (efecto parallax iOS) -->
<ion-header translucent>
  <ion-toolbar>
    <ion-title>Fullscreen Header</ion-title>
  </ion-toolbar>
</ion-header>
<ion-content [fullscreen]="true" class="ion-padding">
  <ion-header collapse="condense">
    <ion-toolbar>
      <ion-title size="large">Fullscreen Header</ion-title>
    </ion-toolbar>
  </ion-header>
  <p>Contenido que se desplaza debajo del header.</p>
</ion-content>
```

```html
<!-- Fixed slot: elementos no scrollables dentro del contenido -->
<ion-content class="ion-padding">
  <ion-fab slot="fixed" vertical="bottom" horizontal="end">
    <ion-fab-button><ion-icon name="add"></ion-icon></ion-fab-button>
  </ion-fab>
  <p>Contenido scrollable...</p>
</ion-content>
```

```html
<!-- Scroll events -->
<ion-content
  [scrollEvents]="true"
  (ionScrollStart)="onScrollStart()"
  (ionScroll)="onScroll($any($event))"
  (ionScrollEnd)="onScrollEnd()"
  class="ion-padding">
</ion-content>
```

## Component TS

```typescript
import { ViewChild } from '@angular/core';
import { IonContent } from '@ionic/angular/standalone';

@Component({ imports: [IonContent] })
export class MyPage {
  @ViewChild(IonContent) content!: IonContent;

  scrollToBottom() {
    this.content.scrollToBottom(500); // 500ms duración
  }

  scrollToTop() {
    this.content.scrollToTop(300);
  }

  scrollToPoint() {
    this.content.scrollToPoint(0, 500, 400);
  }
}
```

## Notas

- **Propiedades clave**: `fullscreen` (boolean), `scrollEvents` (boolean, deshabilitado por defecto), `scrollX`, `scrollY`, `color`.
- **Métodos**: `scrollToBottom(duration?)`, `scrollToTop(duration?)`, `scrollToPoint(x, y, duration?)`, `scrollByPoint(x, y, duration?)`, `getScrollElement()`.
- **Events**: `ionScrollStart`, `ionScroll`, `ionScrollEnd` — requieren `[scrollEvents]="true"`.
- `slot="fixed"` dentro de `ion-content` → el elemento no scrollea.
- **CSS vars**: `--background`, `--color`, `--padding-top`, `--padding-bottom`, `--padding-start`, `--padding-end`.
- **Imports TS**: `IonContent` | con layout completo: `IonContent, IonHeader, IonFooter, IonToolbar, IonTitle`
