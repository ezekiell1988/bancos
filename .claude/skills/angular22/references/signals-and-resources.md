# Angular 22 — Signals, Resource API y Signal Forms

Todas las APIs aquí son **estables en v22** salvo indicación contraria.

---

## 1. Signals core (estables desde v17, maduros en v22)

```typescript
import { signal, computed, effect, linkedSignal, untracked } from '@angular/core';

// Estado escribible
const count = signal(0);
count.set(5);
count.update(c => c + 1);

// Estado derivado (memoizado)
const doubled = computed(() => count() * 2);

// Estado dependiente con reset automático
const options = signal(['A', 'B', 'C']);
const selected = linkedSignal(() => options()[0]);   // reset al cambiar options

// Efecto (side effect declarativo)
effect(() => {
  console.log('count cambió:', count());
});

// Lectura sin tracking de dependencia
const result = computed(() => {
  const a = count();
  const b = untracked(() => otherSignal());  // b no dispara recompute
  return a + b;
});
```

---

## 2. Signal Inputs / Outputs / Model (estables v22)

```typescript
import { Component, ChangeDetectionStrategy, input, output, model } from '@angular/core';

@Component({
  selector: 'app-counter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button (click)="decrement()">-</button>
    <span>{{ value() }}</span>
    <button (click)="increment()">+</button>
  `,
})
export class Counter {
  // Inputs como signals
  value = model(0);              // two-way binding — emite automáticamente valueChange
  min   = input(0);
  max   = input(100);
  label = input.required<string>();

  // Output explícito (cuando no es two-way)
  reset = output<void>();

  increment() {
    if (this.value() < this.max()) this.value.update(v => v + 1);
  }
  decrement() {
    if (this.value() > this.min()) this.value.update(v => v - 1);
  }
}
```

---

## 3. Resource API (estable v22)

### httpResource — el más común

```typescript
import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (users.isLoading()) { <app-spinner /> }
    @else if (users.error()) { <p>{{ users.error() }}</p> }
    @else {
      @for (u of users.value(); track u.id) {
        <div>{{ u.name }}</div>
      }
    }
  `,
})
export class UserList {
  filter = signal('');

  // Re-fetcha automáticamente cuando filter() cambia
  users = httpResource<User[]>(() => ({
    url: '/api/users',
    params: { q: this.filter() },
  }));

  // Acceso a estados
  // users.value()      → T | undefined
  // users.isLoading()  → boolean
  // users.error()      → unknown
  // users.status()     → 'idle' | 'loading' | 'refreshing' | 'resolved' | 'error'
  // users.reload()     → fuerza nuevo fetch
}
```

### resource() — para lógica personalizada

```typescript
import { resource, signal, inject } from '@angular/core';
import { UserService } from './user';

export class UserDetail {
  id = signal<number | null>(null);
  private svc = inject(UserService);

  user = resource({
    request: () => this.id(),
    loader: async ({ request: id, abortSignal }) => {
      if (!id) return null;
      return this.svc.getById(id, abortSignal);
    },
  });
}
```

### rxResource() — cuando el loader es Observable

```typescript
import { rxResource } from '@angular/core/rxjs-interop';
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export class InvoiceData {
  private http = inject(HttpClient);
  clientId = signal(1);

  invoices = rxResource({
    request: () => this.clientId(),
    loader: ({ request: id }) =>
      this.http.get<Invoice[]>(`/api/clients/${id}/invoices`),
  });
}
```

### chain() — composición de recursos

```typescript
// Segundo resource depende del resultado del primero
const client = httpResource<Client>(() => `/api/clients/${clientId()}`);

const invoices = client.chain((c) =>
  httpResource<Invoice[]>(() => `/api/clients/${c.id}/invoices`)
);

// invoices.isLoading() es true mientras client o invoices estén cargando
```

### debounced() — signal con delay (experimental v22)

```typescript
import { debounced } from '@angular/core';

const searchQuery = signal('');

// Crea un resource cuyo valor se actualiza con 300ms de delay
const debouncedQuery = debounced(() => searchQuery(), 300);

const results = httpResource<Result[]>(() => ({
  url: '/api/search',
  params: { q: debouncedQuery.value() ?? '' },
}));
```

---

## 4. Signal Forms (estable v22)

### Form básico con validación

```typescript
import { Component, ChangeDetectionStrategy } from '@angular/core';
import { signalForm, validators } from '@angular/forms';
import { SignalFormModule } from '@angular/forms';

@Component({
  selector: 'app-login-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SignalFormModule],
  template: `
    <form (submit)="submit()">
      <input [formField]="form.fields.email" type="email" />
      @if (form.fields.email.hasError('required')) {
        <span>Email requerido</span>
      }

      <input [formField]="form.fields.password" type="password" />
      @if (form.fields.password.hasError('minLength')) {
        <span>Mínimo 8 caracteres</span>
      }

      <button type="submit" [disabled]="!form.valid()">Ingresar</button>
    </form>
  `,
})
export class LoginForm {
  form = signalForm({
    email: ['', [validators.required, validators.email]],
    password: ['', [validators.required, validators.minLength(8)]],
  });

  submit() {
    if (this.form.valid()) {
      console.log(this.form.value());
    }
  }
}
```

