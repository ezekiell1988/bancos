---
name: color-admin-components
description: Comprehensive guide for Color Admin reusable components including Panel and ngx-datatable implementations. Covers component configuration, customization, API integration, and best practices. Use when implementing or customizing Color Admin UI components in Angular applications.
---

# Color Admin Components Guide

Reusable and customizable components for Color Admin template in Angular applications.

## When to Use This Skill

- User needs to implement Color Admin Panel component
- User wants to use ngx-datatable with server-side pagination
- User asks about Color Admin component customization
- User needs data table implementation with filtering and sorting
- User wants to integrate panels with various content types
- User needs responsive table layouts

## Available Components

This skill covers the following Color Admin components:

### 1. Panel Component
- **Purpose**: Versatile container for encapsulating content
- **Features**: Expand, reload, collapse, remove actions
- **Customization**: Multiple variants, custom headers/footers
- **Content Projection**: Multiple ng-content slots

### 2. ngx-datatable Component
- **Purpose**: High-performance data table for large datasets
- **Features**: Server-side pagination, filtering, sorting
- **Virtual DOM**: Efficient rendering of thousands of rows
- **API Integration**: Built-in support for REST APIs

## Quick Start

### Panel Component Usage

```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-example',
  template: `
    <panel 
      title="Mi Panel" 
      variant="inverse"
      [noButton]="false">
      
      <!-- Contenido principal -->
      <p>Contenido del panel aquí</p>
      
      <!-- Footer (opcional) -->
      <div footer>
        <button class="btn btn-primary">Guardar</button>
      </div>
    </panel>
  `
})
export class ExampleComponent {}
```

### ngx-datatable Basic Usage

```typescript
import { Component, OnInit } from '@angular/core';
import { ColumnMode } from '@swimlane/ngx-datatable';

@Component({
  selector: 'app-data-table',
  template: `
    <ngx-datatable
      class="material"
      [rows]="rows"
      [columnMode]="ColumnMode.force"
      [headerHeight]="50"
      [footerHeight]="50"
      [rowHeight]="'auto'"
      [limit]="10">
      
      <ngx-datatable-column name="Name" prop="name"></ngx-datatable-column>
      <ngx-datatable-column name="Email" prop="email"></ngx-datatable-column>
    </ngx-datatable>
  `
})
export class DataTableComponent implements OnInit {
  ColumnMode = ColumnMode;
  rows: any[] = [];
  
  ngOnInit() {
    // Cargar datos
  }
}
```

## Component Features Overview

### Panel Component Features

| Feature | Description | Default |
|---------|-------------|---------|
| **Variants** | Color schemes (inverse, primary, success, etc.) | `inverse` |
| **Actions** | Expand, reload, collapse, remove buttons | Enabled |
| **Content Slots** | Multiple ng-content projection points | - |
| **No Body Mode** | Render without panel-body wrapper | `false` |
| **Custom Classes** | Add custom CSS to header, body, footer | - |

### ngx-datatable Features

| Feature | Description | Supported |
|---------|-------------|-----------|
| **Virtual Scrolling** | Renders only visible rows | ✅ |
| **Server Pagination** | API-based pagination | ✅ |
| **Filtering** | Client/server-side filtering | ✅ |
| **Sorting** | Multi-column sorting | ✅ |
| **Selection** | Single/multi row selection | ✅ |
| **Responsive** | Mobile-friendly layouts | ✅ |

## Panel Variants

Available color variants for Panel component:

```typescript
// Variantes disponibles
'inverse'    // Panel oscuro (default)
'default'    // Panel claro
'primary'    // Color primario (azul)
'success'    // Verde
'warning'    // Amarillo
'danger'     // Rojo
'info'       // Cian
```

## Content Projection Slots (Panel)

The Panel component provides multiple content projection slots:

