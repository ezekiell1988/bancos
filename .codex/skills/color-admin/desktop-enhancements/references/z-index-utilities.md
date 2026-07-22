# Reference: Z-Index Utilities y ViewEncapsulation para Desktop

## Problema

Bootstrap 5 solo provee `.z-0` / `.z-1` / `.z-2` / `.z-3` (valores 0–3). Color Admin usa
`z-index` propios en sus componentes internos:

| Clase/elemento Color Admin | `z-index` interno |
|---|---|
| `.news-caption` (widget news-feed) | 20 |
| Sidebar overlay | ~1020 |
| Modales Bootstrap | 1055 |

Un overlay de carga que necesite **cubrir** el news-feed requiere al menos `z-index: 30`,
valor que Bootstrap no provee como utility class.

---

## Solución: clases `.z-*` en `desktop-layout.component.scss`

Las clases están definidas en:

```
src/familyAccountWeb/src/app/layouts/desktop-layout/desktop-layout.component.scss
```

```scss
// Z-INDEX UTILITIES (desktop)
// Bootstrap 5 solo trae z-0/1/2/3; aquí extendemos para
// casos de overlay dentro de páginas Color Admin.
.z-10  { z-index: 10  !important; }
.z-20  { z-index: 20  !important; }
.z-30  { z-index: 30  !important; }
.z-100 { z-index: 100 !important; }
```

### Cuándo usar cada nivel

| Clase | `z-index` | Uso típico |
|---|---|---|
| `.z-10` | 10 | Overlay básico sobre contenido normal |
| `.z-20` | 20 | Mismo nivel que `.news-caption` |
| `.z-30` | **30** | Overlay **sobre** `.news-caption` ← caso loading/spinner en páginas |
| `.z-100` | 100 | Sobre casi todo el contenido de página, excepto sidebar y modales Bootstrap |

---

## Por qué `ViewEncapsulation.None` en `desktop-layout`

Por defecto Angular encapsula los estilos de cada componente con un atributo único
(`_ngcontent-xxx`). Esto significa que las clases definidas en
`desktop-layout.component.scss` **no alcanzarían** a los componentes hijos que se renderizan
dentro del `<router-outlet>`.

Con `ViewEncapsulation.None` los estilos del componente se comportan como CSS global
(sin atributo de scope), permitiendo que `.z-10`, `.z-20`, `.z-30` y `.z-100` estén
disponibles en **todas las páginas** cargadas en el router-outlet del desktop layout.

```typescript
// desktop-layout.component.ts
import { Component, ViewEncapsulation } from '@angular/core';

@Component({
  selector: 'app-desktop-layout',
  templateUrl: './desktop-layout.component.html',
  styleUrls: ['./desktop-layout.component.scss'],
  encapsulation: ViewEncapsulation.None,   // ← clave
})
export class DesktopLayoutComponent { ... }
```

> **Alcance:** solo afecta a los estilos de `desktop-layout.component.scss`.
> Los demás componentes con encapsulación por defecto no se ven afectados.

---

## Uso en HTML

Ejemplo: overlay de carga sobre una página que contiene un widget news-feed.

```html
<!-- overlay de carga: cubre todo el contenido incluido news-feed (.z-20) -->
<div class="position-absolute top-0 start-0 w-100 h-100
            d-flex align-items-center justify-content-center
            bg-black bg-opacity-50 z-30">
  <div class="spinner-border text-white" role="status">
    <span class="visually-hidden">Cargando...</span>
  </div>
</div>
```

---

## Anti-patrones a evitar

| Anti-patrón | Por qué es incorrecto | Corrección |
|---|---|---|
| `style="z-index: 30"` inline | Hardcodeado, no reutilizable ni documentado | Usar `.z-30` |
| Definir `.z-*` en el SCSS del sub-componente | Viola la regla sin-scss; ya existe en el layout | Usar las clases del `desktop-layout` |
| Poner estilos web-only en `styles.css` | Se carga también en mobile (Ionic contamina) | Usar `desktop-layout.component.scss` |
| Usar `.z-3` de Bootstrap para cubrir `.news-caption` | `z-index: 3` < `z-index: 20` → no cubre | Usar `.z-30` |

---

## Notas

- Si se agrega una clase `.z-*` nueva, agrégala también en este reference.
- Para modales Bootstrap (`z-index: 1055`) el `.z-100` no es suficiente — usar la API de modal de Bootstrap directamente.
- El `ViewEncapsulation.None` en `desktop-layout` es intencional y documentado; no revertirlo.
