import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'imports' },
  { path: 'imports', title: 'Importaciones · Bancos', loadChildren: () => import('./features/imports/imports.routes').then(m => m.importsRoutes) },
  { path: 'review', title: 'Revisión · Bancos', loadChildren: () => import('./features/review/review.routes').then(m => m.reviewRoutes) },
  { path: '**', redirectTo: 'imports' }
];
