# Reference: ion-header / ion-toolbar / ion-title / ion-footer

## Uso básico

```html
<!-- Estructura mínima de página -->
<ion-header>
  <ion-toolbar>
    <ion-title>Mi Página</ion-title>
  </ion-toolbar>
</ion-header>
<ion-content class="ion-padding">
  <p>Contenido principal aquí.</p>
</ion-content>
```

```html
<!-- Con botones en la toolbar -->
<ion-header>
  <ion-toolbar>
    <ion-buttons slot="start">
      <ion-back-button defaultHref="/home"></ion-back-button>
    </ion-buttons>
    <ion-title>Detalle</ion-title>
    <ion-buttons slot="end">
      <ion-button fill="clear" (click)="save()">
        <ion-icon slot="icon-only" name="checkmark" aria-label="Guardar"></ion-icon>
      </ion-button>
    </ion-buttons>
  </ion-toolbar>
</ion-header>
```

```html
<!-- Con footer -->
<ion-header>
  <ion-toolbar>
    <ion-title>Página con Footer</ion-title>
  </ion-toolbar>
</ion-header>
<ion-content class="ion-padding">
  <p>Contenido scrollable.</p>
</ion-content>
<ion-footer>
  <ion-toolbar>
    <ion-title size="small">Pie de página</ion-title>
  </ion-toolbar>
</ion-footer>
```

```html
<!-- Header con color de marca -->
<ion-header>
  <ion-toolbar color="primary">
    <ion-title>Título Primario</ion-title>
  </ion-toolbar>
</ion-header>
```

## Notas

- `ion-buttons slot="start"` → botones a la izquierda (inicio).
- `ion-buttons slot="end"` → botones a la derecha (final).
- `ion-back-button [defaultHref]` → fallback si no hay historial de navegación.
- `ion-title [size]="large"` → título grande colapsable (iOS parallax header).
- `ion-toolbar` acepta `color` para cambiar el fondo completo.
- **Imports TS**: `IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonBackButton, IonIcon, IonFooter`
