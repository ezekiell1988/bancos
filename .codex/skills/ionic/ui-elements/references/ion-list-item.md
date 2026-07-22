# Reference: ion-list / ion-item / ion-label

## Uso básico

```html
<!-- Lista básica -->
<ion-list>
  <ion-item>
    <ion-label>Pokémon Yellow</ion-label>
  </ion-item>
  <ion-item>
    <ion-label>Super Metroid</ion-label>
  </ion-item>
  <ion-item>
    <ion-label>Mega Man X</ion-label>
  </ion-item>
</ion-list>
```

```html
<!-- Lista inset (bordes redondeados en iOS — requiere fondo gris) -->
<ion-content color="light">
  <ion-list [inset]="true">
    <ion-item>
      <ion-label>Elemento 1</ion-label>
    </ion-item>
    <ion-item>
      <ion-label>Elemento 2</ion-label>
    </ion-item>
  </ion-list>
</ion-content>
```

```html
<!-- Variantes de línea separadora -->
<ion-list lines="full">   <!-- línea completa (default MD) -->
  <ion-item><ion-label>Full lines</ion-label></ion-item>
</ion-list>
<ion-list lines="inset">  <!-- línea con sangría -->
  <ion-item><ion-label>Inset lines</ion-label></ion-item>
</ion-list>
<ion-list lines="none">   <!-- sin líneas -->
  <ion-item><ion-label>No lines</ion-label></ion-item>
</ion-list>
```

```html
<!-- Ítem clicable con routing -->
<ion-list>
  <ion-item [button]="true" [detail]="true" routerLink="/detail/1">
    <ion-label>Ir al detalle</ion-label>
  </ion-item>
</ion-list>
```

```html
<!-- Ítem con slots start/end (iconos, avatares, botones) -->
<ion-list>
  <ion-item>
    <ion-icon name="person" slot="start" color="primary"></ion-icon>
    <ion-label>
      <h2>Juan García</h2>
      <p>Desarrollador Front-end</p>
    </ion-label>
    <ion-button fill="clear" slot="end">
      <ion-icon name="call" slot="icon-only" aria-label="Llamar"></ion-icon>
    </ion-button>
  </ion-item>
</ion-list>
```

```html
<!-- Ítem con nota/etiqueta al final -->
<ion-list>
  <ion-item>
    <ion-label>Batería</ion-label>
    <ion-note slot="end" color="success">85%</ion-note>
  </ion-item>
  <ion-item>
    <ion-label>Almacenamiento</ion-label>
    <ion-note slot="end" color="danger">¡Lleno!</ion-note>
  </ion-item>
</ion-list>
```

```html
<!-- Lista con header y footer de grupo -->
<ion-list>
  <ion-list-header lines="inset">
    <ion-label>Ingredientes</ion-label>
  </ion-list-header>
  <ion-item><ion-label>Harina</ion-label></ion-item>
  <ion-item><ion-label>Azúcar</ion-label></ion-item>
  <ion-item-divider>
    <ion-label>Sección 2</ion-label>
  </ion-item-divider>
  <ion-item><ion-label>Huevos</ion-label></ion-item>
</ion-list>
```

## Notas

- `[button]="true"` en `ion-item` hace el elemento clicable con efecto hover/ripple.
- `[detail]="true"` muestra la flecha de detalle (>). Por defecto aparece en iOS si `button=true`.
- `ion-label` dentro de `ion-item` puede contener `<h2>`, `<h3>`, `<p>` para layout multilínea.
- `ion-note` es para textos secundarios/informativos. `slot="end"` lo coloca a la derecha.
- `ion-list-header` → encabezado de sección visual (sticky opcional).
- `ion-item-divider` → separador de sección con label.
- Método en `ion-list`: `closeSlidingItems()` → cierra todos los `ion-item-sliding` abiertos.
- **Imports TS básico**: `IonItem, IonLabel, IonList`
- **Imports TS avanzado**: `IonItem, IonLabel, IonList, IonIcon, IonButton, IonNote, IonListHeader, IonItemDivider`
