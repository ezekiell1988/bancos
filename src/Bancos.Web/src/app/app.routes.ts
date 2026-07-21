import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'imports' },
  { path: 'imports', title: 'Importaciones · Bancos', loadChildren: () => import('./features/imports/imports.routes').then(m => m.importsRoutes) },
  { path: 'review', title: 'Revisión · Bancos', loadChildren: () => import('./features/review/review.routes').then(m => m.reviewRoutes) },
  { path: 'categories', title: 'Categorías · Bancos', loadChildren: () => import('./features/categories/categories.routes').then(m => m.categoriesRoutes) },
  { path: 'loans', title: 'Préstamos · Bancos', loadChildren: () => import('./features/loans/loans.routes').then(m => m.loansRoutes) },
  { path: 'reports', title: 'Reportes · Bancos', loadChildren: () => import('./features/reports/reports.routes').then(m => m.reportsRoutes) },
  { path: '**', redirectTo: 'imports' }
];
