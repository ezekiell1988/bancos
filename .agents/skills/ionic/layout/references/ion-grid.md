# Reference: ion-grid / ion-row / ion-col

## Uso básico

```html
<!-- Columnas de ancho igual (automático) -->
<ion-grid>
  <ion-row>
    <ion-col>1 de 3</ion-col>
    <ion-col>2 de 3</ion-col>
    <ion-col>3 de 3</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Grid de ancho fijo (máximo responsive por breakpoint) -->
<ion-grid [fixed]="true">
  <ion-row>
    <ion-col>Col A</ion-col>
    <ion-col>Col B</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Tamaños específicos de columna (sistema de 12) -->
<ion-grid>
  <ion-row>
    <ion-col size="2">Angosta</ion-col>
    <ion-col size="8">Ancha</ion-col>
    <ion-col size="2">Angosta</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Responsive: cambia de tamaño según breakpoint -->
<ion-grid>
  <ion-row>
    <!-- En xs: 12 cols (stacked), en sm: 3 cols cada una -->
    <ion-col size="12" size-sm="3">A</ion-col>
    <ion-col size="12" size-sm="3">B</ion-col>
    <ion-col size="12" size-sm="3">C</ion-col>
    <ion-col size="12" size-sm="3">D</ion-col>
  </ion-row>
  <ion-row>
    <!-- Múltiples breakpoints -->
    <ion-col size="12" size-sm="6" size-md="4" size-lg="3">1</ion-col>
    <ion-col size="12" size-sm="6" size-md="4" size-lg="3">2</ion-col>
    <ion-col size="12" size-sm="6" size-md="4" size-lg="3">3</ion-col>
    <ion-col size="12" size-sm="6" size-md="12" size-lg="3">4</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Offset de columnas -->
<ion-grid>
  <ion-row>
    <ion-col>Columna 1</ion-col>
    <ion-col offset="3">Desplazada 3</ion-col>
    <ion-col>Columna 3</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Alineación vertical de filas -->
<ion-grid style="height: 200px;">
  <ion-row class="ion-align-items-start">
    <ion-col>Arriba</ion-col>
  </ion-row>
  <ion-row class="ion-align-items-center">
    <ion-col>Centro</ion-col>
  </ion-row>
  <ion-row class="ion-align-items-end">
    <ion-col>Abajo</ion-col>
  </ion-row>
</ion-grid>
```

```html
<!-- Alineación horizontal -->
<ion-grid>
  <ion-row class="ion-justify-content-center">
    <ion-col size="4">Centrado</ion-col>
  </ion-row>
  <ion-row class="ion-justify-content-between">
    <ion-col size="4">Izquierdo</ion-col>
    <ion-col size="4">Derecho</ion-col>
  </ion-row>
  <ion-row class="ion-justify-content-around">
    <ion-col size="4">A</ion-col>
    <ion-col size="4">B</ion-col>
  </ion-row>
</ion-grid>
```

## Notas

- **Breakpoints**: xs (0px), sm (576px), md (768px), lg (992px), xl (1200px).
- **Props de ion-col**: `size`, `size-sm`, `size-md`, `size-lg`, `size-xl`, `offset`, `offset-sm`, `offset-md`, `push`, `pull`.
- **Clases de ion-row** (vertical): `ion-align-items-start`, `ion-align-items-center`, `ion-align-items-end`, `ion-align-items-stretch`, `ion-align-items-baseline`.
- **Clases de ion-row** (horizontal): `ion-justify-content-start`, `ion-justify-content-center`, `ion-justify-content-end`, `ion-justify-content-between`, `ion-justify-content-around`, `ion-justify-content-evenly`.
- **CSS vars**: `--ion-grid-padding`, `--ion-grid-columns` (default 12), `--ion-grid-width`, `--ion-grid-column-padding`.
- **Imports TS**: `IonCol, IonGrid, IonRow`
