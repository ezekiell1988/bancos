import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ImportsApi } from '../data-access/imports-api';

@Component({
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './imports-page.css',
  template: `<section class="imports"><div class="hero"><p class="eyebrow">Bancos · Importaciones</p><h1>Sube tus estados de cuenta</h1><p>Arrastra varios archivos y envíalos a un auxiliar.</p></div><form (submit)="submit($event)"><label>Auxiliar<select name="auxiliary" [(ngModel)]="auxiliaryId" required><option value="" disabled>Selecciona un auxiliar</option>@for (item of api.auxiliaries.value() ?? []; track item.id) { <option [value]="item.id">{{ item.name }}</option> }</select></label>@if (!api.auxiliaries.isLoading() && !(api.auxiliaries.value()?.length)) { <p class="empty" role="status">No hay auxiliares creados todavía. Crea uno desde la API antes de importar.</p> }<label class="dropzone" [class.dragging]="dragging()" (dragover)="dragOver($event)" (dragleave)="dragging.set(false)" (drop)="drop($event)"><input #fileInput type="file" name="files" multiple (change)="selectFiles(fileInput.files)"><strong>Arrastra archivos aquí</strong><span>o selecciónalos desde tu equipo</span></label>@if (files().length) { <ul class="queue">@for (item of files(); track item.name + item.size) { <li>{{ item.name }} <button type="button" (click)="remove(item)">Quitar</button></li> }</ul> }<button type="submit" [disabled]="!files().length || !auxiliaryId || saving()">{{ saving() ? 'Cargando…' : 'Importar ' + files().length + ' archivo(s)' }}</button></form>@if (message()) { <p class="notice" role="status">{{ message() }}</p> }<h2>Actividad reciente</h2><ul class="list">@for (item of api.imports.value() ?? []; track item.id) { <li><strong>{{ item.fileName }}</strong><span>{{ item.status }}</span><small>{{ item.template ?? 'Plantilla pendiente' }}</small></li> } @empty { <li>Sin importaciones todavía.</li> }</ul></section>`
})
export class ImportsPage {
  readonly api = inject(ImportsApi);
  auxiliaryId = '';
  readonly files = signal<File[]>([]);
  readonly dragging = signal(false);
  readonly saving = signal(false);
  readonly message = signal('');
  selectFiles(items: FileList | null) { this.add(items); }
  dragOver(event: DragEvent) { event.preventDefault(); this.dragging.set(true); }
  drop(event: DragEvent) { event.preventDefault(); this.dragging.set(false); this.add(event.dataTransfer?.files ?? null); }
  remove(file: File) { this.files.update(items => items.filter(item => item !== file)); }
  private add(items: FileList | null) { if (items) this.files.update(current => [...current, ...Array.from(items)]); }
  submit(event: SubmitEvent) { event.preventDefault(); const files = this.files(); if (!files.length || !this.auxiliaryId) return; this.saving.set(true); let left = files.length; this.message.set(''); files.forEach(file => this.api.upload(this.auxiliaryId, file).subscribe({ next: () => { if (!--left) { this.message.set('Archivos enviados.'); this.files.set([]); this.api.imports.reload(); this.saving.set(false); } }, error: () => { this.message.set('Un archivo no pudo cargarse.'); this.saving.set(false); } })); }
}
