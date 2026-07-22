---
name: angular22
description: >
  Guía completa para construir aplicaciones Angular 22 (Signal-First). Usar cuando se cree
  una nueva app Angular, se diseñe la arquitectura feature-based, se implementen Signal Forms,
  Resource API, @Service decorator, injectAsync, httpResource, colores desde environment, o
  cualquier patrón moderno de Angular 22. Cubre: OnPush por defecto, zoneless, FetchBackend,
  TypeScript 6, Node 22, estructura de carpetas core/modules/shared, CSS design tokens desde
  environment, strictTemplates habilitado por defecto, y WebMCP experimental.
  Triggers: Angular 22, angular22, signal forms, httpResource, resource api, feature structure,
  @Service, injectAsync, environment colors, zoneless angular, OnPush default, angular new project.
---

# Angular 22 — Guía Completa (Signal-First Era)

Angular 22 marca la llegada de la era "Signal-First": signals, Signal Forms, Resource API y
OnPush son la realidad cotidiana de desarrollo, no un roadmap.

**Requisitos mínimos:** TypeScript 6 · Node 22 · Angular CLI 22

---

## Qué leer según el contexto

| Necesidad | Archivo |
|-----------|---------|
| Estructura de proyecto / carpetas | [references/architecture.md](./references/architecture.md) |
| Signals, Resource API, Signal Forms | [references/signals-and-resources.md](./references/signals-and-resources.md) |
| Colores y theming desde environment | [references/environment-colors.md](./references/environment-colors.md) |
| Breaking changes y migración | [references/breaking-changes.md](./references/breaking-changes.md) |
| Errores frecuentes del Language Service | [references/language-service-pitfalls.md](./references/language-service-pitfalls.md) |
| Ejemplo: interfaces y tipos | [examples/model.md](./examples/model.md) |
| Ejemplo: API service + store (data-access) | [examples/data-access.md](./examples/data-access.md) |
| Ejemplo: routes (lazy + providers scoped) | [examples/routes.md](./examples/routes.md) |
| Ejemplo: página lista (ts + html + scss) | [examples/page-list.md](./examples/page-list.md) |
| Ejemplo: componente presentacional (card) | [examples/component-card.md](./examples/component-card.md) |

---

## Resumen de features estabilizadas en v22

| Feature | Estado anterior | Estado v22 |
|---------|----------------|-----------|
| `signal()`, `computed()`, `effect()` | Estable desde v17 | Estable ✅ |
| `input()`, `output()`, `model()` | Estable desde v17 | Estable ✅ |
| `linkedSignal()` | Developer Preview | **Estable ✅** |
| `resource()` / `httpResource()` / `rxResource()` | Developer Preview | **Estable ✅** |
| Signal Forms | Experimental | **Estable ✅** |
| Angular Aria | Nuevo | **Estable ✅** |
| Hidratación incremental | Developer Preview | **Estable ✅** |
| Zoneless change detection | Experimental | **Estable ✅** |
| `@Service` decorator | Nuevo | **Estable ✅** |
| `injectAsync()` | Nuevo | **Estable ✅** |
| `debounced()` signal | Nuevo | Experimental |
| WebMCP tools | Nuevo | Experimental |

---

## Cambios de comportamiento por defecto

```
v21  →  v22
─────────────────────────────────────────────────
ChangeDetection.Default   →  ChangeDetection.OnPush  (nuevo default)
XHR backend               →  FetchBackend             (nuevo default)
Hydration incremental OFF →  Hydration incremental ON (nuevo default)
paramsInheritanceStrategy: 'emptyOnly'  →  'always'
strictTemplates: false    →  true                     (nuevo default)
```

> `ng update` aplica migración automática: marca componentes existentes como
> `ChangeDetectionStrategy.Eager` para preservar comportamiento anterior.

---

## Comandos clave

```bash
# Crear nueva app (TypeScript 6, Node 22)
ng new mi-app --style=scss --ssr=false

# Crear feature (ruta lazy)
ng g c features/invoices/pages/list --flat

# Generar servicio con @Service (sin NgModule)
ng g s features/invoices/data-access/invoice

# Build optimizado (chunk optimization ON por defecto en v22)
ng build --optimization

# Test con Vitest (migración desde Karma)
ng generate @angular/core:migrate-karma-to-vitest
```

---

## Patrón mínimo de componente v22

```typescript
// features/invoices/pages/list/list.ts
import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { InvoiceApi } from '../../data-access/invoice-api';

@Component({
  selector: 'app-invoice-list',
  changeDetection: ChangeDetectionStrategy.OnPush,  // default en v22, explícito por claridad
  template: `
    @if (invoices.isLoading()) {
      <p>Cargando...</p>
    } @else if (invoices.error()) {
      <p>Error: {{ invoices.error() }}</p>
    } @else {
      @for (inv of invoices.value(); track inv.id) {
        <div>{{ inv.name }}</div>
      } @empty {
        <p>Sin facturas</p>
      }
    }
  `,
})
export class InvoiceList {
  private api = inject(InvoiceApi);
  invoices = this.api.list();   // httpResource
}
```

---

## @Service decorator — reemplaza @Injectable

```typescript
import { Service } from '@angular/core';

// Antes (v21)
@Injectable({ providedIn: 'root' })
export class InvoiceApi { ... }

// Ahora (v22)
@Service()
export class InvoiceApi { ... }
```

---

## Signal Forms — mínimo viable

```typescript
import { signalForm, validators } from '@angular/forms';

export class CreateInvoice {
  form = signalForm({
    name: ['', validators.required],
    amount: [0, validators.min(1)],
  });

  submit() {
    if (this.form.valid()) {
      console.log(this.form.value());
    }
  }
}
```

---

## injectAsync — lazy loading de servicios

```typescript
import { injectAsync } from '@angular/core';

export class Dashboard {
  // Se carga solo cuando se accede a reportService()
  reportService = injectAsync(() => import('./reports/report'));

  // Con prefetch en idle
  heavyService = injectAsync(() => import('./heavy/service'), { onIdle: true });
}
```

---

## Resource con chain()

```typescript
// Chain permite componer recursos sin efectos secundarios
const client = httpResource<Client>(() => `/api/clients/${clientId()}`);

const invoices = client.chain((c) =>
  httpResource<Invoice[]>(() => `/api/clients/${c.id}/invoices`)
);
```

---

## Reglas de oro Angular 22

1. **Signals everywhere** — state, forms, async data. No BehaviorSubject, no EventEmitter como estado.
2. **OnPush siempre** — aunque sea el default, declararlo explícitamente en cada componente.
3. **Standalone siempre** — nunca `NgModule` para nuevas features.
4. **httpResource sobre HttpClient directo** — manejo automático de condiciones de carrera.
5. **@Service sobre @Injectable** — menos boilerplate.
6. **Feature-based structure** — `core/` → `shared/` → `features/{dominio}/`. Ver arquitectura.
7. **Colores desde environment** — CSS custom properties inyectadas desde `environment.ts`. Ver theming.
8. **@defer para heavy components** — lazy con `on idle(500ms)`.
9. **No `ngIf` / `ngFor` / `ngSwitch`** — solo control flow nativo `@if`, `@for`, `@switch`.
10. **No `ngClass` / `ngStyle`** — bindings directos `[class.x]` y `[style.x]`.
