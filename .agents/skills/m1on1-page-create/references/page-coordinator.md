# Reference: Page Coordinator (estructura, Page component, HTML y barrels)

Cubre la estructura de carpetas, el componente coordinador (`.ts` + `.html`) y los
archivos `index.ts` (barrels) para cualquier página nueva de mantenimiento/listado.

---

## Estructura de carpetas

```
src/app/pages/<sección>/<nombre-kebab>/
  <nombre-kebab>.ts          ← Page component (coordinador)
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

## Page Component TypeScript

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
  private readonly logger = inject(LoggerService).getLogger('MiPaginaPage');

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

**Reglas del coordinador:**
- Extiende `ResponsiveComponent` → provee `isMobile()` e `isDesktop()` como signals.
- `AppSettings.appSidebarNone = true` + `appTopMenu = true` en `constructor`.
- El state del servicio se asigna directamente (`isLoading = this.svc.isLoading`), **no** se hace subscribe manual — son signals del servicio.
- `deletingId` es signal local del page, no del servicio.
- `ngOnDestroy()` **SIEMPRE** restaura `appSidebarNone = false` + `appTopMenu = false` y llama `super.ngOnDestroy()`.
- Los helpers de display (`getStatusBadgeClass`, `formatDate`, `getStatusLabel`) van en los **sub-components** (web/mobile), **NO** en el coordinador.
- Usar `LoggerService.getLogger('NombreClase')` y `finalize()` en todas las suscripciones.

---

## Page HTML

```html
<!-- Error global -->
@if (hasError()) {
  <div class="alert alert-danger alert-dismissible fade show mb-3" role="alert">
    <i class="fa fa-exclamation-triangle me-2"></i>
    <strong>Error:</strong> {{ error() }}
    <button type="button" class="btn-close" aria-label="Cerrar" (click)="clearError()"></button>
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

**Notas del template:**
- El error alert va al tope, fuera del `@if (isMobile())`.
- Mobile: sin breadcrumb, sin `<h1>`.
- Desktop: breadcrumb + `<h1 class="page-header">` + subtítulo en `<small>`.
- Los `input()` del componente hijo se pasan con `()` en el page (son signals): `[items]="items()"`.

---

## Archivos index.ts (barrels)

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
