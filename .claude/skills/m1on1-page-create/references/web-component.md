# Reference: Web Component (Panel + ngx-datatable)

TypeScript y HTML del sub-componente de escritorio: Panel con cabecera de acción,
formulario inline crear/editar, confirmación de borrado y tabla ngx-datatable con
soporte de row-detail (columna oculta expandible).

---

## TypeScript — Sin row-detail

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxDatatableModule, ColumnMode } from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import { MiItemDto } from '../../../../../shared/models';

@Component({
  selector: 'app-mi-pagina-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './mi-pagina-web.component.html',
  styleUrls: ['./mi-pagina-web.component.scss'],
})
export class MiPaginaWebComponent {
  items      = input<MiItemDto[]>([]);
  totalCount = input(0);
  isLoading  = input(false);
  deletingId = input<number | null>(null);

  refresh  = output<void>();
  create   = output<CreatePayload>();
  editSave = output<UpdatePayload & { id: number }>();
  remove   = output<number>();

  ColumnMode = ColumnMode;

  // Formulario
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formField       = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing = computed(() => this.editingId() !== null);
  formTitle = computed(() => this.isEditing() ? 'Editar Item' : 'Nuevo Item');

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

---

## TypeScript — Con row-detail (columna expandible)

Agregar **además** de lo del bloque anterior:

```typescript
import {
  ChangeDetectorRef,
  ViewChild,
  inject,
  // ...resto igual
} from '@angular/core';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,   // ← importar la directiva
} from '@swimlane/ngx-datatable';

// Dentro de la clase:

  // ── Row detail ─────────────────────────────────────────────────────
  // ⚠️ CRÍTICO: @ViewChild por TIPO, NO por nombre de template ref
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);

  expandedId = signal<number | null>(null);

  toggleExpand(row: MiItemDto): void {
    this.rowDetail.toggleExpandRow(row);
    const id = row.idMiItem;
    this.expandedId.update(k => k === id ? null : id);
    this.cdr.markForCheck();   // ⚠️ obligatorio con OnPush
  }
```

**Errores comunes del row-detail:**

| Error | Causa | Solución |
|---|---|---|
| Row detail no abre | `@ViewChild('nombre')` devuelve `ElementRef` | Usar `@ViewChild(DatatableRowDetailDirective)` por tipo |
| UI no actualiza | `OnPush` no detecta cambios imperativos | Llamar `cdr.markForCheck()` tras el toggle |
| `rowDetail` undefined | Datatable dentro de `*ngIf` | Acceder solo después de `ngAfterViewInit` |

---

## HTML — Estructura completa

```html
<!-- ── FORMULARIO CREAR / EDITAR ─────────────────────────── -->
@if (showForm()) {
  <div class="card mb-3">
    <div class="card-header fw-bold">
      <i class="fa fa-edit me-2"></i>{{ formTitle() }}
    </div>
    <div class="card-body">

      <div class="mb-3">
        <label class="form-label" for="fCampo">Campo <span class="text-danger">*</span></label>
        <input id="fCampo" type="text" class="form-control" [value]="formField()"
          (input)="formField.set($any($event.target).value)" />
      </div>

      <div class="d-flex gap-2">
        <button class="btn btn-primary" (click)="submitForm()" [disabled]="!formField()">
          <i class="fa fa-save me-1"></i>{{ isEditing() ? 'Guardar cambios' : 'Crear' }}
        </button>
        <button class="btn btn-secondary" (click)="cancelForm()">
          <i class="fa fa-times me-1"></i>Cancelar
        </button>
      </div>

    </div>
  </div>
}

<!-- ── CONFIRMACIÓN ELIMINAR ──────────────────────────────── -->
@if (confirmDeleteId() !== null) {
  <div class="alert alert-warning d-flex align-items-center gap-3 mb-3">
    <i class="fa fa-exclamation-triangle fa-lg"></i>
    <span>¿Eliminar el registro seleccionado? Esta acción no se puede deshacer.</span>
    <div class="ms-auto d-flex gap-2">
      <button class="btn btn-sm btn-danger" (click)="confirmDelete()">
        <i class="fa fa-trash me-1"></i>Eliminar
      </button>
      <button class="btn btn-sm btn-secondary" (click)="cancelDelete()">Cancelar</button>
    </div>
  </div>
}

<!-- ── PANEL CON TABLA ────────────────────────────────────── -->
<panel title="Mi Sección"
  variant="inverse"
  [showReloadButton]="true"
  [reload]="isLoading()"
  (onReload)="refresh.emit()">

  <!-- Botón de acción en el header del panel -->
  <ng-container panel-header>
    <button class="btn btn-success btn-sm ms-3" (click)="openCreate()" [disabled]="showForm()">
      <i class="fa fa-plus me-1"></i>Nuevo
    </button>
  </ng-container>

  <ngx-datatable
    class="bootstrap"
    [rows]="items()"
    [columnMode]="ColumnMode.force"
    [headerHeight]="40"
    [footerHeight]="50"
    [rowHeight]="'auto'"
    [limit]="20"
    [count]="totalCount()"
    [loadingIndicator]="isLoading()">

    <!-- ⚠️ Row detail DEBE declararse ANTES de las columnas -->
    <ngx-datatable-row-detail rowHeight="auto" #rowDetail>
      <ng-template let-row="row" ngx-datatable-row-detail-template>
        <div class="p-3 border-start border-4 border-primary bg-light">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <small class="text-muted fw-semibold">
              <i class="fa fa-info-circle me-1"></i>Detalle del registro
            </small>
          </div>
          <!-- Contenido expandido. Ej: HTML, JSON, descripción larga -->
          <pre class="bg-dark text-light font-monospace rounded p-3 mb-0 overflow-auto"
            style="max-height: 350px; font-size: 11px; white-space: pre;">{{ row.campoGrande }}</pre>
        </div>
      </ng-template>
    </ngx-datatable-row-detail>

    <!-- Columna con chevron que dispara el toggle -->
    <ngx-datatable-column name="Nombre" prop="nombre" [flexGrow]="2">
      <ng-template ngx-datatable-cell-template let-row="row">
        <button class="btn btn-link p-0 me-1 text-decoration-none"
          (click)="toggleExpand(row)"
          [attr.aria-label]="expandedId() === row.idMiItem ? 'Contraer detalle' : 'Expandir detalle'">
          <i [class]="expandedId() === row.idMiItem
            ? 'fa fa-chevron-down text-primary'
            : 'fa fa-chevron-right text-muted'"></i>
        </button>
        <span class="fw-semibold">{{ row.nombre }}</span>
      </ng-template>
    </ngx-datatable-column>

    <!-- Columna de badges -->
    <ngx-datatable-column name="Etiquetas" [flexGrow]="3">
      <ng-template ngx-datatable-cell-template let-row="row">
        <div class="d-flex flex-wrap gap-1 py-1">
          @for (tag of row.tags; track tag) {
            <span class="badge bg-secondary">{{ tag }}</span>
          }
          @if (row.tags.length === 0) {
            <span class="text-muted small">—</span>
          }
        </div>
      </ng-template>
    </ngx-datatable-column>

    <!-- Columna booleana -->
    <ngx-datatable-column name="Activo" prop="isActive" [flexGrow]="1">
      <ng-template ngx-datatable-cell-template let-value="value">
        @if (value) {
          <span class="badge bg-success"><i class="fa fa-check me-1"></i>Sí</span>
        } @else {
          <span class="badge bg-danger"><i class="fa fa-times me-1"></i>No</span>
        }
      </ng-template>
    </ngx-datatable-column>

    <!-- Columna de fecha -->
    <ngx-datatable-column name="Actualizado" prop="updateAt" [flexGrow]="2">
      <ng-template ngx-datatable-cell-template let-value="value">
        <span class="text-muted small">{{ value | date:'dd/MM/yyyy HH:mm' }}</span>
      </ng-template>
    </ngx-datatable-column>

    <!-- Columna de acciones -->
    <ngx-datatable-column name="Acciones" [flexGrow]="2" [sortable]="false">
      <ng-template ngx-datatable-cell-template let-row="row">
        <div class="d-flex gap-1">
          <button class="btn btn-xs btn-info" (click)="openEdit(row)"
            [disabled]="showForm() || deletingId() === row.idMiItem"
            aria-label="Editar" title="Editar">
            <i class="fa fa-edit"></i>
          </button>
          <button class="btn btn-xs btn-danger"
            (click)="askDelete(row.idMiItem)"
            [disabled]="showForm() || deletingId() === row.idMiItem"
            aria-label="Eliminar" title="Eliminar">
            @if (deletingId() === row.idMiItem) {
              <i class="fa fa-spinner fa-spin"></i>
            } @else {
              <i class="fa fa-trash"></i>
            }
          </button>
        </div>
      </ng-template>
    </ngx-datatable-column>

  </ngx-datatable>

  <!-- Estado vacío -->
  @if (items().length === 0 && !isLoading()) {
    <div class="text-center py-4 text-muted">
      <i class="fa fa-inbox fa-2x mb-2 d-block"></i>
      No hay registros. Crea uno con el botón <strong>Nuevo</strong>.
    </div>
  }

</panel>
```

---

## Notas

- `variant="inverse"` — siempre, para el header oscuro del proyecto.
- `[showReloadButton]="true"` + `[reload]="isLoading()"` + `(onReload)="refresh.emit()"`.
- El botón "Nuevo" va en `<ng-container panel-header>` con `ms-3` — **NO** dentro del body.
- `class="bootstrap"` en `ngx-datatable` — tema del proyecto.
- `[columnMode]="ColumnMode.force"` — reparte ancho por `flexGrow`.
- `[rowHeight]="'auto'"` — obligatorio cuando hay badges multilínea o row-detail.
- **Row detail siempre antes de las columnas** en el template.
- Usar `[limit]="20"` para paginación local; `[externalPaging]="true"` + `(page)` para server-side.
