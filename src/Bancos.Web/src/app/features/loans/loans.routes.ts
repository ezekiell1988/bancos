import { Routes } from '@angular/router';

export const loansRoutes: Routes = [
  { path: '', loadComponent: () => import('./pages/loans-page').then(m => m.LoansPage) }
];
