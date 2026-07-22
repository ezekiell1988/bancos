# Ejemplo: Página Lista (ts + html + scss)

## list.ts

**Archivo:** `src/app/features/customers/pages/list/list.ts`

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  inject,
  computed,
} from '@angular/core';
import { RouterLink }    from '@angular/router';
import { CustomerApi }   from '../../data-access/customer-api';
import { CustomerStore } from '../../data-access/customer-store';
import { CustomerCard }  from '../../components/customer-card/customer-card';
import { Customer }      from '../../data-access/customer.model';

@Component({
  selector: 'app-customer-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CustomerCard],
  templateUrl: './list.html',
  styleUrl: './list.scss',
})
export class CustomerList {
  private api   = inject(CustomerApi);
  private store = inject(CustomerStore);

  filter           = this.store.filter;
  hasActiveFilters = this.store.hasActiveFilters;
  customers        = this.api.list(this.filter);

  totalPages = computed(() =>
    Math.ceil((this.customers.value()?.total ?? 0) / this.filter().size)
  );

  onQueryChange(event: Event): void {
    this.store.setQuery((event.target as HTMLInputElement).value);
  }

  onStatusChange(status: string): void {
    this.store.setStatus(status as Customer['status'] | 'all');
  }

  onPageChange(page: number): void {
    this.store.setPage(page);
  }

  clearFilters(): void {
    this.store.clearFilters();
  }
}
```

---

## list.html

**Archivo:** `src/app/features/customers/pages/list/list.html`

```html
<div class="page-header">
  <h1 class="page-title">Clientes</h1>
  <a routerLink="new" class="btn btn-primary">Nuevo cliente</a>
</div>

<div class="filters">
  <input
    type="search"
    class="filter-search"
    placeholder="Buscar por nombre o email..."
    [value]="filter().query"
    (input)="onQueryChange($event)"
  />

  <select
    class="filter-status"
    [value]="filter().status"
    (change)="onStatusChange($any($event.target).value)"
  >
    <option value="all">Todos</option>
    <option value="active">Activos</option>
    <option value="inactive">Inactivos</option>
    <option value="blocked">Bloqueados</option>
  </select>

  @if (hasActiveFilters()) {
    <button class="btn btn-ghost" (click)="clearFilters()">Limpiar filtros</button>
  }
</div>

@if (customers.isLoading()) {
  <div class="loading-grid">
    @for (i of [1,2,3,4,5,6]; track i) {
      <div class="skeleton-card"></div>
    }
  </div>
} @else if (customers.error()) {
  <div class="error-state">
    <p>No se pudieron cargar los clientes.</p>
    <button class="btn btn-secondary" (click)="customers.reload()">Reintentar</button>
  </div>
} @else if (!customers.value()?.items?.length) {
  <div class="empty-state">
    <p>No se encontraron clientes con los filtros actuales.</p>
    @if (hasActiveFilters()) {
      <button class="btn btn-ghost" (click)="clearFilters()">Ver todos</button>
    }
  </div>
} @else {
  <div class="customer-grid">
    @for (customer of customers.value()!.items; track customer.id) {
      <app-customer-card [customer]="customer" [routerLink]="[customer.id]" />
    }
  </div>

  <div class="pagination">
    <button
      class="btn btn-ghost"
      [disabled]="filter().page <= 1"
      (click)="onPageChange(filter().page - 1)"
    >
      Anterior
    </button>

    <span class="pagination-info">
      Página {{ filter().page }} de {{ totalPages() }}
      · {{ customers.value()!.total }} clientes
    </span>

    <button
      class="btn btn-ghost"
      [disabled]="filter().page >= totalPages()"
      (click)="onPageChange(filter().page + 1)"
    >
      Siguiente
    </button>
  </div>
}
```

---

## list.scss

**Archivo:** `src/app/features/customers/pages/list/list.scss`

```scss
:host {
  display: block;
  padding: var(--space-6);
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-6);
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0;
}

.filters {
  display: flex;
  gap: var(--space-3);
  align-items: center;
  margin-bottom: var(--space-6);
  flex-wrap: wrap;
}

.filter-search {
  flex: 1;
  min-width: 200px;
  padding: var(--space-2) var(--space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  background: var(--color-surface);
  transition: border-color var(--transition-fast);

  &:focus {
    outline: none;
    border-color: var(--color-border-focus);
  }
}

.filter-status {
  padding: var(--space-2) var(--space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.customer-grid,
.loading-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--space-4);
}

.skeleton-card {
  height: 160px;
  border-radius: var(--radius-md);
  background: linear-gradient(
    90deg,
    var(--color-surface-alt) 25%,
    var(--color-surface) 50%,
    var(--color-surface-alt) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.error-state,
.empty-state {
  text-align: center;
  padding: var(--space-12);
  color: var(--color-text-secondary);
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-4);
  margin-top: var(--space-8);
}

.pagination-info {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

// Botones — mover a shared/components/button cuando se reutilicen
.btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: 500;
  cursor: pointer;
  transition: all var(--transition-fast);
  border: none;
  text-decoration: none;

  &:disabled { opacity: 0.5; cursor: not-allowed; }

  &.btn-primary {
    background: var(--color-primary);
    color: var(--color-primary-contrast);
    &:hover:not(:disabled) { background: var(--color-primary-dark); }
  }

  &.btn-secondary {
    background: var(--color-surface-alt);
    color: var(--color-text-primary);
    border: 1px solid var(--color-border);
    &:hover:not(:disabled) { background: var(--color-border); }
  }

  &.btn-ghost {
    background: transparent;
    color: var(--color-primary);
    &:hover:not(:disabled) { background: var(--color-surface-alt); }
  }
}
```

**Reglas del template:**
- `@if / @else if / @else` — siempre cubrir los 3 estados: loading, error, vacío, y datos.
- `customers.reload()` en el estado de error — permite reintentar sin recargar la página.
- `track customer.id` en `@for` — siempre trackear por id, nunca por índice.
- Nunca `ngIf`, `ngFor`, `ngClass`, `ngStyle` en código nuevo.

**Reglas del SCSS:**
- Todos los colores via `var(--color-*)` — los valores vienen del environment.
- Todos los espacios via `var(--space-*)` — consistencia en todo el proyecto.
- `display: block` en `:host` para que el componente ocupe su espacio correctamente.
