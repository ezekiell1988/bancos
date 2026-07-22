---
name: color-admin-create-page
description: >
  Guía completa para crear una página con componentes web (Color Admin + ngx-datatable + Panel)
  y móvil (Ionic cards) en este proyecto. Cubre estructura de carpetas, page component, HTML,
  web component con Panel/ngx-datatable/row-detail, mobile component con tarjetas colapsables,
  índices, rutas y menú. Usar SIEMPRE que se cree una página nueva de mantenimiento/listado.
---

# Crear una Página Nueva (Color Admin + Ionic)

## 1. Estructura de carpetas

```
src/app/pages/<sección>/<nombre-kebab>/
  <nombre-kebab>.ts          ← Page component
  <nombre-kebab>.html        ← Template del page
  <nombre-kebab>.scss        ← Estilos (vacío normalmente)
  index.ts                   ← Barrel del page
  components/
    index.ts                 ← Barrel de componentes internos
    <nombre-kebab>-web/
      <nombre-kebab>-web.component.ts
      <nombre-kebab>-web.component.html
      <nombre-kebab>-web.component.scss
    <nombre-kebab>-mobile/
      <nombre-kebab>-mobile.component.ts
      <nombre-kebab>-mobile.component.html
      <nombre-kebab>-mobile.component.scss
```

---

## 2. Page Component (`<nombre>.ts`)

```typescript
import {
  Component,
  inject,
  OnInit,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs/operators';
import { AppSettings, MiServicioService, LoggerService } from '../../../service';
import { ResponsiveComponent } from 'src/app/shared';
import { MiPaginaWebComponent } from './components/mi-pagina-web';
import { MiPaginaMobileComponent } from './components/mi-pagina-mobile';

@Component({
  selector: 'app-mi-pagina',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslatePipe, MiPaginaWebComponent, MiPaginaMobileComponent],
  templateUrl: './mi-pagina.html',
  styleUrls: ['./mi-pagina.scss'],
})
export class MiPaginaPage extends ResponsiveComponent implements OnInit {
  private readonly svc    = inject(MiServicioService);
  private readonly logger = inject(LoggerService).getLogger('MiPaginaPage'); // ← getLogger('NombreClase')

  // ── Estado del servicio (expuesto al template) ────────────────────
  isLoading  = this.svc.isLoading;
  error      = this.svc.error;
  items      = this.svc.items;
  totalCount = this.svc.totalCount;

  // ── Estado local ──────────────────────────────────────────────────
  deletingId = signal<number | null>(null);
  hasError   = computed(() => this.error() !== null);

  constructor(public appSettings: AppSettings) {
    super();
    this.appSettings.appSidebarNone = true;
    this.appSettings.appTopMenu = true;
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando página Mi Pagina');
    this.load();
  }

  override ngOnDestroy(): void {
    // ⚠️ SIEMPRE restaurar AppSettings al salir de la página
    this.appSettings.appSidebarNone = false;
    this.appSettings.appTopMenu = false;
    super.ngOnDestroy();
  }

  load(): void {
    this.logger.info('📋 Cargando lista');
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Lista cargada'),
        error: (e) => this.logger.error('❌ Error al cargar:', e),
      });
  }

  onCreate(req: CreateDto): void {
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => { this.logger.success('✅ Creado'); this.load(); },
        error: (e) => this.logger.error('❌ Error al crear:', e),
      });
  }

  onEditSave(req: UpdateDto & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => { this.logger.success('✅ Actualizado'); this.load(); },
        error: (e) => this.logger.error('❌ Error al actualizar:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => { this.logger.success('✅ Eliminado'); this.load(); },
        error: (e) => this.logger.error('❌ Error al eliminar:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }
}
```

**Puntos clave:**
- Extiende `ResponsiveComponent` → provee `isMobile()` y `isDesktop()` como signals computados.
- `AppSettings.appSidebarNone = true` + `appTopMenu = true` en constructor.
- El state del servicio se asigna directamente (`isLoading = this.svc.isLoading`), no se hace subscribe manual — son signals del servicio.
- `deletingId` es signal local del page, no del servicio.
- `ngOnDestroy()` **SIEMPRE** restaura `appSidebarNone = false` + `appTopMenu = false` y llama `super.ngOnDestroy()`.
- Los helpers de **display** (`getStatusBadgeClass`, `formatDate`, `getStatusLabel`) van en los **sub-components** (web/mobile), **NO** en el coordinador.
- Usar `LoggerService.getLogger('NombreClase')` y `finalize()` en todas las suscripciones.

