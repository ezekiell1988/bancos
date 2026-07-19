import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, effect, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

export interface Auxiliary { id: string; name: string; }
export interface ImportProgress { importId: string; attempt: number; stage: string; current: number; total: number; percent: number; status: 'Queued' | 'Processing' | 'Completed' | 'Failed'; updatedUtc: string; }
export interface ImportItem { id: string; accountAuxiliaryId: string; fileName: string; status: 'Queued' | 'Processing' | 'Completed' | 'Failed'; template: string | null; failureReason: string | null; processedUtc: string | null; progress: ImportProgress | null; }
export type ImportPreviewStatus = 'ready' | 'needs-type' | 'unsupported';
export interface ImportPreview { template: string; label: string; status: ImportPreviewStatus; message: string | null; }
export interface ImportPreviewBatch { entries: { entryIndex: number; path: string; preview: ImportPreview }[]; }

@Injectable()
export class ImportsApi {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly connection: HubConnection = new HubConnectionBuilder().withUrl('/hubs/import-progress').withAutomaticReconnect([0, 1000, 3000, 5000]).build();
  private readonly subscribed = new Set<string>();
  private readonly fallbackPolling = window.setInterval(() => {
    if (this.connection.state !== HubConnectionState.Connected && this.imports.hasValue()) this.imports.reload();
  }, 5000);
  readonly imports = httpResource<ImportItem[]>(() => '/api/imports');

  constructor() {
    this.connection.on('ProgressUpdated', (progress: ImportProgress) => this.applyProgress(progress));
    this.connection.onreconnected(() => { this.subscribed.clear(); this.imports.reload(); void this.subscribeToActiveImports(); });
    this.destroyRef.onDestroy(() => { window.clearInterval(this.fallbackPolling); void this.connection.stop(); });
    effect(() => {
      if (this.imports.hasValue()) void this.subscribeToActiveImports();
    });
  }

  preview(file: File) {
    const payload = new FormData();
    payload.set('file', file);
    return this.http.post<ImportPreviewBatch>('/api/imports/preview', payload);
  }

  upload(file: File, entryPath: string, entryIndex: number, template?: string) {
    const payload = new FormData();
    payload.set('entryPath', entryPath);
    payload.set('entryIndex', entryIndex.toString());
    if (template) payload.set('template', template);
    payload.set('file', file);
    return this.http.post<ImportItem>('/api/imports/upload', payload);
  }

  private async subscribeToActiveImports() {
    const imports = this.imports.value();
    if (!imports) return;
    if (this.connection.state === HubConnectionState.Disconnected) {
      try { await this.connection.start(); } catch { return; }
    }
    if (this.connection.state !== HubConnectionState.Connected) return;
    for (const item of imports) {
      if (item.status === 'Completed' || item.status === 'Failed' || this.subscribed.has(item.id)) continue;
      try { await this.connection.invoke('Subscribe', item.id); this.subscribed.add(item.id); } catch { return; }
    }
  }

  private applyProgress(progress: ImportProgress) {
    this.imports.update(items => (items ?? []).map(item => {
      if (item.id !== progress.importId) return item;
      const current = item.progress;
      if (current && (progress.attempt < current.attempt || progress.attempt === current.attempt && progress.percent < current.percent)) return item;
      return { ...item, status: progress.status, progress };
    }));
  }
}
