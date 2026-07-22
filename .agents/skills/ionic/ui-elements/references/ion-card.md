# Reference: ion-card

## Uso básico

```html
<!-- Tarjeta básica con header y contenido -->
<ion-card>
  <ion-card-header>
    <ion-card-title>Título de la tarjeta</ion-card-title>
    <ion-card-subtitle>Subtítulo</ion-card-subtitle>
  </ion-card-header>
  <ion-card-content>
    Descripción corta del contenido de la tarjeta.
  </ion-card-content>
</ion-card>
```

```html
<!-- Tarjeta con imagen (media card) -->
<ion-card>
  <img
    alt="Descripción de la imagen"
    src="https://picsum.photos/400/200"
  />
  <ion-card-header>
    <ion-card-title>Título con imagen</ion-card-title>
    <ion-card-subtitle>Subtítulo</ion-card-subtitle>
  </ion-card-header>
  <ion-card-content>
    Descripción complementaria a la imagen.
  </ion-card-content>
</ion-card>
```

```html
<!-- Tarjeta con botones de acción -->
<ion-card>
  <ion-card-header>
    <ion-card-title>Tarjeta con acciones</ion-card-title>
  </ion-card-header>
  <ion-card-content>
    Contenido descriptivo aquí.
  </ion-card-content>
  <ion-button fill="clear">Acción 1</ion-button>
  <ion-button fill="clear">Acción 2</ion-button>
</ion-card>
```

```html
<!-- Tarjeta clicable (botón / enlace) -->
<ion-card [button]="true" href="/detail">
  <ion-card-header>
    <ion-card-title>Tarjeta clicable</ion-card-title>
  </ion-card-header>
  <ion-card-content>Toca para ver el detalle.</ion-card-content>
</ion-card>
```

```html
<!-- Tarjeta con icon y color -->
<ion-card color="primary">
  <ion-card-header>
    <ion-card-title>Destacada</ion-card-title>
    <ion-card-subtitle>
      <ion-icon name="star"></ion-icon> Recomendado
    </ion-card-subtitle>
  </ion-card-header>
  <ion-card-content>
    Tarjeta con color de marca.
  </ion-card-content>
</ion-card>
```

## Notas

- `[button]="true"` convierte la tarjeta en elemento clicable con efecto ripple.
- `href` o `(click)` para la acción de la tarjeta clicable.
- `color` acepta cualquier color Ionic: `primary`, `secondary`, `tertiary`, `success`, `warning`, `danger`, `light`, `medium`, `dark`.
- **CSS vars** en `ion-card`: `--background`, `--color`, `--border-radius`, `--box-shadow`.
- **CSS vars** en `ion-card-title`: `--color`.
- **CSS vars** en `ion-card-subtitle`: `--color`.
- **Imports TS**: `IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle` | con botones: `+ IonButton` | con iconos: `+ IonIcon`
