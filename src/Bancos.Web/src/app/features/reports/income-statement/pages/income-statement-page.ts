import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsApi } from '../reports-api';

const MONTHS = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];

@Component({
  imports: [DecimalPipe, FormsModule],
  providers: [ReportsApi],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './income-statement-page.css',
  template: `
<section class="income-statement">
  <h1>Estado de Resultados</h1>

  <form class="period-selector" (submit)="$event.preventDefault()">
    <label>
      Mes
      <select [ngModel]="api.selectedMonth()" (ngModelChange)="setMonth($event)" name="month">
        @for (m of months; track $index) {
          <option [value]="$index + 1">{{ m }}</option>
        }
      </select>
    </label>
    <label>
      Año
      <select [ngModel]="api.selectedYear()" (ngModelChange)="setYear($event)" name="year">
        @for (y of years; track y) {
          <option [value]="y">{{ y }}</option>
        }
      </select>
    </label>
  </form>

  @if (api.incomeStatement.isLoading()) {
    <p class="muted">Cargando…</p>
  } @else if (api.incomeStatement.error()) {
    <p class="notice error">No se pudo cargar el estado de resultados.</p>
  } @else if (api.incomeStatement.hasValue()) {
    @let data = api.incomeStatement.value()!;

    <div class="tables-row">
      <div class="table-block">
        <h2>Ingresos</h2>
        <table>
          <thead><tr><th>Categoría</th><th class="num">Total CRC</th></tr></thead>
          <tbody>
            @for (row of data.income; track row.category) {
              <tr><td>{{ row.category }}</td><td class="num">{{ row.total | number:'1.2-2' }}</td></tr>
            } @empty {
              <tr><td colspan="2" class="muted">Sin ingresos en el período.</td></tr>
            }
          </tbody>
          <tfoot><tr class="subtotal"><td>Total ingresos</td><td class="num">{{ data.totalIncome | number:'1.2-2' }}</td></tr></tfoot>
        </table>
      </div>

      <div class="table-block">
        <h2>Gastos</h2>
        <table>
          <thead><tr><th>Categoría</th><th class="num">Total CRC</th></tr></thead>
          <tbody>
            @for (row of data.expenses; track row.category) {
              <tr><td>{{ row.category }}</td><td class="num">{{ row.total | number:'1.2-2' }}</td></tr>
            } @empty {
              <tr><td colspan="2" class="muted">Sin gastos en el período.</td></tr>
            }
          </tbody>
          <tfoot><tr class="subtotal"><td>Total gastos</td><td class="num">{{ data.totalExpenses | number:'1.2-2' }}</td></tr></tfoot>
        </table>
      </div>
    </div>

    <div class="net-result" [class.surplus]="data.netResult >= 0" [class.deficit]="data.netResult < 0">
      <span class="net-label">{{ data.netResult >= 0 ? 'Superávit' : 'Déficit' }}</span>
      <span class="net-amount">{{ data.netResult | number:'1.2-2' }} CRC</span>
    </div>
  }
</section>
`
})
export class IncomeStatementPage {
  readonly api = inject(ReportsApi);
  readonly months = MONTHS;
  readonly years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i);

  private pendingYear = this.api.selectedYear();
  private pendingMonth = this.api.selectedMonth();

  setYear(year: number) { this.pendingYear = +year; this.api.setYearMonth(this.pendingYear, this.pendingMonth); }
  setMonth(month: number) { this.pendingMonth = +month; this.api.setYearMonth(this.pendingYear, this.pendingMonth); }
}
