import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Category { id: string; name: string; }
export interface PendingTransaction { id: string; bookingDate: string; description: string; amountCrc: number; currencyCode: string; }

@Injectable()
export class ReviewApi {
  private readonly http = inject(HttpClient);
  readonly categories = httpResource<Category[]>(() => '/api/classification/categories');
  readonly pending = httpResource<PendingTransaction[]>(() => '/api/classification/transactions/pending');
  approve(transactionId: string, categoryId: string) { return this.http.put(`/api/classification/transactions/${transactionId}/review`, { categoryId }); }
}
