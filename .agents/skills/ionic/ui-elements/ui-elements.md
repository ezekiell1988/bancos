---
name: ionic-ui-elements
description: >
  Elementos de UI Ionic: ion-accordion para contenido expandible, ion-card para
  tarjetas de contenido con imagen/texto/acciones, e ion-list + ion-item + ion-label
  para listados. Usar al construir listas, tarjetas o secciones colapsables en
  páginas Ionic Angular.
applyTo: "**/*.html"
---

# ionic-ui-elements

Elementos de interfaz de usuario (UI) en Ionic Framework.

## Componentes de esta sección

- [reference: references/ion-accordion.md]
- [reference: references/ion-card.md]
- [reference: references/ion-list-item.md]

## Reglas de UI

1. `ion-card` puede ser clicable con `[button]="true"` y `href` o `(click)`.
2. `ion-list [inset]="true"` requiere `ion-content color="light"` como padre para que se vea correcto en iOS.
3. `ion-accordion-group` puede tener `[multiple]="true"` para permitir varios abiertos al mismo tiempo.
4. Siempre dar `value` único a cada `ion-accordion` dentro del grupo para control programático.
