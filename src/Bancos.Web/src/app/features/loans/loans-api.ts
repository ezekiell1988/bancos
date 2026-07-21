import { inject } from '@angular/core';
import { httpResource } from '@angular/common/http';

export interface Financiamiento {
  concept: string;
  installments: string;
  installmentAmount: number;
  outstandingBalance: number;
  currencyCode: string;
}

export interface Prestamo {
  nombre: string;
  cuotaTotal: number;
  capital: number;
  interest: number;
  saldoVigente: number;
  ultimoPago: string | null;
}

export interface LoansResponse {
  financiamientos: Financiamiento[];
  prestamos: Prestamo[];
  totalMensualCrc: number;
}

export class LoansApi {
  readonly loans = httpResource<LoansResponse>(() => '/api/loans');
}