```html
<panel title="Example">
  <!-- Slot 1: beforeBody -->
  <div beforeBody>
    Contenido antes del body principal
  </div>
  
  <!-- Slot 2: Default (panel-body) -->
  <p>Contenido principal del panel</p>
  
  <!-- Slot 3: outsideBody -->
  <div outsideBody>
    Contenido fuera del panel-body pero dentro del panel
  </div>

> ⚠️ **Slots del Panel — guía de sintaxis verificada en este proyecto**
>
> | Slot | Sintaxis correcta | Notas |
> |------|-------------------|-------|
> | `footer` | `<div footer>` | Sin `#`. Funciona con contenido simple. |
> | `beforeBody` | `<div beforeBody>` | Sin `#`. Funciona con contenido simple. |
> | `header` | `<div header>` | Sin `#`. |
> | `noBody` | `<div noBody>` | Sin `#`. |
> | `outsideBody` (contenido simple) | `<div outsideBody>` | Sin `#`. |
> | `outsideBody` + `ngx-datatable` SIN `@if` | `<div #outsideBody>` | **Con `#`.** Con `#`, Angular lo coloca en el slot default (`panel-body`), donde el datatable puede medir anchos correctamente. Sin `#`, la tabla no renderiza. |
> | `outsideBody` + `ngx-datatable` CON `@if` | `<ng-container outsideBody>` | Sin `#`. El `@if` actúa como trigger de inicialización. Ver `addresses.component.html`. |
>
> **Regla práctica**: si pones un `ngx-datatable` en `outsideBody` sin envolverlo en `@if (data.length > 0)`, usa `#outsideBody` (template ref). En todos los demás casos, usa el atributo sin `#`.
  
  <!-- Slot 4: footer -->
  <div footer>
    Contenido del footer
  </div>
</panel>
```

## Server-Side Pagination Pattern (ngx-datatable)

Common pattern for API integration:

```typescript
// Service
export class DataService {
  getData(page: number, size: number, sort?: string) {
    const params = {
      page: page.toString(),
      size: size.toString(),
      ...(sort && { sort })
    };
    
    return this.http.get<PagedResponse>('/api/data', { params });
  }
}

// Component
export class TableComponent {
  onPage(event: any) {
    this.page = event.offset;
    this.loadData();
  }
  
  loadData() {
    this.service.getData(this.page, this.pageSize, this.sort)
      .subscribe(response => {
        this.rows = response.content;
        this.totalElements = response.totalElements;
      });
  }
}
```

## Row Detail (Fila expandible)

`ngx-datatable-row-detail` permite mostrar contenido adicional expandible debajo de cada fila.

### Puntos críticos

- **`@ViewChild` por tipo**, NO por template ref `#nombre` — `DatatableRowDetailDirective` no tiene `exportAs`, Angular no resuelve la instancia por nombre de variable.
- **`ChangeDetectorRef.markForCheck()`** es obligatorio con `ChangeDetectionStrategy.OnPush` después de llamar a `toggleExpandRow`.
- El atributo `#rowDetail` en el template es solo decorativo/documentación; la referencia real viene del `@ViewChild(DatatableRowDetailDirective)`.

### Implementación

```typescript
import {
  ChangeDetectorRef,
  Component,
  ViewChild,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,
} from '@swimlane/ngx-datatable';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxDatatableModule],
})
export class MyComponent {
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);

  expandedKey = signal<string | null>(null);

  toggleExpand(row: MyRow): void {
    this.rowDetail.toggleExpandRow(row);
    this.expandedKey.update(k => k === row.key ? null : row.key);
    this.cdr.markForCheck();
  }
}
```

```html
<ngx-datatable [rows]="rows" [rowHeight]="'auto'">

  <!-- Row detail DEBE declararse antes de las columnas -->
  <ngx-datatable-row-detail rowHeight="auto" #rowDetail>
    <ng-template let-row="row" ngx-datatable-row-detail-template>
      <div class="p-2 ps-4">
        <!-- contenido expandido -->
        <pre class="bg-dark text-light rounded p-2">{{ row.value }}</pre>
      </div>
    </ng-template>
  </ngx-datatable-row-detail>

  <!-- Columna que dispara el toggle -->
  <ngx-datatable-column name="Clave" [resizeable]="false">
    <ng-template ngx-datatable-cell-template let-row="row">
      <button class="btn btn-link btn-sm p-0" (click)="toggleExpand(row)">
        <i [class]="expandedKey() === row.key ? 'fa fa-chevron-down' : 'fa fa-chevron-right'"></i>
        {{ row.key }}
      </button>
    </ng-template>
  </ngx-datatable-column>

</ngx-datatable>
```

