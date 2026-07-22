---
name: color-admin-page-options
description: >
  Guía completa de opciones de layout de páginas en Color Admin Angular. Documenta cómo
  configurar AppSettings y clases body para: sidebar (light, minified, wide, right, search,
  transparent, two-sidebar, sin sidebar), menús (top menu, mixed menu, mega menu), layouts
  (boxed, full height, fixed footer, footer). Usar SIEMPRE que se necesite cambiar el layout
  de una página mediante AppSettings o cuando se configure el modo de visualización del
  sidebar, header o contenido en este template.
applyTo: "**/*.ts"
---

# Skill: Color Admin — Page Options

## Propósito

Documentar todas las variantes de layout disponibles en Color Admin, que se activan en los
componentes Angular mediante `AppSettings` (servicio inyectable) o mediante clases CSS en `document.body`.

**Disparar cuando:**
- Se necesite configurar el sidebar (ancho, posición, aparencia, visibilidad)
- Se pida cambiar el tipo de menú (top menu, mixed menu, mega menu)
- Se requiera un layout de contenido especial (full height, footer fijo, boxed)
- Se use `AppSettings` en un componente Angular de este proyecto

## Patrón base

Toda option de layout sigue el mismo patrón:

```typescript
// constructor → activar
constructor(public appSettings: AppSettings) {
  this.appSettings.appXxx = true;
}

// ngOnDestroy → SIEMPRE desactivar
ngOnDestroy() {
  this.appSettings.appXxx = false;
}
```

**Regla crítica:** Siempre resetear en `ngOnDestroy` para que no afecte otras páginas al navegar.

---

## Referencias de layout

[reference: references/page-blank.md]
> Plantilla base sin opciones de layout. Breadcrumb + page-header + panel.

[reference: references/page-with-boxed-layout.md]
> Layout en caja centrada. Usa `body.boxed-layout`. Incluye variante con Mixed Menu.

[reference: references/page-full-height.md]
> Contenido al 100% de la altura. Usa `appContentFullHeight` + `appContentClass = 'p-0'`.

[reference: references/page-with-fixed-footer.md]
> Footer fijo al fondo con contenido scrollable. Requiere flex en el host component.

[reference: references/page-with-footer.md]
> Footer estándar (fluye con el contenido). No requiere AppSettings.

---

## Referencias de sidebar

[reference: references/page-with-light-sidebar.md]
> Sidebar con tema claro. Activa `appSidebarLight` + `appHeaderInverse`.

[reference: references/page-with-minified-sidebar.md]
> Sidebar colapsado a solo íconos. Activa `appSidebarMinified`.

[reference: references/page-with-wide-sidebar.md]
> Sidebar más ancho. Activa `appSidebarWide`.

[reference: references/page-with-right-sidebar.md]
> Sidebar en el lado derecho. Activa `appSidebarEnd`.

[reference: references/page-with-search-sidebar.md]
> Campo de búsqueda en el sidebar. Activa `appSidebarSearch`.

[reference: references/page-with-transparent-sidebar.md]
> Sidebar con fondo transparente. Activa `appSidebarTransparent`.

[reference: references/page-with-two-sidebar.md]
> Dos sidebars (izquierdo + derecho). Activa `appSidebarTwo` + `appSidebarEndToggled`.

[reference: references/page-without-sidebar.md]
> Sin sidebar. Activa `appSidebarNone`.

---

## Referencias de menú

[reference: references/page-with-top-menu.md]
> Solo top menu horizontal (sin sidebar). Activa `appSidebarNone` + `appTopMenu`.

[reference: references/page-with-mixed-menu.md]
> Sidebar + top menu juntos. Solo activa `appTopMenu` (sidebar permanece visible).

[reference: references/page-with-mega-menu.md]
> Mega-menu en el header. Activa `appHeaderMegaMenu`.

---

## Referencia completa de AppSettings

[reference: references/app-settings-reference.md]
> Tabla completa de todas las propiedades de AppSettings con tipo, default y efecto.

---

## Reglas de uso

1. **Siempre resetear en `ngOnDestroy`** — las propiedades de AppSettings son globales.
2. **Un componente, una opción** — no combinar más de 2-3 opciones en el mismo componente salvo que sea intencional.
3. **No modificar AppSettings en el HTML** — solo en el constructor y ngOnDestroy del TS.
4. **`body.boxed-layout`** es la única opción que usa `document.body.className` directamente; las demás usan AppSettings.
5. Para `page-with-fixed-footer`, el host component necesita las clases flex (`d-flex`, `flex-column`, `h-100`) en el constructor vía `ElementRef`.
