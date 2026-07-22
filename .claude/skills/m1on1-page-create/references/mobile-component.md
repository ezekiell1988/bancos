# Reference: Mobile Component (Ionic cards colapsables)

TypeScript y HTML del sub-componente móvil: tarjetas Ionic colapsables con pull-to-refresh,
infinite scroll, búsqueda y formulario inline.

---

## TypeScript

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  CUSTOM_ELEMENTS_SCHEMA,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MiItemDto } from '../../../../../shared/models';

@Component({
  selector: 'app-mi-pagina-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],   // ← para los elementos <ion-*>
  templateUrl: './mi-pagina-mobile.component.html',
  styleUrls: ['./mi-pagina-mobile.component.scss'],
})
export class MiPaginaMobileComponent {
  items      = input<MiItemDto[]>([]);
  isLoading  = input(false);
  deletingId = input<number | null>(null);

  refresh  = output<void>();
  create   = output<CreatePayload>();
  editSave = output<UpdatePayload & { id: number }>();
  remove   = output<number>();

  // En mobile el expand es puro signal, sin ViewChild ni cdr
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formField       = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing = computed(() => this.editingId() !== null);

  toggleExpand(id: number): void {
    this.expandedId.update(v => v === id ? null : id);
  }

  // ⚠️ DEBE ser async — el refresher necesita esperar antes de cerrarse
  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Infinite scroll (alternativa a paginación en mobile) ────────
  visibleCount = signal(20);
  visibleItems = computed(() => this.items().slice(0, this.visibleCount()));
  hasMore      = computed(() => this.visibleCount() < this.items().length);

  loadMore(): void {
    this.visibleCount.update(n => n + 20);
  }

  // ── Búsqueda Ionic ──────────────────────────────────────────────
  // ⚠️ ion-searchbar emite CustomEvent con { detail: { value } }
  //    NO usar event.target.value — en Ionic no funciona
  onSearchInput(event: CustomEvent): void {
    // this.search.emit((event.detail?.value ?? '') as string);
  }

  openCreate(): void { /* inicializar signals del form, showForm.set(true) */ }
  openEdit(row: MiItemDto): void { /* cargar row en signals del form */ }
  cancelForm(): void { this.showForm.set(false); this.editingId.set(null); }
  submitForm(): void { /* emit create o editSave, luego cancelForm() */ }
  askDelete(id: number): void { this.confirmDeleteId.set(id); }
  cancelDelete(): void { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
```

**Diferencias clave respecto al web component:**
- `CUSTOM_ELEMENTS_SCHEMA` — requerido para los web components de Ionic (`<ion-refresher>`, etc.).
- **Sin** `@ViewChild`, **sin** `ChangeDetectorRef` — el expand en mobile es un `@if` reactivo sobre un signal, no hay datatable imperativo.
- `handleRefresh(event)` — llama a `.complete()` sobre el `ion-refresher` element y **debe ser `async`**.

---

## HTML

```html
<!-- Pull-to-refresh de Ionic -->
<ion-refresher slot="fixed" (ionRefresh)="handleRefresh($event)">
  <ion-refresher-content></ion-refresher-content>
</ion-refresher>

<!-- Spinner de carga -->
@if (isLoading()) {
  <div class="p-3 text-center text-muted">
    <i class="fa fa-spinner fa-spin me-2"></i>Cargando...
  </div>
}

<!-- Formulario crear/editar -->
@if (showForm()) {
  <div class="card mx-2 mb-3">
    <div class="card-header fw-bold">
      {{ isEditing() ? 'Editar' : 'Nuevo' }}
    </div>
    <div class="card-body">
      <div class="mb-2">
        <label class="form-label form-label-sm" for="fCampoMobile">
          Campo <span class="text-danger">*</span>
        </label>
        <input id="fCampoMobile" type="text" class="form-control form-control-sm"
          [value]="formField()"
          (input)="formField.set($any($event.target).value)" />
      </div>
      <div class="d-flex gap-2">
        <button class="btn btn-sm btn-primary" (click)="submitForm()" [disabled]="!formField()">
          <i class="fa fa-save me-1"></i>{{ isEditing() ? 'Guardar' : 'Crear' }}
        </button>
        <button class="btn btn-sm btn-secondary" (click)="cancelForm()">Cancelar</button>
      </div>
    </div>
  </div>
}

<!-- Confirmación eliminar -->
@if (confirmDeleteId() !== null) {
  <div class="alert alert-warning mx-2 mb-2 d-flex flex-column gap-2">
    <span><i class="fa fa-exclamation-triangle me-1"></i>¿Eliminar este registro?</span>
    <div class="d-flex gap-2">
      <button class="btn btn-sm btn-danger" (click)="confirmDelete()">Eliminar</button>
      <button class="btn btn-sm btn-secondary" (click)="cancelDelete()">Cancelar</button>
    </div>
  </div>
}

<!-- Botón nuevo -->
@if (!showForm()) {
  <div class="px-2 mb-3">
    <button class="btn btn-success btn-sm w-100" (click)="openCreate()">
      <i class="fa fa-plus me-1"></i>Nuevo
    </button>
  </div>
}

<!-- Lista de tarjetas colapsables -->
@for (item of visibleItems(); track item.idMiItem) {
  <div class="card mx-2 mb-2">
    <div class="card-header d-flex align-items-center"
      (click)="toggleExpand(item.idMiItem)"
      style="cursor:pointer">
      <span class="fw-semibold flex-grow-1">{{ item.nombre }}</span>
      @if (item.isActive) {
        <span class="badge bg-success me-2">Activa</span>
      } @else {
        <span class="badge bg-secondary me-2">Inactiva</span>
      }
      <i [class]="'fa fa-chevron-' + (expandedId() === item.idMiItem ? 'up' : 'down')"></i>
    </div>

    <!-- Detalle colapsable -->
    @if (expandedId() === item.idMiItem) {
      <div class="card-body">
        <div class="mb-2 text-muted small">
          Actualizado: {{ item.updateAt | date:'dd/MM/yyyy HH:mm' }}
        </div>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-info" (click)="openEdit(item)"
            [disabled]="showForm() || deletingId() === item.idMiItem">
            <i class="fa fa-edit me-1"></i>Editar
          </button>
          <button class="btn btn-sm btn-danger"
            (click)="askDelete(item.idMiItem)"
            [disabled]="showForm() || deletingId() === item.idMiItem">
            @if (deletingId() === item.idMiItem) {
              <i class="fa fa-spinner fa-spin"></i>
            } @else {
              <i class="fa fa-trash me-1"></i>Eliminar
            }
          </button>
        </div>
      </div>
    }
  </div>
}

<!-- Estado vacío -->
@if (items().length === 0 && !isLoading()) {
  <div class="p-4 text-center text-muted">
    <i class="fa fa-inbox fa-2x mb-2 d-block"></i>
    No hay registros.
  </div>
}
```

## Notas

- El `@for` itera sobre `visibleItems()` (no `items()`) para soporte de infinite scroll.
- `(click)` en el header de la tarjeta activa el toggle — usar `style="cursor:pointer"`.
- Sin breadcrumb ni `<h1>` — eso es exclusivo del coordinador desktop.