### ⚠️ Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| Row detail no se abre | `@ViewChild('nombre')` devuelve `ElementRef` | Usar `@ViewChild(DatatableRowDetailDirective)` |
| Row detail se abre pero no actualiza UI | `OnPush` no detecta cambios imperativos | Llamar `cdr.markForCheck()` después del toggle |
| `rowDetail` es `undefined` | Componente con `*ngIf` que oculta el datatable | Acceder solo después de `ngAfterViewInit` |

---

## Best Practices

### Panel Component

1. **Use Appropriate Variants**: Match panel colors to content purpose
2. **Leverage Content Projection**: Use slots for flexible layouts
3. **Handle Actions**: Subscribe to panel events when needed
4. **Accessibility**: Ensure header text is descriptive

---

## Accessibility — axe / WCAG AA

### Form inputs sin label visible

No usar `[attr.aria-label]` solo — axe puede ejecutarse antes de que Angular resuelva el binding. Usar `<label class="visually-hidden" for="id">` + `id` en el input:

```html
<!-- ✅ axe siempre lo detecta -->
<label for="my-field" class="visually-hidden">{{ 'KEY' | translate }}</label>
<input id="my-field" type="text" class="form-control" ... />

<!-- ❌ axe puede fallar si el binding no está resuelto aún -->
<input [attr.aria-label]="'KEY' | translate" ... />
```

### Botones de solo icono

Agregar `<span class="visually-hidden">` con el texto traducido **dentro** del botón. El contenido del span forma parte del nombre accesible del botón y axe lo detecta siempre:

```html
<!-- ✅ accesible aunque las traducciones no hayan cargado aún -->
<button type="button" [title]="'KEY' | translate" [attr.aria-label]="'KEY' | translate">
  <i class="fa fa-search" aria-hidden="true"></i>
  <span class="visually-hidden">{{ 'KEY' | translate }}</span>
</button>

<!-- ❌ axe falla si el binding [title] no está resuelto -->
<button type="button" [title]="'KEY' | translate">
  <i class="fa fa-search"></i>
</button>
```

> **Regla práctica**: siempre marcar los `<i>` / `<svg>` con `aria-hidden="true"` para que los lectores de pantalla los ignoren y no mezclen el nombre del ícono con el texto accesible.

#### Botones con texto dinámico (en `@for` loops)

Cuando el texto del botón incluye un valor de la iteración (ej. nombre de carpeta), el `<span class="visually-hidden">` funciona igual con interpolación `{{ }}`:

```html
<!-- ✅ texto dinámico con el item del loop -->
@for (item of items(); track item.id) {
  <button type="button"
    [title]="'KEY' | translate:{ name: item.name }"
    [attr.aria-label]="'KEY' | translate:{ name: item.name }">
    <i class="fa fa-trash" aria-hidden="true"></i>
    <span class="visually-hidden">{{ 'KEY' | translate:{ name: item.name } }}</span>
  </button>
}
```

### Imágenes con `[alt]` dinámico

`[alt]="expr"` es un property binding — Angular escribe el atributo **después** de crear el elemento. Si axe escanea en ese instante intermedio, el `<img>` no tiene `alt` y la regla `image-alt` falla.

**Fix**: añadir `alt=""` estático como fallback. Angular lo sobreescribe con el valor real una vez que inicializa el componente. El `alt=""` vacío marca la imagen como decorativa para axe en el peor caso, evitando el error crítico.

```html
<!-- ✅ alt="" estático + [alt] dinámico — axe nunca encuentra img sin alt -->
<img alt=""
  [src]="item.thumbnailUrl"
  [alt]="item.name || item.key"
  [title]="item.name || item.key"
  [attr.aria-label]="item.name || item.key"
  class="rounded" />

<!-- ❌ axe falla si el binding no está resuelto cuando escanea -->
<img [alt]="item.name" [src]="item.thumbnailUrl" />
```

> **Regla práctica**: usar `||` en lugar de `??` al calcular el texto alternativo para que cadenas vacías (`""`) también activen el fallback — `??` solo aplica para `null`/`undefined`, no para `""`.

```typescript
// ✅ cubre null, undefined Y cadena vacía
getFileName(key: string): string {
  return key.split('/').pop() || key;
}

// ❌ no cubre cadena vacía (ej. key que termina en "/")
getFileName(key: string): string {
  return key.split('/').pop() ?? key;
}
```