### Validación async + submission API

```typescript
import { signalForm, validators } from '@angular/forms';
import { inject } from '@angular/core';
import { AuthApi } from '../data-access/auth-api';

export class RegisterForm {
  private auth = inject(AuthApi);

  form = signalForm({
    email: ['', [validators.required, validators.email], {
      // Validador async
      asyncValidators: [this.auth.checkEmailAvailable.bind(this.auth)],
    }],
    name: ['', validators.required],
  });

  // Submission con estado async (nuevo en v22)
  submission = this.form.createSubmission(async () => {
    await this.auth.register(this.form.value());
  });

  // submission.isSubmitting() → boolean
  // submission.error()        → Error | null
  // submission.hasSubmitted() → boolean
}
```

### Validación dinámica con `when`

```typescript
form = signalForm({
  paymentMethod: ['card'],
  cardNumber: ['', {
    validators: [validators.required],
    when: () => this.form?.fields.paymentMethod.value() === 'card',
  }],
  bankAccount: ['', {
    validators: [validators.required],
    when: () => this.form?.fields.paymentMethod.value() === 'bank',
  }],
});
```

### Debounce en blur

```typescript
import { debounce } from '@angular/forms';

form = signalForm({
  // Valida 300ms después de que el usuario deja de escribir
  // y también valida inmediatamente on blur
  username: ['', validators.required],
});

// En el template
// [formField]="debounce(form.fields.username, 'blur')"
```

### getError() y reloadValidation()

```typescript
// Acceder a un error específico
const emailError = this.form.fields.email.getError('email');
// → string | null

// Re-ejecutar validadores async manualmente
this.form.fields.email.reloadValidation();
```

---

## 5. @Service decorator (estable v22)

```typescript
import { Service, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';

// Reemplaza @Injectable({ providedIn: 'root' })
@Service()
export class InvoiceApi {
  list = (clientId: number) =>
    httpResource<Invoice[]>(() => `/api/clients/${clientId}/invoices`);

  getById = (id: number) =>
    httpResource<Invoice>(() => `/api/invoices/${id}`);
}
```

---

## 6. injectAsync() — lazy loading de dependencias

```typescript
import { injectAsync, Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button (click)="generateReport()">Generar reporte</button>
  `,
})
export class Dashboard {
  // Bundle separado, se descarga cuando se llama por primera vez
  private reportSvc = injectAsync(() =>
    import('./data-access/report').then(m => m.ReportService)
  );

  // Prefetch cuando el navegador está idle (mejora UX)
  private heavySvc = injectAsync(
    () => import('./heavy/heavy').then(m => m.HeavyService),
    { onIdle: true }
  );

  async generateReport() {
    const svc = await this.reportSvc();
    svc.generate();
  }
}
```

---

## 7. isActive como Signal (router, v22)

```typescript
import { inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  template: `
    <a routerLink="/invoices" [class.active]="isInvoicesActive()">Facturas</a>
  `,
})
export class Sidebar {
  private router = inject(Router);

  isInvoicesActive = this.router.isActive('/invoices', {
    paths: 'subset',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  });
  // isActive() es ahora una Signal — se re-evalúa automáticamente
}
```

---

## 8. @defer — lazy UI (mejorado v22)

```typescript
// Timeout en idle
@defer (on idle(500ms)) {
  <app-heavy-chart [data]="data()" />
} @loading {
  <app-skeleton />
} @error {
  <p>Error cargando gráfico</p>
}

// Defer on interaction
@defer (on interaction(triggerBtn)) {
  <app-comments />
} @placeholder {
  <button #triggerBtn>Ver comentarios</button>
}
```

---

## 9. Template syntax nuevo en v22

```typescript
// Spread en class binding
<div [class]="{ ...baseClasses, active: isActive(), disabled: disabled() }">

// Arrow functions con retorno implícito en templates
@for (item of items(); track item.id) {
  {{ item | transform: (x) => x.name.toUpperCase() }}
}

// instanceof directo
@if (error() instanceof ValidationError) {
  <p>Error de validación</p>
}

// @switch exhaustivo
@switch (status()) {
  @case ('active')   { <span class="green">Activo</span> }
  @case ('inactive') { <span class="red">Inactivo</span> }
  @default never;   // error de compilación si se agrega un status sin case
}

// Optional chaining alineado con JS
{{ user()?.address?.city ?? 'Sin ciudad' }}
```

---

## 10. Señales de query (viewChild, contentChild)

```typescript
import { viewChild, viewChildren, contentChild } from '@angular/core';

@Component({...})
export class MyComponent {
  // viewChild como signal
  canvas = viewChild<ElementRef>('myCanvas');   // Signal<ElementRef | undefined>
  canvasRequired = viewChild.required<ElementRef>('myCanvas'); // Signal<ElementRef>

  // Lista de hijos
  items = viewChildren<ItemComponent>(ItemComponent); // Signal<readonly ItemComponent[]>

  // Content projection
  header = contentChild<HeaderComponent>(HeaderComponent);
}
```
