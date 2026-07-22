---
name: ionic
description: >
  Skill maestro para Ionic Framework en Angular standalone (v7+). Cubre layout de página,
  navegación (tabs, menú, breadcrumbs), elementos de UI (cards, listas, accordion),
  overlays (modal, alert, toast, popover, action-sheet) y formularios (inputs, buttons,
  selects, toggles). Usar SIEMPRE que se construya o modifique cualquier componente Angular
  que use Ionic: estructuras de página, componentes de UI, formularios interactivos u overlays.
  Todos los imports son de '@ionic/angular/standalone'.
applyTo: "**/*.{html,ts}"
---

# ionic

Guía maestra para usar Ionic Framework con Angular Standalone en este proyecto.

## Regla de importación principal

**Todos los componentes Ionic se importan desde `@ionic/angular/standalone`**:

```typescript
import { IonContent, IonHeader, IonToolbar, IonTitle } from '@ionic/angular/standalone';

@Component({
  imports: [IonContent, IonHeader, IonToolbar, IonTitle],
})
```

Los iconos requieren registro explícito vía `addIcons` de `ionicons`:

```typescript
import { addIcons } from 'ionicons';
import { heart, star } from 'ionicons/icons';

constructor() {
  addIcons({ heart, star });
}
```

## Secciones disponibles

- [reference: layout/layout.md]
- [reference: navigation/navigation.md]
- [reference: ui-elements/ui-elements.md]
- [reference: overlays/overlays.md]
- [reference: forms/forms.md]

## Reglas globales

1. **Imports standalone**: Nunca usar `IonicModule` — siempre importar componentes individuales desde `@ionic/angular/standalone`.
2. **Estructura de página**: Toda página Ionic debe tener `.ion-page` como clase raíz. Dentro: `<ion-header>` + `<ion-content>` + `<ion-footer>` (opcional).
3. **Iconos**: Registrar iconos con `addIcons({...})` en el constructor del componente que los usa, o una vez en `app.component.ts` para uso global.
4. **Colores disponibles**: `primary`, `secondary`, `tertiary`, `success`, `warning`, `danger`, `light`, `medium`, `dark`.
5. **Modos**: Los componentes admiten `mode="ios"` o `mode="md"` para forzar estilos de plataforma.
6. **CSS utilities**: Usar clases `ion-padding`, `ion-margin`, `ion-text-center`, `ion-align-items-center`, `ion-justify-content-between`, etc.
7. **Accesibilidad**: Siempre incluir `aria-label` en elementos sin texto visible (botones de solo icono, inputs sin label visual).
