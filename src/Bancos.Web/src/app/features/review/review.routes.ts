import { Routes } from '@angular/router';
import { ReviewApi } from './data-access/review-api';

export const reviewRoutes: Routes = [{ path: '', providers: [ReviewApi], loadComponent: () => import('./pages/review-page').then(m => m.ReviewPage) }];
