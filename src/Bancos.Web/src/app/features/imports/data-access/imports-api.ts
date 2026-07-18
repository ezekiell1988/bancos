import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Auxiliary { id: string; name: string; }
export interface ImportItem { id: string; accountAuxiliaryId: string; fileName: string; status: 'Queued' | 'Processing' | 'Completed' | 'Failed'; template: string | null; failureReason: string | null; processedUtc: string | null; }

@Injectable()
export class ImportsApi {
  private readonly http = inject(HttpClient);
  readonly auxiliaries = httpResource<Auxiliary[]>(() => '/api/accounts/auxiliaries');
  readonly imports = httpResource<ImportItem[]>(() => '/api/imports');

  upload(accountAuxiliaryId: string, file: File) {
    const payload = new FormData();
    payload.set('accountAuxiliaryId', accountAuxiliaryId);
    payload.set('file', file);
    return this.http.post<ImportItem>('/api/imports/upload', payload);
  }
}
