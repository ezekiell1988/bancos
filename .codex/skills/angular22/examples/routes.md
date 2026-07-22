# Ejemplo: Routes (lazy loading + providers scoped)

**Archivo:** `src/app/features/customers/customers.routes.ts`

```typescript
import { Routes } from '@angular/router';
import { CustomerApi }   from './data-access/customer-api';
import { CustomerStore } from './data-access/customer-store';

export const customersRoutes: Routes = [
  {
    path: '',
    // Providers scoped a esta feature — se instancian al entrar y se destruyen al salir
    providers: [CustomerApi, CustomerStore],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/list/list').then(m => m.CustomerList),
        title: 'Clientes',
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/detail/detail').then(m => m.CustomerDetail),
        title: 'Detalle de Cliente',
      },
    ],
  },
];
```

**Archivo raíz:** `src/app/app.routes.ts`

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./core/layout/shell/shell').then(m => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'customers', pathMatch: 'full' },
      {
        path: 'customers',
        loadChildren: () =>
          import('./features/customers/customers.routes').then(m => m.customersRoutes),
      },
    ],
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./core/auth/pages/login/login').then(m => m.Login),
  },
];
```

**Reglas:**
- `providers` en la ruta raíz de la feature — Angular 22 destruye estos servicios automáticamente al salir.
- `loadComponent` para cada página — lazy loading por defecto, nunca importar componentes directamente en routes.
- `loadChildren` desde `app.routes.ts` para cargar el módulo de rutas de la feature completa.
- `title` en cada ruta — accesibilidad y UX (cambia el `<title>` del documento).
- `paramsInheritanceStrategy` es `'always'` en v22 — los params de rutas padre llegan automáticamente a las hijas.