### ngx-datatable

1. **Virtual Scrolling**: Always enable for large datasets (>100 rows)
2. **Server Pagination**: Use for datasets >1000 rows
3. **Column Configuration**: Set appropriate widths and flex values
4. **Loading States**: Show indicators during data fetch
5. **Error Handling**: Display user-friendly messages
6. **Responsive Design**: Test on mobile devices

## Common Patterns

### Panel with Table

```html
<panel title="Listado de Usuarios" variant="inverse">
  <ngx-datatable
    [rows]="users"
    [columnMode]="ColumnMode.force"
    [headerHeight]="50"
    [footerHeight]="50">
    <!-- columns -->
  </ngx-datatable>
</panel>
```

### Panel with Form

```html
<panel title="Crear Usuario" variant="primary">
  <form [formGroup]="userForm" (ngSubmit)="onSubmit()">
    <!-- form fields -->
  </form>
  
  <div footer>
    <button type="submit" class="btn btn-primary">Guardar</button>
    <button type="button" class="btn btn-default">Cancelar</button>
  </div>
</panel>
```

## Related Documentation

For detailed implementation guides and advanced patterns, refer to:

- **Panel Component**: Complete guide with all features and examples
- **ngx-datatable**: API integration, filtering, sorting, and optimization

## Next Steps

1. Review detailed component documentation in references folder
2. Implement basic panel with different variants
3. Set up ngx-datatable with server-side pagination
4. Customize components to match your design requirements
5. Optimize performance for large datasets

---

**Note**: This skill focuses on Color Admin specific implementations. For general Angular patterns, refer to the Angular-specific skills.

---

## Form Elements

Color Admin utiliza los elementos de formulario estándar de Bootstrap 5 con estilos propios.

### Estructura básica

```html
<div class="mb-3">
  <label class="form-label">Label</label>
  <input type="text" class="form-control" placeholder="Placeholder" />
</div>
```

### Tipos de inputs

```html
<input type="text" class="form-control" />
<input type="email" class="form-control" />
<input type="password" class="form-control" />
<input type="number" class="form-control" />
<textarea class="form-control" rows="3"></textarea>
<select class="form-select">
  <option>Opción 1</option>
</select>
<input type="file" class="form-control" />
```

### Floating Labels

```html
<div class="form-floating mb-3">
  <input type="email" class="form-control" id="floatingInput" placeholder="name@example.com" />
  <label for="floatingInput">Email address</label>
</div>
<div class="form-floating">
  <select class="form-select" id="floatingSelect">
    <option selected>Open this select menu</option>
  </select>
  <label for="floatingSelect">Works with selects</label>
</div>
```

### Readonly & Plaintext

```html
<input class="form-control" type="text" value="Valor fijo" readonly />
<input class="form-control-plaintext" type="text" value="Sin borde readonly" readonly />
```

### Form Range

```html
<input type="range" class="form-range" min="0" max="5" step="0.5" />
```

### Sizing

```html
<input class="form-control form-control-lg" type="text" placeholder="Large" />
<input class="form-control" type="text" placeholder="Default" />
<input class="form-control form-control-sm" type="text" placeholder="Small" />
```

### Validación

```html
<input type="text" class="form-control is-valid" />
<div class="valid-feedback">Se ve bien!</div>

<input type="text" class="form-control is-invalid" />
<div class="invalid-feedback">Por favor completa este campo.</div>
```

### Checkboxes

```html
<!-- Default -->
<div class="form-check">
  <input class="form-check-input" type="checkbox" id="check1" />
  <label class="form-check-label" for="check1">Default</label>
</div>

<!-- Switch -->
<div class="form-check form-switch">
  <input class="form-check-input" type="checkbox" id="switch1" />
  <label class="form-check-label" for="switch1">Toggle switch</label>
</div>

<!-- Inline -->
<div class="form-check form-check-inline">
  <input class="form-check-input" type="checkbox" id="inlineCheck1" />
  <label class="form-check-label" for="inlineCheck1">1</label>
</div>

<!-- Button-style -->
<div class="btn-group" role="group">
  <input type="checkbox" class="btn-check" id="btnCheck1" autocomplete="off" />
  <label class="btn btn-outline-primary" for="btnCheck1">Option 1</label>
  <input type="checkbox" class="btn-check" id="btnCheck2" autocomplete="off" checked />
  <label class="btn btn-outline-primary" for="btnCheck2">Option 2</label>
</div>
```

