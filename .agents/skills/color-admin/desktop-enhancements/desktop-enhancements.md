---
name: color-admin-desktop-enhancements
description: >
  Extensiones y mejoras para el layout desktop de Color Admin que no vienen de fábrica.
  Cubre: utility classes de z-index extendidas (.z-10/.z-20/.z-30/.z-100), patrón
  ViewEncapsulation.None en desktop-layout para proyectar estilos a páginas hijas,
  dark mode correcto para ng-select (selector [data-bs-theme="dark"] vs .dark-mode),
  dark mode correcto para ngx-datatable, y reglas de dónde colocar estilos exclusivos
  de web (desktop-layout.component.scss). Usar cuando se necesite un overlay sobre
  contenido Color Admin, ng-select o ngx-datatable no respondan al dark mode, o haya
  que agregar estilos exclusivos de desktop sin contaminar mobile.
applyTo: "**/*.{html,ts,scss}"
---

# Skill: Color Admin — Desktop Enhancements

## Propósito

Color Admin incluye estilos propios (ej. `.news-feed` usa `z-index: 20 `) que superan
los valores de Bootstrap 5 (`.z-0` a `.z-3`). Esta sección documenta las extensiones
que se mantienen en `desktop-layout.component.scss` y el patrón `ViewEncapsulation.None`
que permite que lleguen a todos los componentes hijos renderizados en el `<router-outlet>`.

**Disparar cuando:**
- Se necesite un overlay sobre contenido Color Admin (loading, modal personalizado)
- Se use `style="z-index: N"` inline y hay que reemplazarlo con una clase
- Se quiera agregar un estilo exclusivo de web sin afectar la versión mobile
- Se pregunte por qué `.z-3` de Bootstrap no es suficiente en páginas Color Admin
- `ng-select` no responda al dark mode (no cambia fondo/texto al activar modo oscuro)
- `ngx-datatable` no responda al dark mode (footer oscuro en light, bordes fijos, etc.)
- Se vea `.dark-mode &` en SCSS y no funciona — el selector correcto es `[data-bs-theme="dark"] &`

## Referencias

[reference: references/z-index-utilities.md]
> Utility classes .z-10/.z-20/.z-30/.z-100 en desktop-layout.component.scss y el patrón
> ViewEncapsulation.None. Cuándo usar cada nivel y qué valores usa Color Admin internamente.

[reference: references/ng-select-darkmode.md]
> Dark mode correcto para ng-select: selector [data-bs-theme="dark"] &, override SCSS
> completo en angular.scss, anti-patrón de colores hardcodeados en SCSS de componente.

[reference: references/ngx-datatable-darkmode.md]
> Dark mode correcto para ngx-datatable: override SCSS completo, por qué NO importar
> themes/bootstrap.css, anti-patrón ::ng-deep con colores fijos, áreas inline con var CSS.

## Reglas de uso

1. **Nunca `style="z-index: N"` inline** — usar siempre las clases `.z-*` del layout.
2. **Estilos exclusivos de desktop** van en `desktop-layout.component.scss`, no en `styles.css`.
3. `styles.css` es solo para variables Ionic globales y `body.ionic-mode` (código compartido).
4. Los sub-componentes web **no tienen `.scss`** — todo se resuelve con Bootstrap utilities +
   Color Admin utilities + las clases `.z-*` disponibles vía `ViewEncapsulation.None`.
5. **Dark mode**: el selector SCSS correcto es siempre `[data-bs-theme="dark"] &` —
   nunca `.dark-mode &` (esa clase no existe en el DOM de este proyecto).
6. **Dark mode en ng-select y ngx-datatable**: los overrides van en `angular.scss` con
   variables CSS (`var(--bs-component-bg)`, etc.), no en el SCSS del componente con colores fijos.
