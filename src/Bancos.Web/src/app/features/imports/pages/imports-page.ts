import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ImportsApi } from '../data-access/imports-api';

@Component({
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section><h1>Importaciones</h1><p class="muted">Carga un archivo para un auxiliar.</p><form (submit)="submit($event)"><label>Auxiliar<select name="auxiliary" [(ngModel)]="auxiliaryId" required><option value="" disabled>Selecciona un auxiliar</option>@for (item of api.auxiliaries.value() ?? []; track item.id) { <option [value]="item.id">{{ item.name }}</option> }</select></label><label>Archivo<input #fileInput type="file" name="file" required (change)="selectFile(fileInput.files)"></label><button type="submit" [disabled]="!file() || !auxiliaryId || saving()">{{ saving() ? 'Cargando…' : 'Cargar archivo' }}</button></form>@if (message()) { <p class="notice" role="status">{{ message() }}</p> }<h2>Estado</h2>@if (api.imports.isLoading()) { <p>Cargando importaciones…</p> } @else { <ul class="list">@for (item of api.imports.value() ?? []; track item.id) { <li><strong>{{ item.fileName }}</strong><span>{{ item.status }}</span><small>{{ item.template ?? 'Plantilla pendiente' }}</small>@if (item.failureReason) { <small class="error">{{ item.failureReason }}</small> }</li> } @empty { <li>Sin importaciones.</li> }</ul> }</section>`
})
export class ImportsPage {
  readonly api = inject(ImportsApi);
  auxiliaryId = '';
  readonly file = signal<File | null>(null);
  readonly saving = signal(false);
  readonly message = signal('');
  selectFile(files: FileList | null) { this.file.set(files?.item(0) ?? null); }
  submit(event: SubmitEvent) { event.preventDefault(); const file = this.file(); if (!file || !this.auxiliaryId) return; this.saving.set(true); this.message.set(''); this.api.upload(this.auxiliaryId, file).subscribe({ next: () => { this.message.set('Archivo enviado.'); this.file.set(null); this.api.imports.reload(); this.saving.set(false); }, error: () => { this.message.set('No se pudo cargar el archivo.'); this.saving.set(false); } }); }
}
