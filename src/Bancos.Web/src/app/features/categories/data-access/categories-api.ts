import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';

export interface Category { id: string; name: string; }
export interface TransactionRow { id: string; bookingDate: string; description: string; amountCrc: number; categoryId: string | null; categoryName: string | null; }
export interface PagedTransactionResult { items: TransactionRow[]; count: number; totalAmount: number; }

export interface TransactionFilter { categoryId?: string; description?: string; page: number; }

@Injectable()
export class CategoriesApi {
  private readonly http = inject(HttpClient);

  readonly categories = httpResource<Category[]>(() => '/api/classification/categories');

  transactions(filter: Signal<TransactionFilter>) {
    return httpResource<PagedTransactionResult>(() => {
      const f = filter();
      const params = new URLSearchParams({ page: String(f.page), pageSize: '50' });
      if (f.categoryId) params.set('categoryId', f.categoryId);
      if (f.description?.trim()) params.set('description', f.description.trim());
      return `/api/transactions?${params}`;
    });
  }

  patchCategory(transactionId: string, categoryId: string) {
    return this.http.patch(`/api/transactions/${transactionId}/category`, { categoryId });
  }
}
