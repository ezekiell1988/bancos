# Reference: ion-breadcrumbs

## Uso básico

```html
<!-- Breadcrumbs básicos -->
<ion-breadcrumbs>
  <ion-breadcrumb href="/home">Inicio</ion-breadcrumb>
  <ion-breadcrumb href="/electronics">Electrónica</ion-breadcrumb>
  <ion-breadcrumb href="/cameras">Cámaras</ion-breadcrumb>
  <ion-breadcrumb>Detalle</ion-breadcrumb>
</ion-breadcrumbs>
```

```html
<!-- Con iconos -->
<ion-breadcrumbs>
  <ion-breadcrumb href="/home">
    <ion-icon slot="start" name="home"></ion-icon>
    Inicio
  </ion-breadcrumb>
  <ion-breadcrumb href="/category">
    <ion-icon slot="start" name="folder"></ion-icon>
    Categoría
  </ion-breadcrumb>
  <ion-breadcrumb>
    <ion-icon slot="start" name="document"></ion-icon>
    Elemento actual
  </ion-breadcrumb>
</ion-breadcrumbs>
```

```html
<!-- Separador personalizado -->
<ion-breadcrumbs>
  <ion-breadcrumb href="/home">
    Inicio
    <ion-icon slot="separator" name="arrow-forward-circle"></ion-icon>
  </ion-breadcrumb>
  <ion-breadcrumb href="/category">
    Categoría
    <ion-icon slot="separator" name="arrow-forward-circle"></ion-icon>
  </ion-breadcrumb>
  <ion-breadcrumb>Actual</ion-breadcrumb>
</ion-breadcrumbs>
```

```html
<!-- Con colapso y expansión dinámica -->
<ion-breadcrumbs
  [maxItems]="maxBreadcrumbs"
  [itemsBeforeCollapse]="1"
  [itemsAfterCollapse]="1"
  (ionCollapsedClick)="expandBreadcrumbs()">
  <ion-breadcrumb href="/home">Inicio</ion-breadcrumb>
  <ion-breadcrumb href="/cat">Categoría</ion-breadcrumb>
  <ion-breadcrumb href="/sub">Subcategoría</ion-breadcrumb>
  <ion-breadcrumb href="/item">Ítem</ion-breadcrumb>
  <ion-breadcrumb>Detalle</ion-breadcrumb>
</ion-breadcrumbs>
```

## Component TS

```typescript
import { IonBreadcrumbs, IonBreadcrumb } from '@ionic/angular/standalone';

@Component({ imports: [IonBreadcrumbs, IonBreadcrumb, IonIcon] })
export class BreadcrumbsPage {
  maxBreadcrumbs = 3;

  expandBreadcrumbs() {
    this.maxBreadcrumbs = Infinity; // muestra todos
  }
}
```

## Notas

- El último `ion-breadcrumb` (sin `href`) se muestra como el elemento activo actual (no es clicable por defecto).
- `maxItems`: número máximo antes de colapsar; `itemsBeforeCollapse` e `itemsAfterCollapse` controlan cuántos se muestran a cada lado del botón de expansión.
- El evento `(ionCollapsedClick)` recibe `CustomEvent<{ collapsedBreadcrumbs: HTMLIonBreadcrumbElement[] }>`.
- `color` en `ion-breadcrumbs` aplica a todo el conjunto.
- **Imports TS**: `IonBreadcrumbs, IonBreadcrumb` | con iconos: `+ IonIcon`
