# Reference: ion-input

## Uso básico

```html
<!-- Básico dentro de ion-item (estilo tradicional) -->
<ion-list>
  <ion-item>
    <ion-input label="Nombre" placeholder="Ingresa tu nombre"></ion-input>
  </ion-item>
  <ion-item>
    <ion-input label="Solo lectura" value="Juan García" [readonly]="true"></ion-input>
  </ion-item>
  <ion-item>
    <ion-input label="Deshabilitado" [disabled]="true"></ion-input>
  </ion-item>
</ion-list>
```

```html
<!-- Label placements (posición del label) -->
<!-- start: label a la izquierda (default dentro de ion-item) -->
<ion-input label="Label izquierda" labelPlacement="start" placeholder="Texto..."></ion-input>

<!-- fixed: label con ancho fijo, alineado -->
<ion-input label="Label fijo" labelPlacement="fixed" placeholder="Texto..."></ion-input>

<!-- stacked: label arriba del input -->
<ion-input label="Label arriba" labelPlacement="stacked" placeholder="Texto..."></ion-input>

<!-- floating: label flota cuando hay valor -->
<ion-input label="Label flotante" labelPlacement="floating" placeholder="Texto..."></ion-input>
```

```html
<!-- Fill styles (standalone, SIN ion-item) -->
<ion-input
  label="Email"
  labelPlacement="floating"
  fill="solid"
  type="email"
  placeholder="correo@ejemplo.com">
</ion-input>

<ion-input
  label="Contraseña"
  labelPlacement="floating"
  fill="outline"
  type="password">
</ion-input>
```

```html
<!-- Con validación y textos de ayuda/error -->
<ion-input
  type="email"
  fill="solid"
  label="Correo electrónico"
  labelPlacement="floating"
  helperText="Ingresa un email válido"
  errorText="Email inválido"
  ngModel
  name="email"
  email>
</ion-input>

<ion-input
  type="text"
  fill="outline"
  label="Nombre de usuario"
  labelPlacement="floating"
  helperText="Mínimo 3 caracteres"
  errorText="Nombre muy corto"
  ngModel
  name="username"
  minlength="3">
</ion-input>
```

```html
<!-- Con botón de limpiar -->
<ion-input
  label="Búsqueda"
  labelPlacement="stacked"
  type="search"
  [clearInput]="true"
  placeholder="Buscar...">
</ion-input>
```

```html
<!-- Con contador de caracteres -->
<ion-input
  label="Descripción"
  labelPlacement="floating"
  fill="outline"
  [counter]="true"
  maxlength="100"
  placeholder="Máximo 100 caracteres">
</ion-input>
```

```html
<!-- Con slots start/end (iconos y botones adicionales) -->
<ion-input
  labelPlacement="stacked"
  label="Contraseña"
  [type]="showPassword ? 'text' : 'password'"
  placeholder="Ingresa tu contraseña">
  <ion-icon slot="start" name="lock-closed" aria-hidden="true"></ion-icon>
  <ion-button
    fill="clear"
    slot="end"
    (click)="showPassword = !showPassword"
    [attr.aria-label]="showPassword ? 'Ocultar' : 'Mostrar'">
    <ion-icon
      slot="icon-only"
      [name]="showPassword ? 'eye-off' : 'eye'"
      aria-hidden="true">
    </ion-icon>
  </ion-button>
</ion-input>
```

```html
<!-- Formulario reactivo (Angular Reactive Forms) -->
<form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
  <ion-input
    fill="solid"
    label="Email"
    labelPlacement="floating"
    type="email"
    formControlName="email"
    helperText="ejemplo@correo.com"
    errorText="Email requerido o inválido">
  </ion-input>
  <ion-input
    fill="solid"
    label="Contraseña"
    labelPlacement="floating"
    type="password"
    formControlName="password"
    errorText="La contraseña es requerida">
  </ion-input>
  <ion-button type="submit" expand="block" [disabled]="loginForm.invalid">
    Iniciar sesión
  </ion-button>
</form>
```

## Component TS

```typescript
import { IonInput, IonItem, IonList, IonButton, IonIcon } from '@ionic/angular/standalone';
import { ViewChild } from '@angular/core';
import { addIcons } from 'ionicons';
import { lockClosed, eye, eyeOff } from 'ionicons/icons';

@Component({ imports: [IonInput, IonItem, IonList, IonButton, IonIcon] })
export class InputPage {
  @ViewChild(IonInput) input!: IonInput;
  showPassword = false;

  constructor() {
    addIcons({ lockClosed, eye, eyeOff });
  }

  focusInput() {
    this.input.setFocus();
  }

  async getElement() {
    const nativeInput = await this.input.getInputElement();
    nativeInput.select(); // seleccionar todo el texto
  }

  onInputChange(event: Event) {
    const value = (event as CustomEvent).detail.value;
    console.log('Valor:', value);
  }
}
```

## Notas

- **labelPlacement**: `start` (izquierda, default en `ion-item`), `end`, `fixed` (ancho fijo), `floating` (anima al enfocar), `stacked` (siempre arriba).
- **fill**: `solid`, `outline` — **solo para standalone** (fuera de `ion-item`). Dentro de `ion-item` no usar `fill`.
- **Tipos**: `text`, `password`, `email`, `number`, `search`, `tel`, `url`, `date`, `time`.
- **Eventos**: `(ionInput)` cada tecla, `(ionChange)` al perder foco o confirmar, `(ionFocus)`, `(ionBlur)`.
- **Métodos**: `setFocus()`, `getInputElement()` → async, retorna el `HTMLInputElement` nativo.
- `helperText` visible siempre; `errorText` visible solo cuando el input tiene clase `ion-invalid ion-touched`.
- `[clearInput]="true"` → botón X para limpiar (solo cuando hay valor y el input tiene foco).
- `[counter]="true"` + `maxlength` → muestra contador de caracteres.
- **CSS vars**: `--background`, `--color`, `--placeholder-color`, `--placeholder-opacity`, `--border-color`, `--border-radius`, `--highlight-color-focused`, `--padding-top`, `--padding-bottom`, `--padding-start`, `--padding-end`.
- **Imports TS básico**: `IonInput` | dentro de lista: `IonInput, IonItem, IonList` | con slots: `+ IonButton, IonIcon`
