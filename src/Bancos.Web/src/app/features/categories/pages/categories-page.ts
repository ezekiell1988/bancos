import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoriesApi, TransactionFilter } from '../data-access/categories-api';

@Component({
  imports: [DecimalPipe, FormsModule],
  providers: [CategoriesApi],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './categories-page.css',
  template: `
<section>
  <h1>Movimientos por categoría</h1>
  <p class="muted">Filtra por categoría o descripción y reclasifica transacciones inline.</p>

  <div class="filters">
    <label>
      Categoría
      <select [ngModel]="filter().categoryId ?? ''" (ngModelChange)="setCategoryFilter($event)" name="category">
        <option value="">Todas</option>
        @for (cat of api.categories.value() ?? []; track cat.id) {
          <option [value]="cat.id">{{ cat.name }}</option>
        }
      </select>
    </label>
    <label>
      Descripción
      <input type="search" name="description" autocomplete="off" placeholder="Buscar…"
        [ngModel]="filter().description ?? ''" (ngModelChange)="setDescriptionFilter($event)">
    </label>
  </div>

  @if (txResource.isLoading()) {
    <p>Cargando movimientos…</p>
  } @else {
    @let result = txResource.value();
    <div class="summary">
      <span>{{ result?.count ?? 0 }} movimientos</span>
      <strong>Total: {{ result?.totalAmount | number:'1.2-2' }} CRC</strong>
    </div>

    <ul class="list">
      @for (item of result?.items ?? []; track item.id) {
        <li>
          <div class="info">
            <strong>{{ item.description || 'Sin descripción' }}</strong>
            <small>{{ item.bookingDate }} · {{ item.amountCrc | number:'1.2-2' }} CRC</small>
          </div>
          <div class="action">
            <label class="sr-only" [for]="'cat-' + item.id">Categoría</label>
            <select [id]="'cat-' + item.id" [ngModel]="pendingCategory()[item.id] ?? item.categoryId ?? ''"
              (ngModelChange)="reclassify(item.id, $event)" name="category">
              <option value="" disabled>Sin categoría</option>
              @for (cat of api.categories.value() ?? []; track cat.id) {
                <option [value]="cat.id">{{ cat.name }}</option>
              }
            </select>
          </div>
        </li>
      } @empty {
        <li class="empty">Sin movimientos para el filtro activo.</li>
      }
    </ul>

    @if ((result?.count ?? 0) > 50) {
      <div class="pagination">
        <button [disabled]="filter().page <= 1" (click)="prevPage()">← Anterior</button>
        <span>Página {{ filter().page }}</span>
        <button [disabled]="(result?.items?.length ?? 0) < 50" (click)="nextPage()">Siguiente →</button>
      </div>
    }
  }
</section>`
})
export class CategoriesPage {
  readonly api = inject(CategoriesApi);
  readonly filter = signal<TransactionFilter>({ page: 1 });
  readonly txResource = this.api.transactions(this.filter);
  readonly pendingCategory = signal<Record<string, string | undefined>>({});

  setCategoryFilter(categoryId: string) {
    this.filter.update(f => ({ ...f, categoryId: categoryId || undefined, page: 1 }));
  }

  setDescriptionFilter(description: string) {
    this.filter.update(f => ({ ...f, description, page: 1 }));
  }

  reclassify(id: string, categoryId: string) {
    if (!categoryId) return;
    this.pendingCategory.update(m => ({ ...m, [id]: categoryId }));
    this.api.patchCategory(id, categoryId).subscribe(() => this.txResource.reload());
  }

  prevPage() { this.filter.update(f => ({ ...f, page: Math.max(1, f.page - 1) })); }
  nextPage() { this.filter.update(f => ({ ...f, page: f.page + 1 })); }
}
