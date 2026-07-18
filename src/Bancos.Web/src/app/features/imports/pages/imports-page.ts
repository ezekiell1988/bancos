import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ImportPreview, ImportsApi } from '../data-access/imports-api';

interface ReviewItem extends ImportPreview { file: File; path: string; selectedTemplate?: string; }

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './imports-page.css',
  template: `<section class="imports"><div class="hero"><p class="eyebrow">Bancos · Importaciones</p><h1>Sube tus estados de cuenta</h1><p>Detectamos el tipo de cada archivo antes de importarlo.</p></div><form (submit)="submit($event)"><label class="dropzone" [class.dragging]="dragging()" (dragover)="dragOver($event)" (dragleave)="dragging.set(false)" (drop)="drop($event)"><input #fileInput type="file" name="files" multiple (change)="selectFiles(fileInput.files)"><strong>Arrastra archivos aquí</strong><span>o selecciónalos desde tu equipo</span></label>@if (items().length) { <h2>Revisión previa</h2><ul class="queue">@for (item of items(); track item.file.name + item.path) { <li><div><strong>{{ item.path }}</strong><span [class]="'status ' + item.status">{{ item.label }}</span>@if (item.status === 'needs-type') { <label>Tipo de archivo<select [value]="item.selectedTemplate ?? ''" (change)="selectTemplate(item, $any($event.target).value)"><option value="" disabled>Selecciona el tipo</option>@for (option of templateOptions; track option.value) { <option [value]="option.value">{{ option.label }}</option> }</select></label> } @else if (item.message) { <small>{{ item.message }}</small> }</div><button type="button" (click)="remove(item)">Quitar</button></li> }</ul> }<button type="submit" [disabled]="!canSubmit()">{{ saving() ? 'Importando…' : 'Confirmar ' + items().length + ' archivo(s)' }}</button></form>@if (message()) { <p class="notice" role="status">{{ message() }}</p> }<h2>Actividad reciente</h2><ul class="list">@for (item of api.imports.value() ?? []; track item.id) { <li><strong>{{ item.fileName }}</strong><span>{{ item.status }}</span><small>{{ item.template ?? 'Plantilla pendiente' }}</small></li> } @empty { <li>Sin importaciones todavía.</li> }</ul></section>`
})
export class ImportsPage {
  readonly api = inject(ImportsApi);
  readonly items = signal<ReviewItem[]>([]);
  readonly dragging = signal(false);
  readonly saving = signal(false);
  readonly message = signal('');
  readonly canSubmit = computed(() => this.items().length > 0 && this.items().every(item => item.status === 'ready') && !this.saving());
  readonly templateOptions = [
    { value: 'bcr-debit-csv-v1', label: 'Movimientos de cuenta' },
    { value: 'bac-credit-financing-xls-v1', label: 'Financiamientos' },
    { value: 'coopealianza-loan-pdf-v1', label: 'Estado de préstamo' }
  ];
  selectFiles(files: FileList | null) { this.add(files); }
  dragOver(event: DragEvent) { event.preventDefault(); this.dragging.set(true); }
  drop(event: DragEvent) { event.preventDefault(); this.dragging.set(false); this.add(event.dataTransfer?.files ?? null); }
  remove(item: ReviewItem) { this.items.update(items => items.filter(current => current !== item)); }
  selectTemplate(item: ReviewItem, template: string) { this.items.update(items => items.map(current => current === item ? { ...current, selectedTemplate: template, status: template ? 'ready' : 'needs-type', label: template ? this.labelFor(template) : current.label, message: null } : current)); }
  private add(files: FileList | null) { if (files) Array.from(files).forEach(file => this.preview(file)); }
  private preview(file: File) { const pending: ReviewItem = { file, path: file.name, template: 'unknown', label: 'Analizando archivos…', status: 'unsupported', message: null }; this.items.update(items => [...items, pending]); this.api.preview(file).subscribe({ next: batch => this.items.update(items => [...items.filter(item => item !== pending), ...batch.entries.map(entry => ({ ...entry.preview, file, path: entry.path }))]), error: () => { this.items.update(items => items.filter(item => item !== pending)); this.message.set('No se pudo abrir el archivo o ZIP.'); } }); }
  private labelFor(template: string) { return this.templateOptions.find(option => option.value === template)?.label ?? 'Tipo seleccionado'; }
  submit(event: SubmitEvent) { event.preventDefault(); const items = this.items(); if (!this.canSubmit()) return; this.saving.set(true); let left = items.length; this.message.set(''); items.forEach(item => this.api.upload(item.file, item.selectedTemplate).subscribe({ next: () => { if (!--left) { this.message.set('Archivos enviados para procesamiento.'); this.items.set([]); this.api.imports.reload(); this.saving.set(false); } }, error: () => { this.message.set('Un archivo no pudo cargarse.'); this.saving.set(false); } })); }
}
