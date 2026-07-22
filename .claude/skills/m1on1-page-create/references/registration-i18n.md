# Reference: Registro en Rutas, Menú e i18n

Cómo registrar una página nueva en `app.routes.ts`, en `app-menus.service.ts` y cómo agregar
claves de internacionalización con `@ngx-translate/core`.

---

## Registrar en Rutas (`app.routes.ts`)

```typescript
// 1. Importar el page
import { MiPaginaPage } from './pages/<sección>/mi-pagina';

// 2. Agregar la ruta dentro del array de rutas
{
  path: "<sección>/mi-pagina",
  component: MiPaginaPage,
  data: { title: "Mi Página" },
  canActivate: [AuthGuard],
},
```

---

## Registrar en Menú (`app-menus.service.ts`)

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

## Internacionalización (i18n)

El proyecto usa `@ngx-translate/core` con `TranslatePipe`. Los archivos de traducción están en:

```
src/assets/i18n/
  es.json    ← español (idioma principal)
  en.json    ← inglés
```

### Namespaces existentes

`COMMON`, `HEADER`, `LOGIN`, `SIDEBAR`, `THEME`, `ERROR`, `HOME`, `COLUMN`,
`CAMPAIGNS`, `CLIENTS`, `COMERCIOS`, `DOMINIOS`, `USUARIOS`, `ADDRESSES`,
`INVOICES`, `REPORTS`, `STORAGE`.

### Agregar un namespace nuevo

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

### Uso en templates

```html
<!-- Pipe directo -->
{{ 'MI_SECCION.TITLE' | translate }}

<!-- En atributos -->
<input [placeholder]="'COMMON.SEARCH' | translate" />

<!-- Con parámetros interpolados -->
{{ 'COMMON.SHOWING' | translate:{ count: totalCount() } }}
```

### Uso en TypeScript

Preferir siempre el pipe en el template para que las traducciones reactiven en cambio de idioma.
Solo usar `TranslateService.instant()` en casos excepcionales (`alert()`, `confirm()`):

```typescript
// ⚠️ Solo si es estrictamente necesario en TS:
private translate = inject(TranslateService);
this.translate.instant('MI_SECCION.TITLE')
```

### Claves comunes reutilizables (`COMMON.*`)

```
COMMON.LOADING   → "Cargando..."
COMMON.SAVE      → "Guardar"
COMMON.CANCEL    → "Cancelar"
COMMON.CONFIRM   → "Confirmar"
COMMON.DELETE    → "Eliminar"
COMMON.EDIT      → "Editar"
COMMON.SEARCH    → "Buscar..."
COMMON.YES       → "Sí"
COMMON.NO        → "No"
```
