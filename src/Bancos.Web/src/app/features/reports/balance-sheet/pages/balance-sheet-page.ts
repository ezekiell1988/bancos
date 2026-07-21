import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { BalanceSheetApi } from '../balance-sheet-api';

@Component({
  imports: [DecimalPipe],
  providers: [BalanceSheetApi],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './balance-sheet-page.css',
  template: `
<section class="balance-sheet">
  <h1>Estado de Situación Financiera</h1>

  @if (api.balanceSheet.isLoading()) {
    <p class="muted">Cargando…</p>
  } @else if (api.balanceSheet.error()) {
    <p class="notice error">No se pudo cargar el estado de situación financiera.</p>
  } @else if (api.balanceSheet.hasValue()) {
    @let data = api.balanceSheet.value()!;

    <div class="tables-row">
      <div class="table-block">
        <h2>Activos</h2>
        <table>
          <thead><tr><th>Cuenta</th><th class="num">Saldo CRC</th></tr></thead>
          <tbody>
            @for (row of data.assets; track row.name) {
              <tr><td>{{ row.name }}</td><td class="num">{{ row.amountCrc | number:'1.2-2' }}</td></tr>
            } @empty {
              <tr><td colspan="2" class="muted">Sin activos registrados.</td></tr>
            }
          </tbody>
          <tfoot><tr class="subtotal"><td>Total activos</td><td class="num">{{ data.totalAssets | number:'1.2-2' }}</td></tr></tfoot>
        </table>
      </div>

      <div class="table-block">
        <h2>Pasivos</h2>
        <table>
          <thead><tr><th>Cuenta</th><th class="num">Saldo CRC</th></tr></thead>
          <tbody>
            @for (row of data.liabilities; track row.name) {
              <tr><td>{{ row.name }}</td><td class="num">{{ row.amountCrc | number:'1.2-2' }}</td></tr>
            } @empty {
              <tr><td colspan="2" class="muted">Sin pasivos registrados.</td></tr>
            }
          </tbody>
          <tfoot><tr class="subtotal"><td>Total pasivos</td><td class="num">{{ data.totalLiabilities | number:'1.2-2' }}</td></tr></tfoot>
        </table>
      </div>
    </div>

    <div class="net-worth" [class.positive]="data.netWorth >= 0" [class.negative]="data.netWorth < 0">
      <span class="net-label">Patrimonio neto</span>
      <span class="net-amount">{{ data.netWorth | number:'1.2-2' }} CRC</span>
    </div>
  }
</section>
`
})
export class BalanceSheetPage {
  readonly api = inject(BalanceSheetApi);
}
