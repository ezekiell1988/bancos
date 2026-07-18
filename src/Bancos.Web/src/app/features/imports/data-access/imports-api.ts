import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Auxiliary { id: string; name: string; }
export interface ImportItem { id: string; accountAuxiliaryId: string; fileName: string; status: 'Queued' | 'Processing' | 'Completed' | 'Failed'; template: string | null; failureReason: string | null; processedUtc: string | null; }
export type ImportPreviewStatus = 'ready' | 'needs-type' | 'unsupported';
export interface ImportPreview { template: string; label: string; status: ImportPreviewStatus; message: string | null; }
export interface ImportPreviewBatch { entries: { path: string; preview: ImportPreview }[]; }

@Injectable()
export class ImportsApi {
  private readonly http = inject(HttpClient);
  readonly imports = httpResource<ImportItem[]>(() => '/api/imports');

  preview(file: File) {
    const payload = new FormData();
    payload.set('file', file);
    return this.http.post<ImportPreviewBatch>('/api/imports/preview', payload);
  }

  upload(file: File, entryPath: string, template?: string) {
    const payload = new FormData();
    payload.set('entryPath', entryPath);
    if (template) payload.set('template', template);
    payload.set('file', file);
    return this.http.post<ImportItem>('/api/imports/upload', payload);
  }
}
