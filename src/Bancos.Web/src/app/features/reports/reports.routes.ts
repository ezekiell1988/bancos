import { Routes } from '@angular/router';

export const reportsRoutes: Routes = [
  { path: 'income-statement', loadComponent: () => import('./income-statement/pages/income-statement-page').then(m => m.IncomeStatementPage) },
  { path: 'balance-sheet', loadComponent: () => import('./balance-sheet/pages/balance-sheet-page').then(m => m.BalanceSheetPage) }
];
