---
name: color-admin
description: >
  Skill maestro del template Color Admin para Angular. Enruta a cuatro secciones especializadas:
  page-options (configurar layout/sidebar/menú con AppSettings en TypeScript),
  ui-elements (botones, iconos, tabs, modales, badges, tablas y cualquier elemento HTML visual),
  form (inputs, selects, checkboxes, validación, wizards y plugins de formulario en HTML),
  helper (utility classes CSS de Color Admin: colores, espaciado en px, tamaños, tipografía),
  desktop-enhancements (z-index extendido, ViewEncapsulation.None, estilos exclusivos de desktop).
  Usar SIEMPRE que se construya o modifique cualquier componente Angular que use el template
  Color Admin: layout de página, elementos de UI, formularios, clases CSS de utilidad o
  cuando se necesite un overlay, control de z-index o estilos web-only en desktop.
applyTo: "**/*.{html,ts}"
---

# Skill: Color Admin (maestro)

## Propósito

Este skill es el punto de entrada raíz para todo el conocimiento de **Color Admin** en este
proyecto Angular. Recoge cuatro secciones especializadas, identifica para cada una qué tipo
de archivo cubre y cuándo debe activarse.

**No inventes clases ni estructura HTML.** Todo lo que necesitas está documentado en las
referencias de cada sección.

---

## Secciones disponibles

### 1. Page Options — configuración de layout (`.ts`)

Controla el layout de la página mediante `AppSettings`: sidebar, menús, boxed layout,
full-height, footer fijo, etc.

[reference: page-options/page-options.md]
> Todas las opciones de layout de Color Admin activadas desde TypeScript (AppSettings).
> Incluye patrón base constructor/ngOnDestroy y referencia completa de variantes.

---

### 2. UI Elements — elementos visuales HTML (`.html`)

Botones, alertas, tipografía, tabs, acordeones, modales, iconos, banderas, tablas, badges,
progress bars, media objects, social buttons y widget boxes.

[reference: ui-elements/ui-elements.md]
> Clases CSS y patrones HTML listos para copiar. CSS ya compilado en vendor.min.css.

---

### 3. Form — formularios HTML (`.html`)

Inputs, selects, checkboxes, radios, switches, grupos de entrada, validación visual,
layouts (vertical, horizontal, inline), wizards multi-paso y plugins (datepicker,
timepicker, tagify, editor rico, color picker).

[reference: form/form.md]
> Formularios completos sin CSS personalizado, usando solo clases de Color Admin y ng-bootstrap.

---

### 4. Helper CSS — utility classes (`.html`)

Spacing en px (`mt-10px`, `p-15px`), sizing (`w-200px`, `h-50px`), colores de fondo y texto
con variantes 100–900 (`bg-blue-300`, `text-red`), tipografía (`fs-14px`, `fw-600`),
flex, borders, display, position y shadows.

[reference: helper/helper.md]
> Referencia completa de utility classes propias de Color Admin que amplían Bootstrap 5.

---

### 5. Desktop Enhancements — extensiones de z-index, dark mode y estilos desktop (`.html`, `.ts`, `.scss`)

Utility classes de z-index extendidas (`.z-10`, `.z-20`, `.z-30`, `.z-100`), patrón
`ViewEncapsulation.None`, dark mode correcto para `ng-select` y `ngx-datatable` (selector
`[data-bs-theme="dark"] &`), y reglas sobre dónde colocar estilos exclusivos de desktop.

[reference: desktop-enhancements/desktop-enhancements.md]
> Clases .z-*, ViewEncapsulation.None, dark mode ng-select/ngx-datatable con
> [data-bs-theme="dark"] &, y reglas de ubicación de estilos web-only.

---

## Reglas globales

1. **Nunca escribir CSS personalizado** si ya existe una clase en `vendor.min.css`.
2. **Page Options** se activa/desactiva en `constructor`/`ngOnDestroy` — nunca dejar estado activo al navegar.
3. **UI Elements y Form** aplican a archivos `.html`; **Page Options** aplica a archivos `.ts`.
4. **Helper** aplica a `.html` para cualquier clase de utilidad que no sea un componente concreto.
5. Para iconos: Bootstrap Icons (`bi bi-*`), FontAwesome 6 (`fas fa-*`), Solar Duotone (`solar:*-bold-duotone`), Simple Line (`icon-*`), Flags (`fi fi-*`).
6. **Estilos exclusivos de desktop** van en `desktop-layout.component.scss` (con `ViewEncapsulation.None`) — nunca en `styles.css` ni en el SCSS del sub-componente.
7. **Bootstrap `.z-*` solo llega a `.z-3`** — para overlays sobre elementos Color Admin usar `.z-10/.z-20/.z-30/.z-100` (ver sección 5).
