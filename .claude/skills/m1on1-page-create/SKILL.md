---
name: family-account-page-create
description: >
  Guía completa para crear una página nueva en family-account: modelos TypeScript,
  service, estructura de carpetas, page coordinador (ResponsiveComponent + AppSettings),
  sub-componente web (Color Admin + Panel + ngx-datatable con row-detail opcional) y
  mobile (Ionic cards colapsables), barrels, registro en rutas (app.routes.ts), registro en
  menú (AppMenuService), i18n con ngx-translate, accesibilidad WCAG 2.1 AA, sistema CSS
  híbrido desktop/mobile, dark mode unificado y tabla de anti-patrones.
  Usar SIEMPRE que se cree una página nueva en este proyecto.
applyTo: "src/familyAccountWeb/**"
---

# Crear una Página Nueva en family-account

> **Referencias detalladas disponibles** — ver sección al final de este skill:
> `page-coordinator`, `web-component`, `mobile-component`, `registration-i18n`,
> `accessibility-antipatterns` y `css-mix`.
>
> **Máscaras de input (referencia):** `ngx-mask`.
> - Instalación recomendada en este proyecto (Angular 20): `npm install ngx-mask@20`.
> - Uso sugerido: aplicar máscara para UX en teléfono/documentos y mantener validación de negocio en el componente/service.

---

## Arquitectura dual: Web vs Mobile

Cada página tiene **dos sub-componentes** con tecnologías completamente distintas:

| Aspecto | Versión Web | Versión Mobile |
|---|---|---|
| Tecnología | Color Admin (Bootstrap 5 + Font Awesome) | Ionic Framework (skill `ionic`) |
| Skill de referencia | `color-admin` | `ionic` |
| Imports | `CommonModule`, `FormsModule`, `NgxDatatableModule`, `PanelComponent` | Solo componentes individuales de `@ionic/angular/standalone` |
| CSS | **CSS propio = 0**. Solo utility classes Bootstrap/Color Admin | **CSS propio = 0**. Solo utility classes Ionic (`ion-padding`, `ion-margin`, etc.) |
| Alertas/errores | `<div class="alert alert-danger" role="alert">` | `<ion-text color="danger">` |
| Formularios | `ngModel` + clases Bootstrap (`form-floating`, `form-control`) | `ion-input fill="outline"` standalone |
| Layout | Bootstrap grid (`row`/`col-*`) o `PanelComponent` | `ion-grid / ion-row / ion-col` |
| Iconos | Font Awesome (`<i class="fa fa-...">`) | ionicons (`addIcons()` + `<ion-icon name="...">`) |

> **Regla de oro:** nunca mezclar tecnologías entre versiones. Nada de Ionic en el componente web y nada de Bootstrap en el componente mobile.
> **Regla sobre CSS:** los sub-componentes **no deben tener archivo `.scss`** ni `styleUrls`. Color Admin e Ionic proveen todos los estilos necesarios.
> - Si se necesita un estilo **exclusivo de web/desktop** (overlays, z-index extendido), consultar el skill `color-admin` sección **Desktop Enhancements** — ahí están las clases `.z-*` y el patrón `ViewEncapsulation.None`.
> - Si se necesita algo **compartido entre plataformas** (variables Ionic, body.ionic-mode), va en `src/styles.css`.

---

## 0. Modelos TypeScript (`src/app/shared/models/<nombre>.models.ts`)

Antes de crear el service o el page, definir los DTOs que corresponden a los DTOs del API (.NET).

```typescript
// src/app/shared/models/<nombre>.models.ts
export interface <Nombre>Dto {
  id<Nombre>: number;          // PK: id{Entidad} camelCase
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}

export interface Create<Nombre>Request {
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}

export interface Update<Nombre>Request {
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}
```

