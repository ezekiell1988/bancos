# Reference: ion-accordion

## Uso básico

```html
<!-- Accordion básico -->
<ion-accordion-group>
  <ion-accordion value="first">
    <ion-item slot="header" color="light">
      <ion-label>Primer grupo</ion-label>
    </ion-item>
    <div class="ion-padding" slot="content">
      Este es el contenido del primer acordeón.
    </div>
  </ion-accordion>

  <ion-accordion value="second">
    <ion-item slot="header" color="light">
      <ion-label>Segundo grupo</ion-label>
    </ion-item>
    <div class="ion-padding" slot="content">
      Este es el contenido del segundo acordeón.
    </div>
  </ion-accordion>
</ion-accordion-group>
```

```html
<!-- Múltiples acordeones abiertos simultáneamente -->
<ion-accordion-group [multiple]="true" [value]="['first', 'third']">
  <ion-accordion value="first">
    <ion-item slot="header" color="light"><ion-label>Uno</ion-label></ion-item>
    <div class="ion-padding" slot="content">Contenido uno.</div>
  </ion-accordion>
  <ion-accordion value="second">
    <ion-item slot="header" color="light"><ion-label>Dos</ion-label></ion-item>
    <div class="ion-padding" slot="content">Contenido dos.</div>
  </ion-accordion>
  <ion-accordion value="third">
    <ion-item slot="header" color="light"><ion-label>Tres</ion-label></ion-item>
    <div class="ion-padding" slot="content">Contenido tres (abierto por defecto).</div>
  </ion-accordion>
</ion-accordion-group>
```

```html
<!-- Expand style: inset o compact -->
<ion-accordion-group expand="inset">
  <ion-accordion value="a">
    <ion-item slot="header"><ion-label>Inset</ion-label></ion-item>
    <div class="ion-padding" slot="content">Contenido.</div>
  </ion-accordion>
</ion-accordion-group>
```

## Component TS

```typescript
import { IonAccordion, IonAccordionGroup, IonItem, IonLabel } from '@ionic/angular/standalone';
import { ViewChild } from '@angular/core';

@Component({ imports: [IonAccordion, IonAccordionGroup, IonItem, IonLabel] })
export class AccordionPage {
  @ViewChild(IonAccordionGroup) accordionGroup!: IonAccordionGroup;

  openFirst() {
    this.accordionGroup.value = 'first'; // abrir programáticamente
  }

  closeAll() {
    this.accordionGroup.value = undefined; // cerrar todos
  }
}
```

## Notas

- `slot="header"` → el disparador del acordeón (normalmente un `ion-item`).
- `slot="content"` → el contenido colapsable.
- `expand` en `ion-accordion-group`: `undefined` (default), `"inset"` (padding lateral), `"compact"` (sin gaps).
- `[multiple]="true"` → permite varios abiertos. El `value` del grupo entonces es un array de strings.
- `(ionChange)` en `ion-accordion-group` → se emite cuando cambia el acordeón activo.
- **CSS Parts**: `header`, `content`, `expanded` — para customización con `::ng-deep` o Shadow DOM.
- **Imports TS**: `IonAccordion, IonAccordionGroup, IonItem, IonLabel`