### Radios

```html
<!-- Default -->
<div class="form-check">
  <input class="form-check-input" type="radio" name="radios" id="radio1" />
  <label class="form-check-label" for="radio1">Radio 1</label>
</div>

<!-- Inline -->
<div class="form-check form-check-inline">
  <input class="form-check-input" type="radio" name="inlineRadios" id="inlineR1" />
  <label class="form-check-label" for="inlineR1">1</label>
</div>

<!-- Button-style -->
<div class="btn-group" role="group">
  <input type="radio" class="btn-check" name="btnRadios" id="btnR1" autocomplete="off" checked />
  <label class="btn btn-outline-primary" for="btnR1">Radio 1</label>
  <input type="radio" class="btn-check" name="btnRadios" id="btnR2" autocomplete="off" />
  <label class="btn btn-outline-primary" for="btnR2">Radio 2</label>
</div>
```

### Input Group

```html
<!-- Texto prepend -->
<div class="input-group mb-3">
  <span class="input-group-text">@</span>
  <input type="text" class="form-control" placeholder="Username" />
</div>

<!-- Botón append -->
<div class="input-group mb-3">
  <input type="text" class="form-control" placeholder="Buscar..." />
  <button class="btn btn-outline-secondary" type="button">
    <i class="fa fa-search"></i>
  </button>
</div>

<!-- Sizing -->
<div class="input-group input-group-lg mb-3">...</div>
<div class="input-group mb-3">...</div>
<div class="input-group input-group-sm">...</div>
```

### Form Layout - Horizontal

```html
<form>
  <div class="row mb-3">
    <label class="col-sm-2 col-form-label">Email</label>
    <div class="col-sm-10">
      <input type="email" class="form-control" />
    </div>
  </div>
</form>
```

### Form Layout - Inline

```html
<form class="row row-cols-lg-auto g-3 align-items-center">
  <div class="col-12">
    <input type="text" class="form-control" placeholder="Username" />
  </div>
  <div class="col-12">
    <button type="submit" class="btn btn-primary">Submit</button>
  </div>
</form>
```

---

## Form Wizards

Navegación paso a paso. Tres variantes visuales disponibles. La lógica de avance/retroceso se implementa en el componente Angular.

**Estados de los pasos:**

| Clase | Descripción |
|-------|-------------|
| `completed` | Paso ya completado (línea llena hacia el siguiente) |
| `active` | Paso actual (resaltado en color primario) |
| `disabled` | Paso no disponible aún (gris) |

### Wizard Layout 1 — Número + texto

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
      <a class="nav-link active" href="javascript:;">
        <div class="nav-no">2</div>
        <div class="nav-text">Active step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-no">3</div>
        <div class="nav-text">Disabled step</div>
      </a>
    </div>
  </nav>
</div>
```

### Wizard Layout 2 — Barra con texto

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
        <div class="nav-text">2. Active step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-text">3. Disabled step</div>
      </a>
    </div>
  </nav>
</div>
```

### Wizard Layout 3 — Punto + título + subtítulo

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
      <a class="nav-link active" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 2</div>
        <div class="nav-text">Active step</div>
      </a>
    </div>
    <div class="nav-item col">
      <a class="nav-link disabled" href="javascript:;">
        <div class="nav-dot"></div>
        <div class="nav-title">Step 3</div>
        <div class="nav-text">Disabled step</div>
      </a>
    </div>
  </nav>
</div>
```

### Lógica de pasos en Angular

```typescript
export class WizardComponent {
  currentStep = 1;
  totalSteps = 4;

  getStepClass(step: number): string {
    if (step < this.currentStep) return 'completed';
    if (step === this.currentStep) return 'active';
    return 'disabled';
  }

  next() { if (this.currentStep < this.totalSteps) this.currentStep++; }
  prev() { if (this.currentStep > 1) this.currentStep--; }
}
```

```html
<div class="nav-wizards-container">
  <nav class="nav nav-wizards-1 mb-2">
    <div class="nav-item col" *ngFor="let step of [1,2,3,4]">
      <a class="nav-link {{ getStepClass(step) }}" href="javascript:;">
        <div class="nav-no">{{ step }}</div>
        <div class="nav-text">Step {{ step }}</div>
      </a>
    </div>
  </nav>
