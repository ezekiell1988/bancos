---
name: ionic-layout
description: >
  Estructura de páginas Ionic: ion-header, ion-toolbar, ion-title, ion-content,
  ion-footer y sistema de grid (ion-grid, ion-row, ion-col).
  Usar al construir la estructura HTML de cualquier página o componente Ionic.
applyTo: "**/*.html"
---

# ionic-layout

Componentes de estructura y layout de página Ionic.

## Componentes de esta sección

- [reference: references/ion-header-footer.md]
- [reference: references/ion-content.md]
- [reference: references/ion-grid.md]

## Reglas de layout

1. Toda página Ionic usa: `ion-header` → `ion-content` → `ion-footer` (el footer es opcional).
2. `ion-toolbar` siempre va dentro de `ion-header` o `ion-footer`.
3. `ion-title` siempre va dentro de `ion-toolbar`.
4. Para contenido detrás del header transparente usar `[fullscreen]="true"` en `ion-content`.
5. El grid usa 12 columnas por defecto. Los breakpoints son: xs (0), sm (576px), md (768px), lg (992px), xl (1200px).