---

## 3. Page HTML (`<nombre>.html`)

```html
<!-- Error global -->
@if (hasError()) {
  <div class="alert alert-danger alert-dismissible fade show mb-3" role="alert">
    <i class="fa fa-exclamation-triangle me-2"></i>
    <strong>Error:</strong> {{ error() }}
    <button type="button" class="btn-close" (click)="clearError()"></button>
  </div>
}

<!-- ========== VERSIÓN MÓVIL ========== -->
@if (isMobile()) {
  <app-mi-pagina-mobile
    [items]="items()"
    [isLoading]="isLoading()"
    [deletingId]="deletingId()"
    (refresh)="load()"
    (create)="onCreate($event)"
    (editSave)="onEditSave($event)"
    (remove)="onDelete($event)">
  </app-mi-pagina-mobile>

} @else {

  <!-- BEGIN breadcrumb -->
  <ol class="breadcrumb float-xl-end">
    <li class="breadcrumb-item"><a href="javascript:;">Home</a></li>
    <li class="breadcrumb-item">Mantenimiento</li>
    <li class="breadcrumb-item active">Mi Página</li>
  </ol>
  <!-- END breadcrumb -->

  <h1 class="page-header">
    Mi Página
    <small>Descripción breve de la sección</small>
  </h1>

  <app-mi-pagina-web
    [items]="items()"
    [totalCount]="totalCount()"
    [isLoading]="isLoading()"
    [deletingId]="deletingId()"
    (refresh)="load()"
    (create)="onCreate($event)"
    (editSave)="onEditSave($event)"
    (remove)="onDelete($event)">
  </app-mi-pagina-web>

}
```

**Puntos clave:**
- El error alert siempre va al tope, fuera del `@if (isMobile())`.
- Mobile: sin breadcrumb, sin `<h1>`.
- Desktop: breadcrumb + `<h1 class="page-header">` + subtítulo en `<small>`.
- Los `input()` del componente hijo se pasan con `()` en el page (son signals): `[items]="items()"`.

---

## 4. Web Component — TypeScript

### 4.1. Sin columna oculta (row-detail)

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

### 4.2. Con columna oculta (row-detail)

Agregar ADEMÁS de lo anterior:

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

**Reglas críticas del row-detail:**
| Error común | Causa | Solución |
|---|---|---|
| Row detail no abre | `@ViewChild('nombre')` devuelve `ElementRef` | Usar `@ViewChild(DatatableRowDetailDirective)` por tipo |
| UI no actualiza | `OnPush` no detecta cambios imperativos | Llamar `cdr.markForCheck()` tras el toggle |
| `rowDetail` undefined | Datatable dentro de un `*ngIf` | Acceder sólo después de `ngAfterViewInit` |

---

## 5. Web Component — HTML

### 5.1. Estructura completa con panel, formulario y row-detail

