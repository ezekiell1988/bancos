import { Routes } from '@angular/router';

export const categoriesRoutes: Routes = [{ path: '', loadComponent: () => import('./pages/categories-page').then(m => m.CategoriesPage) }];
