---
name: ionic-forms
description: >
  Formularios Ionic: ion-button con variantes de estilo, tamaño, íconos y routing,
  e ion-input con label placements, validación, contador, clear button y slots
  start/end. Usar al construir cualquier formulario interactivo o botón de acción
  en una página Ionic Angular.
applyTo: "**/*.html"
---

# ionic-forms

Componentes de formulario e interacción en Ionic Framework.

## Componentes de esta sección

- [reference: references/ion-button.md]
- [reference: references/ion-input.md]

## Reglas de formularios

1. `ion-input` con `fill="solid"` o `fill="outline"` **NO** debe usarse dentro de `ion-item` (es standalone).
2. `ion-input` dentro de `ion-item` no lleva `fill` — se hereda el estilo del item.
3. Validación Angular: usar `ngModel` con directivas como `email`, `required`, `minlength`; combinar con `helperText` y `errorText` de `ion-input`.
4. Para formularios reactivos usar `formControlName` en los campos Ionic.
5. Siempre agregar `aria-label` a botones con solo icono (`slot="icon-only"`).