```html
<!-- ── FORMULARIO CREAR / EDITAR ─────────────────────────── -->
@if (showForm()) {
  <div class="card mb-3">
    <div class="card-header fw-bold">
      <i class="fa fa-edit me-2"></i>{{ formTitle() }}
    </div>
    <div class="card-body">

      <div class="mb-3">
        <label class="form-label">Campo <span class="text-danger">*</span></label>
        <input type="text" class="form-control" [value]="formField()"
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
          <!-- Aquí el contenido expandido. Ej: HTML, JSON, descripción larga -->
          <pre class="bg-dark text-light font-monospace rounded p-3 mb-0"
            style="overflow: auto; max-height: 350px; font-size: 11px; white-space: pre;">{{ row.campoGrande }}</pre>
        </div>
      </ng-template>
    </ngx-datatable-row-detail>

    <!-- Columna con chevron que dispara el toggle -->
    <ngx-datatable-column name="Nombre" prop="nombre" [flexGrow]="2">
      <ng-template ngx-datatable-cell-template let-row="row">
        <button class="btn btn-link p-0 me-1 text-decoration-none" (click)="toggleExpand(row)" title="Ver detalle">
          <i [class]="expandedId() === row.idMiItem
            ? 'fa fa-chevron-down text-primary'
            : 'fa fa-chevron-right text-muted'"></i>
        </button>
        <span class="fw-semibold">{{ row.nombre }}</span>
      </ng-template>
    </ngx-datatable-column>

    <!-- Columna de badges (d-flex flex-wrap para que no se corten) -->
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
            title="Editar">
            <i class="fa fa-edit"></i>
          </button>
          <button class="btn btn-xs btn-danger"
            (click)="askDelete(row.idMiItem)"
            [disabled]="showForm() || deletingId() === row.idMiItem"
            title="Eliminar">
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

### 5.2. Checklist del Panel

- `variant="inverse"` — siempre, para el header oscuro del proyecto.
- `[showReloadButton]="true"` + `[reload]="isLoading()"` + `(onReload)="refresh.emit()"`.
- El botón de creación va en `<ng-container panel-header>` con `ms-3` — NO dentro del body.
- El `ngx-datatable` va directamente dentro del `<panel>`, sin wrapper adicional.

### 5.3. Checklist del ngx-datatable

- `class="bootstrap"` — tema del proyecto.
- `[columnMode]="ColumnMode.force"` — reparte ancho por `flexGrow`.
- `[rowHeight]="'auto'"` — obligatorio cuando hay badges multilínea o row-detail.
- `[headerHeight]="40"`, `[footerHeight]="50"`.
- `[limit]="20"` para paginación local; usar `[externalPaging]="true"` + `(page)` para paginación server-side.
- **Row detail siempre antes de las columnas** en el template.

---

## 6. Mobile Component — TypeScript

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

  // En mobile el expand es puro signal, no necesita ViewChild ni cdr
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

  visibleItems  = computed(() => this.items().slice(0, this.visibleCount()));
  hasMore       = computed(() => this.visibleCount() < this.items().length);

  loadMore(): void {
    this.visibleCount.update(n => n + 20);
  }

  // ── Búsqueda Ionic (usa CustomEvent, NO Event nativo) ─────────────
  // ⚠️ ion-searchbar emite CustomEvent con { detail: { value } }
  //    NO usar event.target.value — en Ionic no funciona
  onSearchInput(event: CustomEvent): void {
    this.search.emit((event.detail?.value ?? '') as string);
  }

  // ...mismos métodos openCreate/openEdit/cancelForm/submitForm/askDelete/confirmDelete del web component
}
```

**Diferencia clave respecto al web component:**
- `CUSTOM_ELEMENTS_SCHEMA` — requerido para los web components de Ionic (`<ion-refresher>`, etc.).
- **Sin** `@ViewChild`, **sin** `ChangeDetectorRef` — el expand en mobile es un `@if` reactivo sobre un signal, no hay datatable imperativo.
- `handleRefresh(event)` — llama a `.complete()` sobre el `ion-refresher` element.

---

