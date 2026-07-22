# Ejemplo: Data Access (API + Store)

## API Service — `customer-api.ts`

**Archivo:** `src/app/features/customers/data-access/customer-api.ts`

```typescript
import { Service, inject, Signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { Customer, CustomerFilter, PagedResult } from './customer.model';
import { environment } from '../../../../environments/environment';

@Service()
export class CustomerApi {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  // Re-fetcha automáticamente cuando el signal de filtro cambia
  list(filter: Signal<CustomerFilter>) {
    return httpResource<PagedResult<Customer>>(() => ({
      url: `${this.base}/api/v1/customers`,
      params: {
        q:      filter().query,
        status: filter().status,
        page:   filter().page,
        size:   filter().size,
      },
    }));
  }

  // Re-fetcha cuando el id signal cambia
  getById(id: Signal<number>) {
    return httpResource<Customer>(
      () => `${this.base}/api/v1/customers/${id()}`
    );
  }
}
```

**Reglas:**
- `@Service()` reemplaza `@Injectable({ providedIn: 'root' })` en Angular 22.
- Los métodos reciben `Signal<T>` como parámetro para que `httpResource` pueda rastrear cambios.
- Toda la URL base viene de `environment.apiUrl` — nunca hardcodeada.
- No hay `subscribe()` ni `async/await` directo — `httpResource` maneja el ciclo de vida.

---

## Store de UI — `customer-store.ts`

**Archivo:** `src/app/features/customers/data-access/customer-store.ts`

```typescript
import { Service, signal, computed } from '@angular/core';
import { CustomerFilter } from './customer.model';

@Service()
export class CustomerStore {
  private _filter = signal<CustomerFilter>({
    query:  '',
    status: 'all',
    page:   1,
    size:   20,
  });

  readonly filter = this._filter.asReadonly();

  readonly hasActiveFilters = computed(() =>
    this._filter().query !== '' || this._filter().status !== 'all'
  );

  setQuery(query: string): void {
    this._filter.update(f => ({ ...f, query, page: 1 }));
  }

  setStatus(status: CustomerFilter['status']): void {
    this._filter.update(f => ({ ...f, status, page: 1 }));
  }

  setPage(page: number): void {
    this._filter.update(f => ({ ...f, page }));
  }

  clearFilters(): void {
    this._filter.set({ query: '', status: 'all', page: 1, size: 20 });
  }
}
```

**Reglas:**
- Estado interno `private _filter` — nunca exponer el signal escribible.
- `asReadonly()` para exponer lectura pública sin permitir mutación externa.
- `computed()` para estados derivados — nunca calcularlos en el componente.
- Resetear `page: 1` al cambiar query o status (UX: volver a primera página).
- El store solo gestiona estado de UI (filtros, selección). El estado del servidor vive en `httpResource`.
