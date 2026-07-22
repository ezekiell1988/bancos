# Angular 22 — Arquitectura Feature-Based

## Principio de diseño

Organizar por **dominio de negocio**, no por tipo técnico. Cada feature es un slice vertical
autónomo: componentes, servicios, modelos y rutas en su propia carpeta.

```
Flujo de dependencias (SOLO hacia abajo, nunca hacia arriba):

  features/  →  core/  →  shared/
     ↑              ↑          ↑
  (negocio)    (infra)    (UI pura)
```

---

## Estructura completa

```
src/
├── app/
│   ├── app.config.ts          ← bootstrapApplication config
│   ├── app.routes.ts          ← rutas raíz (lazy a features)
│   ├── app.ts                 ← componente raíz mínimo
│   │
│   ├── core/                  ← infraestructura transversal (no negocio)
│   │   ├── auth/
│   │   │   ├── models/
│   │   │   │   └── auth.model.ts
│   │   │   ├── guards/
│   │   │   │   └── auth-guard.ts
│   │   │   ├── services/
│   │   │   │   └── auth-store.ts       ← @Service(), signals
│   │   │   └── auth.routes.ts
│   │   ├── interceptors/
│   │   │   └── api-interceptor.ts
│   │   ├── layout/
│   │   │   ├── shell/
│   │   │   │   ├── shell.ts
│   │   │   │   └── shell.html
│   │   │   └── sidebar/
│   │   │       ├── sidebar.ts
│   │   │       └── sidebar.html
│   │   └── services/
│   │       └── notification-api.ts
│   │
│   ├── shared/                ← UI pura, sin lógica de negocio
│   │   ├── components/
│   │   │   ├── button/
│   │   │   │   └── button.ts
│   │   │   ├── card/
│   │   │   │   └── card.ts
│   │   │   └── spinner/
│   │   │       └── spinner.ts
│   │   ├── pipes/
│   │   │   └── date-pipe.ts
│   │   └── utils/
│   │       └── array.utils.ts
│   │
│   └── features/              ← slices verticales por dominio
│       ├── invoices/          ← dominio: facturas
│       │   ├── data-access/
│       │   │   ├── invoice-api.ts      ← @Service(), httpResource
│       │   │   └── invoice.model.ts
│       │   ├── pages/
│       │   │   ├── list/
│       │   │   │   ├── list.ts
│       │   │   │   ├── list.html
│       │   │   │   └── list.scss
│       │   │   └── detail/
│       │   │       ├── detail.ts
│       │   │       ├── detail.html
│       │   │       └── detail.scss
│       │   ├── components/    ← componentes privados de la feature
│       │   │   └── invoice-card/
│       │   │       └── invoice-card.ts
│       │   └── invoices.routes.ts
│       │
│       ├── customers/         ← dominio: clientes
│       │   ├── data-access/
│       │   ├── pages/
│       │   └── customers.routes.ts
│       │
│       └── campaigns/         ← dominio: campañas
│           ├── data-access/
│           ├── pages/
│           └── campaigns.routes.ts
│
├── environments/
│   ├── environment.ts         ← dev
│   └── environment.prod.ts    ← prod
│
└── styles/
    ├── _tokens.scss           ← CSS custom properties desde environment
    ├── _reset.scss
    └── styles.scss            ← entry point
```

---

## Convenciones de nombres (Angular 20+)

| Tipo | Formato clase | Nombre de archivo |
|------|--------------|-------------------|
| Component | `InvoiceList` | `invoice-list.ts` (o `list.ts` dentro de su carpeta) |
| Service | `InvoiceApi` | `invoice-api.ts` |
| Guard | `AuthGuard` | `auth-guard.ts` |
| Interceptor | `ApiInterceptor` | `api-interceptor.ts` |
| Pipe | `DatePipe` | `date-pipe.ts` |
| Model/Interface | `Invoice` | `invoice.model.ts` |
| Routes | — | `invoices.routes.ts` |

> **Regla nueva v20+:** Componentes, directivas y servicios **no** llevan sufijo en el nombre
> del archivo (`invoice-list.ts`, NO `invoice-list.component.ts`). Guards, interceptors y pipes
> sí mantienen sufijo separado por guion.

---

## app.config.ts — bootstrap sin NgModule

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withViewTransitions, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration, withIncrementalHydration } from '@angular/platform-browser';
import { routes } from './app.routes';
import { apiInterceptor } from './core/interceptors/api-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Zoneless (estable en v22)
    provideZonelessChangeDetection(),

    // Router con transiciones de vista y binding de inputs desde params
    provideRouter(
      routes,
      withViewTransitions(),
      withComponentInputBinding(),
    ),

    // HTTP: FetchBackend es el default en v22, pero se declara explícitamente
    provideHttpClient(
      withFetch(),
      withInterceptors([apiInterceptor]),
    ),

    // SSR: hidratación incremental activada por defecto en v22
    provideClientHydration(
      withIncrementalHydration(),
    ),
  ],
};
```

---

## app.routes.ts — lazy loading por feature

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./core/layout/shell/shell').then(m => m.Shell),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'invoices',
        pathMatch: 'full',
      },
      {
        path: 'invoices',
        loadChildren: () =>
          import('./features/invoices/invoices.routes').then(m => m.invoicesRoutes),
      },
      {
        path: 'customers',
        loadChildren: () =>
          import('./features/customers/customers.routes').then(m => m.customersRoutes),
      },
      {
        path: 'campaigns',
        loadChildren: () =>
          import('./features/campaigns/campaigns.routes').then(m => m.campaignsRoutes),
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

---

## Feature routes — providers scoped a la ruta

```typescript
// src/app/features/invoices/invoices.routes.ts
import { Routes } from '@angular/router';
import { InvoiceApi } from './data-access/invoice-api';

export const invoicesRoutes: Routes = [
  {
    path: '',
    // Proveedor scoped: InvoiceApi se destruye al salir de la feature
    providers: [InvoiceApi],
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/list/list').then(m => m.InvoiceList),
      },
      {
        path: ':id',
        loadComponent: () => import('./pages/detail/detail').then(m => m.InvoiceDetail),
      },
    ],
  },
];
```

---

## Reglas de dependencias

```
✅ features/ puede importar de core/ y shared/
✅ core/    puede importar de shared/
✅ shared/  no importa de features/ ni de core/
❌ features/ NO importa de otras features/
❌ shared/  NO importa de features/ ni de core/
❌ core/    NO importa de features/
```

Si dos features comparten lógica, moverla a `shared/` (UI pura) o crear un
sub-dominio en `core/services/` si tiene lógica de negocio.

---

## Estructura interna de una feature (detalle)

```
features/invoices/
├── data-access/
│   ├── invoice-api.ts        ← @Service(), httpResource, toda la comunicación HTTP
│   ├── invoice-store.ts      ← @Service(), signals de estado local si aplica
│   └── invoice.model.ts      ← interfaces/types: Invoice, InvoiceFilter, etc.
│
├── pages/
│   └── list/
│       ├── list.ts            ← lógica del componente página
│       ├── list.html          ← template separado para páginas (>=50 líneas)
│       └── list.scss          ← estilos de página (usa tokens CSS del environment)
│
├── components/                ← componentes privados, solo visibles en esta feature
│   └── invoice-card/
│       ├── invoice-card.ts
│       └── invoice-card.html
│
└── invoices.routes.ts
```

> **Regla:** Templates inline para componentes simples (<30 líneas). Template en archivo
> separado `.html` para páginas y componentes complejos.
