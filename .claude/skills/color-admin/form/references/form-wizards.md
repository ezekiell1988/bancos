# Reference: Form Wizards

Fuente: `color-admin/template_angularjs20/src/app/pages/form/form-wizards/form-wizards.html`

Color Admin ofrece tres variantes de wizard para flujos multi-paso.
**No se necesita CSS personalizado** — todas las clases están en `vendor.min.css`.

---

## Estados de los Pasos

| Clase CSS    | Descripción              |
|-------------|--------------------------|
| `completed` | Paso completado/anterior |
| `active`    | Paso actual activo       |
| `disabled`  | Paso pendiente/bloqueado |

---

## Wizard Layout 1 — Número + Texto

Muestra un número de paso (`nav-no`) y un texto descriptivo (`nav-text`).

```html
<div class="nav-wizards-container">
  <nav class="nav nav-wizards-1 mb-2">
    <div class="nav-item col">
      <a class="nav-link completed" href="javascript:;">
        <div class="nav-no">1</div>
        <div class="nav-text">Completed step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link completed" href="javascript:;">
        <div class="nav-no">2</div>
        <div class="nav-text">Second step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link active" href="javascript:;">
        <div class="nav-no">3</div>
        <div class="nav-text">Active step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-no">4</div>
        <div class="nav-text">Disabled step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-no">5</div>
        <div class="nav-text">Last step</div>
      </a>
    </div>
  </nav>
</div>
<div class="card">
  <div class="card-body">
    wizard content here
  </div>
</div>
```

---

## Wizard Layout 2 — Solo Texto

Solo muestra texto de paso (`nav-text`), sin número ni punto.

```html
<div class="nav-wizards-container">
  <nav class="nav nav-wizards-2 mb-3">
    <div class="nav-item col">
      <a class="nav-link completed" href="javascript:;">
        <div class="nav-text">1. Completed step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link active" href="javascript:;">
        <div class="nav-text">2. Active step text</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-text">3. Disabled step text</div>
      </a>
    </div>
  </nav>
</div>
<div class="card">
  <div class="card-body">
    wizard content here
  </div>
</div>
```

---

## Wizard Layout 3 — Punto + Título + Subtexto

Muestra un punto decorativo (`nav-dot`), un título (`nav-title`) y un subtexto (`nav-text`).

```html
<div class="nav-wizards-container">
  <nav class="nav nav-wizards-3 mb-2">
    <div class="nav-item col">
      <a class="nav-link completed" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 1</div>
        <div class="nav-text">Completed step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link completed" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 2</div>
        <div class="nav-text">Second step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link active" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 3</div>
        <div class="nav-text">Active step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 4</div>
        <div class="nav-text">Disabled step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 5</div>
        <div class="nav-text">Last step</div>
      </a>
    </div>
  </nav>
</div>
<div class="card">
  <div class="card-body">
    wizard content here
  </div>
</div>
```

---

## Wizard Dinámico en Angular (Signals)

### Componente TypeScript

```typescript
import { Component, signal, computed, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-wizard',
  templateUrl: './wizard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WizardComponent {
  readonly steps = ['Información', 'Dirección', 'Confirmación'];
  readonly currentStep = signal(0);

  stepClass(i: number): string {
    const s = this.currentStep();
    if (i < s)  return 'completed';
    if (i === s) return 'active';
    return 'disabled';
  }

  next() { if (this.currentStep() < this.steps.length - 1) this.currentStep.update(s => s + 1); }
  prev() { if (this.currentStep() > 0)                     this.currentStep.update(s => s - 1); }
}
```

### Template Angular

```html
<!-- Usar layout 1 como base -->
<div class="nav-wizards-container">
  <nav class="nav nav-wizards-1 mb-2">
    @for (step of steps; track $index) {
      <div class="nav-item col">
        <a class="nav-link" [class]="stepClass($index)" href="javascript:;">
          <div class="nav-no">{{ $index + 1 }}</div>
          <div class="nav-text">{{ step }}</div>
        </a>
      </div>
    }
  </nav>
</div>

<div class="card">
  <div class="card-body">
    <!-- Contenido dinámico según currentStep() -->
    @switch (currentStep()) {
      @case (0) { <!-- Paso 1 --> }
      @case (1) { <!-- Paso 2 --> }
      @case (2) { <!-- Paso 3 --> }
    }
  </div>
</div>

<!-- Navegación -->
<div class="mt-3 d-flex justify-content-between">
  <button class="btn btn-default w-100px"
          (click)="prev()"
          [disabled]="currentStep() === 0">
    Anterior
  </button>
  <button class="btn btn-primary w-100px"
          (click)="next()"
          [disabled]="currentStep() === steps.length - 1">
    Siguiente
  </button>
</div>
```

---

## Tabla de Clases

| Clase                 | Descripción                                  |
|----------------------|----------------------------------------------|
| `nav-wizards-container` | Contenedor obligatorio del wizard         |
| `nav nav-wizards-1`  | Wizard con número de paso y texto            |
| `nav nav-wizards-2`  | Wizard solo texto                            |
| `nav nav-wizards-3`  | Wizard con punto, título y subtexto          |
| `nav-item col`       | Item de paso (usa columna Bootstrap)         |
| `nav-link completed` | Estado: paso completado                      |
| `nav-link active`    | Estado: paso activo actual                   |
| `nav-link disabled`  | Estado: paso pendiente                       |
| `nav-no`             | Número del paso (solo `nav-wizards-1`)       |
| `nav-text`           | Texto descriptivo del paso                   |
| `nav-dot`            | Punto decorativo (solo `nav-wizards-3`)      |
| `nav-title`          | Título del paso (solo `nav-wizards-3`)       |