</div>

<div class="card mb-3">
  <div class="card-body">
    <ng-container [ngSwitch]="currentStep">
      <div *ngSwitchCase="1">Contenido paso 1</div>
      <div *ngSwitchCase="2">Contenido paso 2</div>
      <div *ngSwitchCase="3">Contenido paso 3</div>
      <div *ngSwitchCase="4">Contenido paso 4</div>
    </ng-container>
  </div>
</div>

<div class="d-flex gap-2">
  <button class="btn btn-default" (click)="prev()" [disabled]="currentStep === 1">
    <i class="fa fa-arrow-left me-1"></i> Anterior
  </button>
  <button class="btn btn-primary ms-auto" (click)="next()" [disabled]="currentStep === totalSteps">
    Siguiente <i class="fa fa-arrow-right ms-1"></i>
  </button>
</div>
```

---

## Form Plugins

Resumen de plugins disponibles:

1. **[ngb-datepicker](#1-ngb-datepicker)** — Selector de fecha de Ng Bootstrap
2. **[ngb-timepicker](#2-ngb-timepicker)** — Selector de hora de Ng Bootstrap
3. **[Tagify](#3-tagify)** — Input de tags (@yaireo/tagify)
4. **[ngx-editor](#4-ngx-editor)** — Editor de texto enriquecido
5. **[ngx-color](#5-ngx-color)** — Color picker estilo Sketch

---

### 1. ngb-datepicker

**Paquete**: `@ng-bootstrap/ng-bootstrap` (ya incluido en Color Admin)

Adapter para usar `Date` nativo de JS en lugar de `NgbDateStruct`:

```typescript
import { Injectable } from '@angular/core';
import { NgbDateAdapter, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';

@Injectable()
export class NgbDateNativeAdapter extends NgbDateAdapter<Date> {
  fromModel(date: Date): NgbDateStruct {
    return (date && date.getFullYear)
      ? { year: date.getFullYear(), month: date.getMonth() + 1, day: date.getDate() }
      : null;
  }
  toModel(date: NgbDateStruct): Date {
    return date ? new Date(date.year, date.month - 1, date.day) : null;
  }
}

@Component({
  providers: [{ provide: NgbDateAdapter, useClass: NgbDateNativeAdapter }]
})
export class MyComponent {
  model: Date;
  get today() { return new Date(); }
}
```

```html
<!-- Calendario inline (model1) -->
<ngb-datepicker #d1 [(ngModel)]="model1" #c1="ngModel"></ngb-datepicker>
<button class="btn btn-sm btn-outline-primary" (click)="model1 = today">Select Today</button>
<pre>Model: {{ model1 | json }}</pre>
<pre>State: {{ c1.status }}</pre>

<!-- Input con botón toggle (model2) -->
<div class="input-group">
  <input class="form-control" placeholder="yyyy-mm-dd"
    name="d2" #c2="ngModel" [(ngModel)]="model2"
    ngbDatepicker #d2="ngbDatepicker" />
  <button class="btn btn-outline-secondary" (click)="d2.toggle()" type="button">
    <i class="fa fa-calendar"></i>
  </button>
</div>
<button class="btn btn-sm btn-outline-primary" (click)="model2 = today">Select Today</button>
```

---

### 2. ngb-timepicker

**Paquete**: `@ng-bootstrap/ng-bootstrap` (ya incluido)

```typescript
time = { hour: 13, minute: 30 };
meridian = true;

// Validación personalizada con FormControl
ctrl = new FormControl('', (control: FormControl) => {
  const value = control.value;
  if (!value) return null;
  if (value.hour < 12) return { tooEarly: true };
  if (value.hour > 13) return { tooLate: true };
  return null;
});
```

```html
<!-- Con AM/PM -->
<ngb-timepicker [(ngModel)]="time" [meridian]="meridian"></ngb-timepicker>
<!-- Clase dinámica: success cuando meridian está ON, danger cuando OFF -->
<button class="btn btn-sm btn-outline-{{meridian ? 'success' : 'danger'}}" (click)="toggleMeridian()">
  Meridian - {{meridian ? "ON" : "OFF"}}
</button>

<!-- Con validación custom (rango 12:00–13:59) -->
<!-- Nota: puede combinarse [formControl] con [(ngModel)] para acceder al valor tipado -->
<ngb-timepicker [formControl]="ctrl" [(ngModel)]="time2" required></ngb-timepicker>
<div *ngIf="ctrl.valid" class="form-text text-success f-w-600">Great choice</div>
<div class="form-text text-danger f-w-600" *ngIf="!ctrl.valid">
  <div *ngIf="ctrl.errors['required']">Select some time during lunchtime</div>
  <div *ngIf="ctrl.errors['tooLate']">Oh no, it's way too late</div>
  <div *ngIf="ctrl.errors['tooEarly']">It's a bit too early</div>
</div>
```

---

### 3. Tagify

**Instalación**: `npm install @yaireo/tagify`

> Requiere `ViewEncapsulation.None` para que los estilos de Tagify apliquen correctamente.

```typescript
import { Component, ViewEncapsulation, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import Tagify from '@yaireo/tagify';

@Component({
  encapsulation: ViewEncapsulation.None,  // requerido para que los estilos de Tagify apliquen
  styleUrls: ['./form-plugins.css']
})
export class FormPluginsPage implements OnInit, OnDestroy {
  ngAfterViewInit() {
    // Tagify se inicializa en ngAfterViewInit para garantizar que el DOM existe
    var inputElement = document.querySelector('[data-render="tags"]');
    new Tagify(inputElement);
  }
}
```

CSS del componente:
```css
@import '~@yaireo/tagify/dist/tagify.css';
```

```html
<input data-render="tags" value='[{"value":"angular"}, {"value":"color-admin"}]' />
```

---

### 4. ngx-editor

**Instalación**: `npm install ngx-editor`

> Siempre llamar `this.editor.destroy()` en `ngOnDestroy` para evitar memory leaks.
> Usar `noBody="true"` en el panel para que el editor ocupe todo el ancho sin padding extra.

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Editor } from 'ngx-editor';

@Component({ ... })
export class MyComponent implements OnInit, OnDestroy {
  editor: Editor;
  html: '';

  ngOnInit() {
    this.editor = new Editor();
  }

  ngOnDestroy() {
    this.editor.destroy(); // siempre destruir para evitar memory leaks
  }
}
```

```html
<panel title="ngx-editor" noBody="true">
  <div outsideBody>
    <div class="NgxEditor__Wrapper border-0">
      <ngx-editor-menu [editor]="editor"> </ngx-editor-menu>
      <!-- [ngModel] es one-way (lectura), no usar [(ngModel)] -->
      <ngx-editor
        [editor]="editor"
        [ngModel]="html"
        [disabled]="false"
        [placeholder]="'Type here...'">
      </ngx-editor>
    </div>
  </div>
</panel>
```

---

### 5. ngx-color

**Instalación**: `npm install ngx-color`

```typescript
import { ColorEvent } from 'ngx-color';

color; // se inicializa en ngOnInit

ngOnInit() {
  this.color = '#0074ff';
}

handleChange($event: ColorEvent) {
  this.color = $event.color.hex;
}
```

```html
<!-- Input con dropdown color picker (estilo Sketch) integrado con ngbDropdown -->
<div class="input-group" ngbDropdown placement="bottom-right">
  <!-- Preview del color seleccionado -->
  <div class="input-group-text px-2">
    <!-- Notar: this.color (referencia explícita al componente) -->
    <div class="w-20px h-20px rounded" [ngStyle]="{'background-color': this.color}"></div>
  </div>
  <!-- Valor hex editable -->
  <input type="text" class="form-control" [(ngModel)]="this.color" />
  <!-- Toggle dropdown -->
  <button class="btn btn-outline-inverse" ngbDropdownToggle>
    <i class="fa fa-tint"></i>
  </button>
  <!-- Picker: color inicial con atributo estático, onChange actualiza el modelo -->
  <div class="dropdown-menu dropdown-toggle w-250px p-0" ngbDropdownMenu>
    <color-sketch color="#00acac" (onChange)="handleChange($event)"></color-sketch>
  </div>
</div>
```