**Convenciones de modelos:**
- El archivo va en `src/app/shared/models/<nombre>.models.ts` (kebab-case).
- Los interfaces reflejan **exactamente** los records del API (C#): campos en camelCase.
- La PK sigue el patrón `id{Entidad}` (ej: `idAccount`, `idBankAccount`).
- Exportar desde `src/app/shared/models/index.ts` agregando:
  ```typescript
  export * from './<nombre>.models';
  ```

---

## 1. Service (`src/app/service/<nombre>.service.ts`)

```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { <Nombre>Dto, Create<Nombre>Request, Update<Nombre>Request } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class <Nombre>Service {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('<Nombre>Service');
  private readonly base   = `${environment.apiUrl}<ruta-api>`; // ej: 'accounts'

  // ── Estado ───────────────────────────────────────────────────────
  items      = signal<<Nombre>Dto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<<Nombre>Dto[]> {
    this.start();
    return this.http.get<<Nombre>Dto[]>(`${this.base}.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.totalCount.set(res?.length ?? 0);
      }),
      catchError(err => { this.fail('Error al cargar'); return throwError(() => err); }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(req: Create<Nombre>Request): Observable<<Nombre>Dto> {
    this.start();
    return this.http.post<<Nombre>Dto>(`${this.base}/`, req).pipe(
      tap(res => {
        this.items.update(ls => [...ls, res]);
        this.totalCount.update(n => n + 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, req: Update<Nombre>Request): Observable<<Nombre>Dto> {
    this.start();
    return this.http.put<<Nombre>Dto>(`${this.base}/${id}`, req).pipe(
      tap(res => this.items.update(ls => ls.map(i => (i.id<Nombre> === id ? res : i)))),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ELIMINAR ─────────────────────────────────────────────────────
  delete(id: number): Observable<void> {
    this.start();
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.items.update(ls => ls.filter(i => i.id<Nombre> !== id));
        this.totalCount.update(n => n - 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
```

**Convenciones del service:**
- Un service por feature, en `src/app/service/<nombre>.service.ts`.
- Exportar desde `src/app/service/index.ts` agregando:
  ```typescript
  export { <Nombre>Service } from './<nombre>.service';
  ```
- El estado son **signals** (`signal()`) — nunca `BehaviorSubject`.
- `loadList()` llama a `<ruta>.json` (convención del API de este proyecto).
- `create()` llama a `<ruta>/` (con barra final), `update`/`delete` a `<ruta>/{id}`.
- Los mensajes de error se obtienen de `err?.error` primero; si no es string, usar mensaje genérico.
- No hay `PagedResult` para endpoints que devuelven array directo; usarlo solo cuando el API retorna `{ data, totalCount }`.

---

## 2. Estructura de carpetas

```
src/app/pages/
  index.ts                                  ← Barrel global de páginas (actualizar)
  <nombre-kebab>/
    <nombre-kebab>.ts                       ← Page coordinador
    <nombre-kebab>.html                     ← Template coordinador
    index.ts                                ← Barrel del page (actualizar)
    components/
      index.ts                              ← Barrel de componentes internos
      <nombre-kebab>-web/
        <nombre-kebab>-web.component.ts
        <nombre-kebab>-web.component.html
      <nombre-kebab>-mobile/
        <nombre-kebab>-mobile.component.ts
        <nombre-kebab>-mobile.component.html
```

---

## 3. Page coordinador (`<nombre-kebab>.ts`)

Responsabilidades: estado reactivo con signals + llamadas al servicio. **Sin lógica de presentación**.

```typescript
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { AppSettings, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { MiPaginaWebComponent, MiPaginaMobileComponent } from './components'; // ← import corto desde barrel

@Component({
  selector: 'app-mi-pagina',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [MiPaginaWebComponent, MiPaginaMobileComponent],
  templateUrl: './mi-pagina.html',
})
export class MiPaginaPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly logger = inject(LoggerService).getLogger('MiPaginaPage');

  // Estado reactivo
  loading      = signal(false);
  errorMessage = signal('');

  constructor(public appSettings: AppSettings) {
    super();
    // Ajustar layout de Color Admin si la página no usa sidebar
    // this.appSettings.appSidebarNone = true;
    // this.appSettings.appTopMenu     = true;
  }

  override ngOnDestroy(): void {
    // ⚠️ SIEMPRE restaurar AppSettings al salir
    // this.appSettings.appSidebarNone = false;
    // this.appSettings.appTopMenu     = false;
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Mi Página');
    this.load();
  }

  load(): void {
    // this.svc.loadList().subscribe({ next: ..., error: ... });
  }
}
```

**Reglas del coordinador:**
- Extiende `ResponsiveComponent` → provee `isMobile()` e `isDesktop()` como signals computados.
- Estado como `signal()`, nunca propiedades planas mutables para estado reactivo.
- `ngOnDestroy()` **SIEMPRE** llama `super.ngOnDestroy()` y restaura `AppSettings` si se modificó en el constructor.
- Importa únicamente `MiPaginaWebComponent` y `MiPaginaMobileComponent` (ni Ionic ni Bootstrap directamente).
- **Siempre usar el import corto desde el barrel:** `import { MiPaginaWebComponent, MiPaginaMobileComponent } from './components';` — nunca rutas largas hasta los archivos internos.
- Los helpers de display (`formatDate`, `getBadgeClass`) van en los sub-componentes, NO aquí.

---

## 4. Template coordinador (`<nombre-kebab>.html`)

```html
@if (isMobile()) {
  <app-mi-pagina-mobile
    [loading]="loading()"
    [errorMessage]="errorMessage()"
  />
} @else {
  <app-mi-pagina-web
    [loading]="loading()"
    [errorMessage]="errorMessage()"
  />
}
```

**Reglas:**
- Los `input()` del componente hijo se pasan con `()` porque son signals: `[isLoading]="loading()"`.
- No hay breadcrumb, `<h1>` ni contenido presentacional aquí; todo va en el sub-componente web.

---

## 5. Sub-componente Web (`<nombre-kebab>-web.component.ts`)

> **Referencia detallada:** ver `references/web-component.md` — incluye patrón sin/con row-detail, formulario inline, confirmación de borrado y checklist de `PanelComponent` + `ngx-datatable`.
> **Skill:** consultar skill `color-admin` para cualquier elemento UI, formulario o clase CSS.

Color Admin (Bootstrap 5 + Font Awesome) con `PanelComponent` y `ngx-datatable`.

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

@Component({
  selector: 'app-mi-pagina-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './mi-pagina-web.component.html',
})
export class MiPaginaWebComponent {
  // Inputs del coordinador
  loading      = input(false);
  errorMessage = input('');

  // Outputs hacia el coordinador
  refresh = output<void>();

  ColumnMode = ColumnMode;
}
```

### Template web (`<nombre-kebab>-web.component.html`)

```html
<!-- BEGIN breadcrumb -->
<ol class="breadcrumb float-xl-end">
  <li class="breadcrumb-item"><a href="javascript:;">Home</a></li>
  <li class="breadcrumb-item active">Mi Página</li>
</ol>
<!-- END breadcrumb -->

<h1 class="page-header">
  Mi Página
  <small>Descripción breve de la sección</small>
</h1>

<!-- Error global -->
@if (errorMessage()) {
  <div class="alert alert-danger mb-3" role="alert">
    <i class="fa fa-exclamation-triangle me-2"></i>{{ errorMessage() }}
  </div>
}

<!-- Panel con tabla -->
<panel title="Mi Sección"
  variant="inverse"
  [showReloadButton]="true"
  [reload]="loading()"
  (onReload)="refresh.emit()">

  <ngx-datatable
    class="bootstrap"
    [rows]="[]"
    [columnMode]="ColumnMode.force"
    [headerHeight]="40"
    [footerHeight]="50"
    [rowHeight]="'auto'"
    [limit]="20"
    [loadingIndicator]="loading()">

    <ngx-datatable-column name="Columna" prop="campo" [sortable]="true">
      <ng-template let-value="value" ngx-datatable-cell-template>
        {{ value }}
      </ng-template>
    </ngx-datatable-column>

  </ngx-datatable>
</panel>
```

---

## 6. Sub-componente Mobile (`<nombre-kebab>-mobile.component.ts`)

> **Referencia detallada:** ver `references/mobile-component.md`.
> **Skill:** consultar skill `ionic` para cualquier componente, layout, formulario u overlay Ionic.

### Reglas críticas del componente mobile

1. **`host: { class: 'ion-page' }` es OBLIGATORIO** cuando el componente usa `<ion-content>`. Sin esta clase el componente no tiene contexto de altura y `ion-content` renderiza en blanco.
2. **CSS propio = 0.** Usar exclusivamente utility classes Ionic (`ion-padding`, `ion-margin`, `ion-text-center`, `ion-padding-vertical`, etc.). El SCSS debe quedar vacío.
3. **Errores y mensajes:** usar `<ion-text color="danger/success">` — nunca clases CSS custom.
4. **Formularios:** usar `<ion-input fill="outline" labelPlacement="floating">` standalone (sin `<ion-item>` envolvente cuando se usa `fill`). Ver skill `ionic` sección forms.
5. **Layout:** usar `ion-grid / ion-row / ion-col` para centr y distribuir. No usar `display:flex` ni clases Bootstrap.
6. **Todos los imports** de `@ionic/angular/standalone` — nunca `IonicModule`.
7. **Iconos:** registrar con `addIcons()` en el constructor. Agregar solo los íconos que se usen.
8. **`ion-label position="floating"` está deprecado en Ionic 7+.** Usar `labelPlacement="floating"` directamente en `<ion-input>`.

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { addIcons } from 'ionicons';
import { refreshOutline } from 'ionicons/icons';
import {
  IonContent,
  IonGrid,
  IonRow,
  IonCol,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonRefresher,
  IonRefresherContent,
  IonIcon,
  IonSpinner,
  IonText,
} from '@ionic/angular/standalone';

@Component({
  selector: 'app-mi-pagina-mobile',
  host: { class: 'ion-page' },        // ← OBLIGATORIO para que ion-content funcione
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    IonGrid,
    IonRow,
    IonCol,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonRefresher,
    IonRefresherContent,
    IonIcon,
    IonSpinner,
    IonText,
  ],
  templateUrl: './mi-pagina-mobile.component.html',
})
export class MiPaginaMobileComponent implements OnInit {
  // Inputs del coordinador
  loading      = input(false);
  errorMessage = input('');

  // Outputs hacia el coordinador
  refresh = output<void>();

  ngOnInit(): void {
    addIcons({ refreshOutline });
  }

  doRefresh(event: CustomEvent): void {
    this.refresh.emit();
    setTimeout(() => (event.target as HTMLIonRefresherElement).complete(), 1500);
  }
}
```

### Template mobile (`<nombre-kebab>-mobile.component.html`)

```html
<ion-content class="ion-padding">
  <ion-grid>
    <ion-row class="ion-justify-content-center">
      <ion-col size="12" size-sm="10" size-md="8">

        <!-- Pull-to-refresh -->
        <ion-refresher slot="fixed" (ionRefresh)="doRefresh($any($event))">
          <ion-refresher-content></ion-refresher-content>
        </ion-refresher>

        <!-- Error -->
        @if (errorMessage()) {
          <ion-text color="danger">
            <p>
              <ion-icon name="alert-circle-outline" aria-hidden="true"></ion-icon>
              {{ errorMessage() }}
            </p>
          </ion-text>
        }

        <!-- Loading -->
        @if (loading()) {
          <div class="ion-text-center ion-padding">
            <ion-spinner name="crescent"></ion-spinner>
          </div>
        }

        <!-- Contenido -->
        <ion-card>
          <ion-card-header>
            <ion-card-title>Mi Página</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <!-- contenido aquí -->
          </ion-card-content>
        </ion-card>

      </ion-col>
    </ion-row>
  </ion-grid>
</ion-content>
```

---

## 7. Barrel de componentes (`components/index.ts`)

```typescript
export { MiPaginaWebComponent } from './mi-pagina-web/mi-pagina-web.component';
export { MiPaginaMobileComponent } from './mi-pagina-mobile/mi-pagina-mobile.component';
```

---

## 8. Barrel del page (`index.ts`)

```typescript
export { MiPaginaPage } from './mi-pagina';
```

---

## 9. Barrel global de páginas (`src/app/pages/index.ts`)

Agregar el export del nuevo page al archivo existente:

```typescript
// ANTES:
export { HomePage } from './home';
export { ErrorPage } from './error';
export { LoginPage } from './login';

// DESPUÉS (agregar la línea del nuevo page):
export { HomePage } from './home';
export { ErrorPage } from './error';
export { LoginPage } from './login';
export { MiPaginaPage } from './mi-pagina';
```

---

## 10. Rutas (`src/app/app.routes.ts`)

Agregar la ruta al array `routes` en el archivo existente:

```typescript
// app.routes.ts — solo agregar la nueva entrada al array routes:
{
  path: 'maintenance/<nombre-kebab>',   // ← siempre bajo maintenance/
  component: MiPaginaPage,
  data: { title: 'Mi Página' },
  canActivate: [AuthGuard],             // ← incluir siempre si requiere autenticación
},
```

Importar el componente en el mismo archivo:
```typescript
import { MiPaginaPage } from './pages';
```

**Orden de rutas:**
1. `''` → redirect a `/home`
2. `'login'` → sin guard
3. Páginas autenticadas (orden alfabético o por sección)
4. `'**'` → `ErrorPage` (siempre al final)

**Convención de rutas:**
- Las páginas de mantenimiento/catálogos van bajo el prefijo `maintenance/`.
- No usar rutas hijas (children) — el proyecto usa rutas planas (`maintenance/accounts`, no `{ path: 'maintenance', children: [...] }`).

---

## 11. Menú (`src/app/service/app-menus.service.ts`)

Las páginas de mantenimiento van dentro del submenu **Mantenimiento**. Agregar el item dentro de `submenu` del bloque `Mantenimiento` existente:

```typescript
// En app-menus.service.ts — agregar dentro del submenu del item Mantenimiento:
{
  icon: 'fa fa-<icono>',
  iconMobile: '<nombre>-outline',
  title: 'Mi Página',
  url: '/maintenance/<nombre-kebab>',
  roles: [1, 2],
},
```

Si aún no existe el bloque Mantenimiento, crearlo:

```typescript
{
  icon: 'fa fa-wrench',
  iconMobile: 'settings-outline',
  title: 'Mantenimiento',
  url: '/maintenance',
  caret: 'true',
  roles: [1, 2],
  submenu: [
    {
      icon: 'fa fa-<icono>',
      iconMobile: '<nombre>-outline',
      title: 'Mi Página',
      url: '/maintenance/<nombre-kebab>',
      roles: [1, 2],
    },
  ],
},
```

**Roles disponibles:**
| ID | Nombre  | Acceso |
|----|---------|--------|
| 1  | DEV     | Total  |
| 2  | ADMIN   | Todo excepto herramientas internas |
| 3  | LOCAL   | Solo Inicio + secciones asignadas |
| 4  | SUPPORT | Solo Inicio |

Si el item **no** lleva `roles` (o el array está vacío), es visible para **todos** los roles autenticados.

---

## 12. Checklist completo

```
[ ] Crear src/app/shared/models/<nombre>.models.ts
[ ] Actualizar src/app/shared/models/index.ts    (agregar export)
[ ] Crear src/app/service/<nombre>.service.ts
[ ] Actualizar src/app/service/index.ts          (agregar export)
[ ] Crear carpeta src/app/pages/<nombre-kebab>/
[ ] Crear <nombre-kebab>.ts       (coordinador)
[ ] Crear <nombre-kebab>.html     (template coordinador)
[ ] Crear index.ts                (barrel del page)
[ ] Crear components/index.ts     (barrel de sub-componentes)
[ ] Crear components/<nombre>-web/   (2 archivos: .ts, .html)  → Color Admin, sin scss
[ ] Crear components/<nombre>-mobile/  (2 archivos: .ts, .html)  → Ionic, sin scss
[ ] Actualizar src/app/pages/index.ts            (agregar export)
[ ] Actualizar app.routes.ts                     (agregar ruta maintenance/<nombre>)
[ ] Actualizar app-menus.service.ts              (agregar al submenu Mantenimiento)
[ ] Ejecutar build para verificar errores
```

### Anti-patrones críticos — Web (Color Admin)

Consultar la sección **Desktop Enhancements** del skill `color-admin` para la lista completa
de anti-patrones (z-index inline, estilos web-only en `styles.css`, clases CSS en el SCSS
del sub-componente).

| Anti-patrón | Consecuencia | Corrección |
|---|---|---|
| Clase CSS custom en SCSS del componente | No mantenible, viola la regla sin-scss | Usar utility classes Bootstrap/Color Admin |
| `style="z-index: N"` inline en el HTML | Valor hardcodeado, no reutilizable | Usar `.z-10/.z-20/.z-30/.z-100` (ver skill `color-admin`) |

### Anti-patrones críticos — Mobile (Ionic)

| Anti-patrón | Consecuencia | Corrección |
|---|---|---|
| Falta `host: { class: 'ion-page' }` | `ion-content` renderiza en blanco (altura 0) | Agregar `host: { class: 'ion-page' }` al `@Component` |
| CSS propio (clases custom) en SCSS | Rompe dark mode, override difícil | Eliminar CSS, usar utility classes Ionic |
| `ion-item` + `ion-label position="floating"` | Deprecado en Ionic 7+ | Usar `<ion-input fill="outline" labelPlacement="floating">` standalone |
| `IonicModule` en imports | Bundle innecesariamente grande | Importar solo los componentes individuales de `@ionic/angular/standalone` |
| Clases Bootstrap (`d-flex`, `col-6`, `alert`) en mobile | Conflicto de estilos y layouts | Usar `ion-grid/ion-row/ion-col` e `ion-text color=""` |
| Mensajes de error con `<div class="alert alert-danger">` | No respeta dark/light mode Ionic | Usar `<ion-text color="danger">` |
| `<i class="fa fa-...">` para iconos | Sin soporte en Ionic | Usar `<ion-icon name="...">` con `addIcons()` |

---

## Referencias detalladas

[reference: references/page-coordinator.md]
> Estructura de carpetas, Page component (.ts + .html), archivos index.ts (barrels).
> Patrón completo: ResponsiveComponent + AppSettings + LoggerService. HTML del coordinador.

[reference: references/web-component.md]
> Sub-componente de escritorio: Panel + ngx-datatable + formulario inline crear/editar.
> Variante sin y con row-detail (columna expandible). Checklist Panel y ngx-datatable.

[reference: references/mobile-component.md]
> Sub-componente móvil con Ionic: `host: { class: 'ion-page' }` obligatorio, ion-grid layout,
> ion-text para mensajes, ion-input fill="outline" standalone, cero CSS propio.
> Consultar siempre el skill `ionic` para cualquier elemento UI o formulario.

[reference: references/registration-i18n.md]
> Registro en app.routes.ts con AuthGuard, registro en menú (app-menus.service.ts),
> claves @ngx-translate (es.json / en.json) y claves COMMON.* reutilizables.

[reference: references/accessibility-antipatterns.md]
> Reglas WCAG 2.1 AA: for/id, aria-label, btn-close, links con ngTemplateOutlet, alt en imágenes,
> estilos inline prohibidos. Tabla completa de anti-patrones a evitar.

[reference: references/css-mix.md]
> Sistema CSS híbrido para apps desktop+mobile: utility classes Bootstrap vs Ionic,
> detección de plataforma (desktop-mode / ionic-mode), carga dinámica de CSS, dark mode
> unificado con data-bs-theme, breakpoints, patrones comunes y troubleshooting.
