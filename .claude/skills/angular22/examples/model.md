# Ejemplo: Model (interfaces y tipos)

**Archivo:** `src/app/features/customers/data-access/customer.model.ts`

```typescript
export interface Customer {
  id: number;
  name: string;
  email: string;
  phone: string;
  totalPurchases: number;
  lastPurchaseDate: string;
  status: 'active' | 'inactive' | 'blocked';
}

export interface CustomerFilter {
  query:  string;
  status: Customer['status'] | 'all';
  page:   number;
  size:   number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page:  number;
  size:  number;
}
```

**Reglas:**
- Solo interfaces y types — sin lógica ni imports de Angular.
- `PagedResult<T>` es genérico y puede ir en `shared/models/` si se reutiliza entre features.
- Los union types de status se declaran en el modelo, no se repiten en componentes.
