---
name: ionic-overlays
description: >
  Overlays Ionic: ion-modal (diálogo completo / sheet modal / card modal),
  ion-alert (confirmaciones y formularios cortos), ion-action-sheet (menús de
  acción contextuales), ion-popover (tooltips y menús emergentes) e ion-toast
  (notificaciones no intrusivas). Usar al implementar cualquier overlay, diálogo
  o notificación en Ionic Angular. Los overlays pueden activarse vía trigger
  HTML o por controlador programático desde TypeScript.
applyTo: "**/*.{html,ts}"
---

# ionic-overlays

Componentes de capa superpuesta (overlays) en Ionic Framework.

## Componentes de esta sección

- [reference: references/ion-modal.md]
- [reference: references/ion-alert.md]
- [reference: references/ion-action-sheet.md]
- [reference: references/ion-popover.md]
- [reference: references/ion-toast.md]

## Reglas de overlays

1. **Trigger HTML**: todos los overlays aceptan `trigger="id-del-elemento"` para activarse al click sin TypeScript.
2. **Controlador**: usar `ModalController`, `AlertController`, `ActionSheetController`, `PopoverController`, `ToastController` para lógica programática (siempre `await`).
3. **Dismiss**: los overlays se cierran con `dismiss()` programático, o automáticamente con `duration` (toast), o cuando el usuario toca el backdrop.
4. **Retornar datos**: usar `modal.onWillDismiss()` / `modal.onDidDismiss()` que retornan `{ data, role }`.
5. **`isOpen`**: prop boolean para control reactivo desde la plantilla (alternativa al trigger).
