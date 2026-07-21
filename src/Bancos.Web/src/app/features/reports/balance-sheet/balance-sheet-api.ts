import { httpResource } from '@angular/common/http';

export interface BalanceSheetLine { name: string; amountCrc: number; }
export interface BalanceSheetResponse {
  assets: BalanceSheetLine[];
  liabilities: BalanceSheetLine[];
  totalAssets: number;
  totalLiabilities: number;
  netWorth: number;
}

export class BalanceSheetApi {
  readonly balanceSheet = httpResource<BalanceSheetResponse>(() => '/api/reports/balance-sheet');
}
