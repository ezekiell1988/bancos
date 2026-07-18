import { Routes } from '@angular/router';
import { ImportsApi } from './data-access/imports-api';

export const importsRoutes: Routes = [{ path: '', providers: [ImportsApi], loadComponent: () => import('./pages/imports-page').then(m => m.ImportsPage) }];
