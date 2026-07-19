import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReviewApi } from '../data-access/review-api';

@Component({
  imports: [DecimalPipe, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './review-page.css',
  template: `<section><h1>Movimientos pendientes</h1><p class="muted">Lo que no resolvieron las reglas ni la IA queda en General para que lo clasifiques.</p><form class="category-form" (submit)="createCategory($event)"><label for="new-category">Nueva categoría familiar<input id="new-category" name="newCategory" maxlength="80" autocomplete="off" placeholder="Ej. Supermercado o Salario" [ngModel]="newCategoryName()" (ngModelChange)="newCategoryName.set($event)"></label><button type="submit" [disabled]="creatingCategory() || newCategoryName().trim().length < 2">{{ creatingCategory() ? 'Creando…' : 'Crear categoría' }}</button></form>@if (message()) { <p class="notice" role="status">{{ message() }}</p> }@if (api.pending.isLoading()) { <p>Cargando movimientos…</p> } @else { <ul class="list">@for (item of api.pending.value() ?? []; track item.id) { <li><div><strong>{{ item.description || 'Sin descripción' }}</strong><small>{{ item.bookingDate }} · {{ item.amountCrc | number:'1.2-2' }} {{ item.currencyCode }}</small></div><form (submit)="approve($event, item.id)"><label class="sr-only" [for]="'category-' + item.id">Categoría</label><select [id]="'category-' + item.id" [ngModel]="categoryByTransaction()[item.id]" (ngModelChange)="setCategory(item.id, $event)" name="category" required><option value="" disabled>Selecciona una categoría</option>@for (category of api.categories.value() ?? []; track category.id) { <option [value]="category.id">{{ category.name }}</option> }</select><button type="submit" [disabled]="!categoryByTransaction()[item.id]">Clasificar</button></form></li> } @empty { <li>Sin movimientos pendientes.</li> }</ul> }</section>`
})
export class ReviewPage {
  readonly api = inject(ReviewApi);
  readonly categoryByTransaction = signal<Record<string, string>>({});
  readonly newCategoryName = signal('');
  readonly creatingCategory = signal(false);
  readonly message = signal('');
  setCategory(id: string, categoryId: string) { this.categoryByTransaction.update(value => ({ ...value, [id]: categoryId })); }
  createCategory(event: SubmitEvent) { event.preventDefault(); const name = this.newCategoryName().trim(); if (name.length < 2 || this.creatingCategory()) return; this.creatingCategory.set(true); this.message.set(''); this.api.createCategory(name).subscribe({ next: category => { this.newCategoryName.set(''); this.message.set(`Categoría ${category.name} creada.`); this.api.categories.reload(); this.creatingCategory.set(false); }, error: () => { this.message.set('No se pudo crear la categoría; revisa si ya existe.'); this.creatingCategory.set(false); } }); }
  approve(event: SubmitEvent, id: string) { event.preventDefault(); const categoryId = this.categoryByTransaction()[id]; if (!categoryId) return; this.api.approve(id, categoryId).subscribe(() => { this.categoryByTransaction.update(value => { const next = { ...value }; delete next[id]; return next; }); this.api.pending.reload(); }); }
}
