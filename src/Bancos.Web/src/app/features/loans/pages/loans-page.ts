import { DecimalPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LoansApi } from '../loans-api';

@Component({
  imports: [DecimalPipe, DatePipe],
  providers: [LoansApi],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './loans-page.css',
  template: `
<section class="loans-page">
  @if (api.loans.isLoading()) {
    <p class="muted">Cargando…</p>
  } @else if (api.loans.error()) {
    <p class="notice error">No se pudo cargar los préstamos y financiamientos.</p>
  } @else if (api.loans.hasValue()) {
    @let data = api.loans.value()!;

    <header class="loans-header">
      <h1>Préstamos y Financiamientos</h1>
      <div class="total-mensual">
        <span class="total-label">Pago fijo mensual</span>
        <span class="total-amount">{{ data.totalMensualCrc | number:'1.0-0' }} CRC</span>
      </div>
    </header>

    <section class="loans-section">
      <h2>Financiamientos BAC</h2>
      @if (data.financiamientos.length === 0) {
        <p class="muted">Sin financiamientos activos.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Concepto</th>
              <th class="center">Cuotas</th>
              <th class="num">Cuota</th>
              <th class="num">Saldo faltante</th>
              <th class="center">Moneda</th>
            </tr>
          </thead>
          <tbody>
            @for (f of data.financiamientos; track f.concept + f.installments) {
              <tr>
                <td>{{ f.concept }}</td>
                <td class="center mono">{{ f.installments }}</td>
                <td class="num">{{ f.installmentAmount | number:'1.0-0' }}</td>
                <td class="num">{{ f.outstandingBalance | number:'1.0-0' }}</td>
                <td class="center">{{ f.currencyCode }}</td>
              </tr>
            }
          </tbody>
          <tfoot>
            <tr class="subtotal">
              <td colspan="2">Total financiamientos CRC</td>
              <td class="num">{{ financiamientosTotalCrc(data.financiamientos) | number:'1.0-0' }}</td>
              <td colspan="2"></td>
            </tr>
          </tfoot>
        </table>
      }
    </section>

    <section class="loans-section">
      <h2>Préstamos Coopealianza</h2>
      @if (data.prestamos.length === 0) {
        <p class="muted">Sin préstamos registrados.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Préstamo</th>
              <th class="num">Cuota total</th>
              <th class="num">Capital</th>
              <th class="num">Intereses</th>
              <th class="num">Saldo vigente</th>
              <th>Último pago</th>
            </tr>
          </thead>
          <tbody>
            @for (p of data.prestamos; track p.nombre) {
              <tr>
                <td>{{ p.nombre }}</td>
                <td class="num">{{ p.cuotaTotal | number:'1.0-0' }}</td>
                <td class="num">{{ p.capital | number:'1.0-0' }}</td>
                <td class="num">{{ p.interest | number:'1.0-0' }}</td>
                <td class="num">{{ p.saldoVigente | number:'1.0-0' }}</td>
                <td>{{ p.ultimoPago | date:'dd/MM/yyyy' }}</td>
              </tr>
            }
          </tbody>
          <tfoot>
            <tr class="subtotal">
              <td>Total préstamos</td>
              <td class="num">{{ data.prestamos[0].cuotaTotal | number:'1.0-0' }}</td>
              <td colspan="4"></td>
            </tr>
          </tfoot>
        </table>
      }
    </section>
  }
</section>
`
})
export class LoansPage {
  readonly api = inject(LoansApi);

  financiamientosTotalCrc(financiamientos: { installmentAmount: number; currencyCode: string }[]) {
    return financiamientos.filter(f => f.currencyCode === 'CRC').reduce((sum, f) => sum + f.installmentAmount, 0);
  }
}
