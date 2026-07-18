import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReviewApi } from '../data-access/review-api';

@Component({
  imports: [DecimalPipe, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section><h1>Movimientos pendientes</h1><p class="muted">Confirma una categoría para cada movimiento.</p>@if (api.pending.isLoading()) { <p>Cargando movimientos…</p> } @else { <ul class="list">@for (item of api.pending.value() ?? []; track item.id) { <li><div><strong>{{ item.description || 'Sin descripción' }}</strong><small>{{ item.bookingDate }} · {{ item.amountCrc | number:'1.2-2' }} {{ item.currencyCode }}</small></div><form (submit)="approve($event, item.id)"><label class="sr-only" [for]="'category-' + item.id">Categoría</label><select [id]="'category-' + item.id" [ngModel]="categoryByTransaction()[item.id]" (ngModelChange)="setCategory(item.id, $event)" name="category" required><option value="" disabled>Categoría</option>@for (category of api.categories.value() ?? []; track category.id) { <option [value]="category.id">{{ category.name }}</option> }</select><button type="submit" [disabled]="!categoryByTransaction()[item.id]">Aprobar</button></form></li> } @empty { <li>Sin movimientos pendientes.</li> }</ul> }</section>`
})
export class ReviewPage {
  readonly api = inject(ReviewApi);
  readonly categoryByTransaction = signal<Record<string, string>>({});
  setCategory(id: string, categoryId: string) { this.categoryByTransaction.update(value => ({ ...value, [id]: categoryId })); }
  approve(event: SubmitEvent, id: string) { event.preventDefault(); const categoryId = this.categoryByTransaction()[id]; if (!categoryId) return; this.api.approve(id, categoryId).subscribe(() => this.api.pending.reload()); }
}
