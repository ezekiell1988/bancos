import { computed, inject, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';

export interface CategoryTotal { category: string; total: number; }
export interface IncomeStatementResponse {
  year: number; month: number;
  income: CategoryTotal[]; expenses: CategoryTotal[];
  totalIncome: number; totalExpenses: number; netResult: number;
}

export class ReportsApi {
  private readonly year = signal(new Date().getFullYear());
  private readonly month = signal(new Date().getMonth() + 1);

  readonly selectedYear = this.year.asReadonly();
  readonly selectedMonth = this.month.asReadonly();

  readonly incomeStatement = httpResource<IncomeStatementResponse>(
    () => `/api/reports/income-statement?year=${this.year()}&month=${this.month()}`
  );

  setYearMonth(year: number, month: number) {
    this.year.set(year);
    this.month.set(month);
  }
}
