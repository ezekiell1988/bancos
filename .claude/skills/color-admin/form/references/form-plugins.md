# Reference: Form Plugins

Fuente: `color-admin/template_angularjs20/src/app/pages/form/form-plugins/`

Plugins de formulario integrados en el template Color Admin para Angular.

---

## 1. ng-bootstrap Datepicker

Requiere: `@ng-bootstrap/ng-bootstrap`

### Datepicker Inline

```html
<ngb-datepicker #d1 [(ngModel)]="model1" #c1="ngModel"></ngb-datepicker>
<hr />
<button class="btn btn-sm btn-outline-primary" (click)="model1 = today">Select Today</button>
<hr />
<pre>Model: {{ model1 | json }}</pre>
<pre>State: {{ c1.status }}</pre>
```

### Datepicker en Input (popup)

```html
<form class="form-inline">
  <div class="form-group">
    <div class="input-group">
      <input class="form-control" placeholder="yyyy-mm-dd"
             name="d2" #c2="ngModel" [(ngModel)]="model2"
             ngbDatepicker #d2="ngbDatepicker" />
      <button class="btn btn-outline-secondary" (click)="d2.toggle()" type="button">
        <i class="fa fa-calendar"></i>
      </button>
    </div>
  </div>
</form>
```

### TypeScript — Adapter de fecha nativa JS

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

// En el @Component:
// providers: [{ provide: NgbDateAdapter, useClass: NgbDateNativeAdapter }]
```

---

## 2. ng-bootstrap Timepicker

Requiere: `@ng-bootstrap/ng-bootstrap`

### Timepicker con Meridian (AM/PM)

```html
<ngb-timepicker [(ngModel)]="time" [meridian]="meridian"></ngb-timepicker>
<button class="btn btn-sm btn-outline-{{meridian ? 'success' : 'danger'}}" (click)="toggleMeridian()">
  Meridian - {{meridian ? "ON" : "OFF"}}
</button>
<hr />
<pre>Selected time: {{time | json}}</pre>
```

### Timepicker con Validación Personalizada

```html
<div class="form-group">
  <ngb-timepicker [formControl]="ctrl" [(ngModel)]="time2" required></ngb-timepicker>
  <div *ngIf="ctrl.valid" class="form-text text-success f-w-600">Great choice</div>
  <div class="form-text text-danger f-w-600" *ngIf="!ctrl.valid">
    <div *ngIf="ctrl.errors['required']">Select some time during lunchtime</div>
    <div *ngIf="ctrl.errors['tooLate']">Oh no, it's way too late</div>
    <div *ngIf="ctrl.errors['tooEarly']">It's a bit too early</div>
  </div>
</div>
<pre>Selected time: {{time2 | json}}</pre>
```

### TypeScript — Validador personalizado + estado lógico

```typescript
import { FormControl } from '@angular/forms';

time  = { hour: 13, minute: 30 };
time2;
meridian = true;

toggleMeridian() { this.meridian = !this.meridian; }

ctrl = new FormControl('', (control: FormControl) => {
  const value = control.value;
  if (!value)          return null;
  if (value.hour < 12) return { tooEarly: true };
  if (value.hour > 13) return { tooLate: true };
  return null;
});
```

---

## 3. Tagify — Tags Input

Requiere: `npm install @yaireo/tagify`

CSS en el componente (no olvidar):
```css
@import '~@yaireo/tagify/dist/tagify.css';
```

### HTML

```html
<!-- El atributo data-render="tags" sirve como selector en ngAfterViewInit -->
<input data-render="tags" value='[{"value":"tag1"}, {"value":"tag2"}]' />
```

### TypeScript

```typescript
import Tagify from '@yaireo/tagify';

// En el @Component: encapsulation: ViewEncapsulation.None
// para que el CSS de tagify no quede encapsulado

ngAfterViewInit() {
  const inputElement = document.querySelector('[data-render="tags"]');
  new Tagify(inputElement);
}
```

---

## 4. ngx-editor — Editor de Texto Rico

Requiere: `npm install ngx-editor`

### HTML

```html
<div class="NgxEditor__Wrapper border-0">
  <ngx-editor-menu [editor]="editor"></ngx-editor-menu>
  <ngx-editor
    [editor]="editor"
    [ngModel]="html"
    [disabled]="false"
    [placeholder]="'Type here...'"
  ></ngx-editor>
</div>
```

### TypeScript

```typescript
import { Editor } from 'ngx-editor';

editor: Editor;
html = '';

ngOnInit()    { this.editor = new Editor(); }
ngOnDestroy() { this.editor.destroy(); }
```

> **Nota**: Siempre usar `panel` con `noBody="true"` cuando el editor ocupa todo el cuerpo del panel.

```html
<panel title="Editor" noBody="true">
  <ng-container outsideBody>
    <div class="NgxEditor__Wrapper border-0">
      <ngx-editor-menu [editor]="editor"></ngx-editor-menu>
      <ngx-editor [editor]="editor" [(ngModel)]="html"></ngx-editor>
    </div>
  </ng-container>
</panel>
```

---

## 5. ngx-color — Color Picker

Requiere: `npm install ngx-color`

### Color Picker con Input integrado (patrón ngbDropdown)

```html
<div class="input-group" ngbDropdown placement="bottom-right">
  <!-- Muestra el color actual como swatch -->
  <div class="input-group-text px-2">
    <div class="w-20px h-20px rounded" [ngStyle]="{'background-color': color}"></div>
  </div>
  <!-- Input editable con el valor hex -->
  <input type="text" class="form-control" [(ngModel)]="color" />
  <!-- Botón que abre el dropdown -->
  <button class="btn btn-outline-inverse" ngbDropdownToggle>
    <i class="fa fa-tint"></i>
  </button>
  <!-- Dropdown con el picker -->
  <div class="dropdown-menu dropdown-toggle w-250px p-0" ngbDropdownMenu>
    <color-sketch color="#00acac" (onChange)="handleChange($event)"></color-sketch>
  </div>
</div>
```

### TypeScript

```typescript
import { ColorEvent } from 'ngx-color';

color = '#0074ff';

handleChange($event: ColorEvent) {
  this.color = $event.color.hex;
}
```

---

## Configuración del Componente

Para usar Tagify y ngx-editor con estilos correctos, el componente necesita:

```typescript
import {
  Component, ViewEncapsulation, OnInit, OnDestroy, AfterViewInit
} from '@angular/core';

@Component({
  selector: 'app-mi-form',
  templateUrl: './mi-form.component.html',
  encapsulation: ViewEncapsulation.None,  // requerido para Tagify / ngx-editor
  styleUrls: ['./mi-form.component.css']  // @import tagify.css aquí
})
export class MiFormComponent implements OnInit, OnDestroy, AfterViewInit {
  // ... lógica
}
```