## 7. Mobile Component — HTML

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
        <label class="form-label form-label-sm">Campo <span class="text-danger">*</span></label>
        <input type="text" class="form-control form-control-sm" [value]="formField()"
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
@for (item of items(); track item.idMiItem) {
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

---

## 8. Archivos index.ts (barrels)

**`components/index.ts`:**
```typescript
export * from './mi-pagina-web';
export * from './mi-pagina-mobile';
```

**`components/mi-pagina-web/index.ts`:**
```typescript
export { MiPaginaWebComponent } from './mi-pagina-web.component';
```

**`components/mi-pagina-mobile/index.ts`:**
```typescript
export { MiPaginaMobileComponent } from './mi-pagina-mobile.component';
```

**`index.ts` (raíz del page):**
```typescript
export { MiPaginaPage } from './mi-pagina';
```

---

## 9. Registrar en Rutas (`app.routes.ts`)

```typescript
// 1. Importar el page
import { MiPaginaPage } from './pages/<sección>/mi-pagina';

// 2. Agregar la ruta dentro del array de rutas con children
{
  path: "<sección>/mi-pagina",
  component: MiPaginaPage,
  data: { title: "Mi Página" },
  canActivate: [AuthGuard],
},
```

---

## 10. Internacionalización (i18n)

El proyecto usa `@ngx-translate/core` con `TranslatePipe`. Los archivos de traducción están en:

```
src/assets/i18n/
  es.json    ← español (idioma principal)
  en.json    ← inglés
```

### 10.1. Estructura de claves

Los namespaces raíz existentes son: `COMMON`, `HEADER`, `LOGIN`, `SIDEBAR`, `THEME`, `ERROR`, `HOME`, `COLUMN`, `CAMPAIGNS`, `CLIENTS`, `COMERCIOS`, `DOMINIOS`, `USUARIOS`, `ADDRESSES`, `INVOICES`, `REPORTS`, `STORAGE`.

Para una sección nueva, agregar un bloque con el nombre en MAYÚSCULAS:

**`es.json`:**
```json
"MI_SECCION": {
  "TITLE": "Mi Sección",
  "SUBTITLE": "Gestión de ...",
  "LOADING": "Cargando...",
  "EMPTY": "No hay registros",
  "EMPTY_DESC": "Crea el primero con el botón Nuevo",
  "COLUMN_NOMBRE": "Nombre",
  "COLUMN_ESTADO": "Estado",
  "COLUMN_ACCIONES": "Acciones",
  "BREADCRUMB": "Mi Sección",
  "NEW": "Nuevo",
  "EDIT_TITLE": "Editar Registro",
  "CREATE_TITLE": "Nuevo Registro"
}
```

### 10.2. Uso en templates

```html
<!-- Pipe directo -->
{{ 'MI_SECCION.TITLE' | translate }}

<!-- En atributos -->
<input [placeholder]="'COMMON.SEARCH' | translate" />

<!-- Con parámetros interpolados -->
{{ 'COMMON.SHOWING' | translate:{ count: totalCount() } }}
```

### 10.3. Uso en TypeScript

No usar `TranslateService.instant()` salvo en casos excepcionales. Preferir siempre el pipe en el template para que las traducciones reactiven en cambio de idioma.

```typescript
// ✅ Correcto: en el template
// {{ 'MI_SECCION.TITLE' | translate }}

// ⚠️ Solo si es estrictamente necesario en TS (ej: alert(), confirm()):
// private translate = inject(TranslateService);
// this.translate.instant('MI_SECCION.TITLE')
```

### 10.4. Claves comunes reutilizables (`COMMON.*`)

```
COMMON.LOADING     → "Cargando..."
COMMON.SAVE        → "Guardar"
COMMON.CANCEL      → "Cancelar"
COMMON.CONFIRM     → "Confirmar"
COMMON.DELETE      → "Eliminar"
COMMON.EDIT        → "Editar"
COMMON.SEARCH      → "Buscar..."
COMMON.YES         → "Sí"
COMMON.NO          → "No"
```

---

## 11. Registrar en Menú (`app-menus.service.ts`)

Agregar dentro del submenu de la sección correspondiente:

```typescript
{
  url: "/<sección>/mi-pagina",
  title: "Mi Página",
  icon: "fa fa-table",           // icono Font Awesome para desktop
  iconMobile: "list-outline",    // icono Ionicons para mobile
},
```

---

## 12. Anti-patrones a evitar

| ❌ Incorrecto | ✅ Correcto |
|---|---|
| `@ViewChild('rowDetail')` | `@ViewChild(DatatableRowDetailDirective)` |
| Olvidar `cdr.markForCheck()` tras toggle | Siempre llamar después de `toggleExpandRow()` |
| Botón "Nuevo" dentro del body del panel | Usar slot `<ng-container panel-header>` |
| Panel sin `variant="inverse"` | Siempre `variant="inverse"` |
| `class="material"` en ngx-datatable | Usar `class="bootstrap"` |
| `[rowHeight]="50"` con badges/row-detail | Usar `[rowHeight]="'auto'"` |
| Badges inline sin contenedor | `<div class="d-flex flex-wrap gap-1 py-1">` |
| `ngOnDestroy()` sin llamar `super` | `override ngOnDestroy(): void { super.ngOnDestroy(); }` |
| `ngOnDestroy()` sin restaurar AppSettings | Siempre `appSidebarNone = false` + `appTopMenu = false` antes de `super.ngOnDestroy()` |
| Leer state del servicio en `subscribe` | Asignar directamente: `items = this.svc.items` |
| Mobile con `@ViewChild` para expand | Signal puro: `expandedId.update(v => v === id ? null : id)` |
| `event.target.value` en `ion-searchbar` | Usar `event.detail?.value` — Ionic usa `CustomEvent` con `.detail` |
| `handleRefresh` síncrono | Usar `async/await` con `setTimeout(800)` antes de `.complete()` |
| Helpers de display en el coordinador page | Los helpers (`formatDate`, `getStatusBadgeClass`) van en los **sub-components** |
| `.subscribe()` sin callbacks ni finalize | Usar `{ next, error }` + `finalize()` siempre |
