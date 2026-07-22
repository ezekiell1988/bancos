# Reference: Dark Mode — ng-select con Color Admin

## El Problema Raíz

`@ng-select/ng-select` usa colores hardcodeados (`white`, `#f5f5f5`, etc.). Color Admin activa
el dark mode poniendo `data-bs-theme="dark"` en `<html>`, por lo que el selector SCSS correcto es:

```scss
[data-bs-theme="dark"] &   // ✅ CORRECTO
.dark-mode &               // ❌ NUNCA — esa clase no existe en el DOM de este proyecto
```

---

## 1. Dónde poner los overrides

En `src/scss/angular.scss`, dentro de un bloque `.ng-select-custom { ... }` global.
**No** en SCSS de componente — los overrides con `::ng-deep` y colores hardcodeados rompen el dark mode.

---

## 2. Override SCSS completo

```scss
/* ── ng-select dark mode ─────────────────────────────── */

.ng-select-custom {
  // Contenedor principal — TODOS los estados (default, opened, focused)
  // ⚠️  ng-select aplica background:#fff en .ng-select-opened y .ng-select-focused
  //     con mayor especificidad. Usar !important o cubrir todos los selectores.
  .ng-select-container,
  &.ng-select-opened > .ng-select-container,
  &.ng-select-focused > .ng-select-container,
  &.ng-select-focused .ng-select-container {
    background-color: var(--#{$prefix}component-bg) !important;
    color: var(--#{$prefix}component-color) !important;
    border-color: var(--#{$prefix}component-border-color) !important;
  }

  .ng-select-container {
    .ng-input > input {
      color: var(--#{$prefix}component-color) !important;
      background-color: transparent !important;
    }

    .ng-placeholder {
      color: var(--#{$prefix}secondary-color);
    }
  }

  // Tags de valores seleccionados (múltiple)
  .ng-value {
    background-color: #007bff;
    color: $white;

    [data-bs-theme="dark"] & {
      background-color: rgba(#007bff, .75);
    }
  }

  // Botón × de limpiar todo
  .ng-clear-wrapper {
    color: var(--#{$prefix}secondary-color);

    &:hover { color: $danger; }
  }

  // Flecha indicador
  .ng-arrow-wrapper .ng-arrow {
    border-color: var(--#{$prefix}secondary-color) transparent transparent;
  }
  &.ng-select-opened .ng-arrow-wrapper .ng-arrow {
    border-color: transparent transparent var(--#{$prefix}secondary-color);
  }

  // Focus: box-shadow y borde
  &.ng-select-focused > .ng-select-container,
  &.ng-select-focused .ng-select-container {
    border-color: #80bdff !important;
    box-shadow: 0 0 0 .2rem rgba(0, 123, 255, .25) !important;

    [data-bs-theme="dark"] & {
      border-color: rgba(#80bdff, .6) !important;
      box-shadow: 0 0 0 .2rem rgba(0, 123, 255, .15) !important;
    }
  }

  // Disabled
  &.ng-select-disabled .ng-select-container {
    background-color: var(--#{$prefix}secondary-bg);
    opacity: .6;
    cursor: not-allowed;
  }

  // Dropdown panel
  .ng-dropdown-panel {
    background-color: var(--#{$prefix}component-bg);
    border-color: var(--#{$prefix}component-border-color);
    box-shadow: 0 .5rem 1rem rgba($black, .175);

    [data-bs-theme="dark"] & {
      box-shadow: 0 .5rem 1rem rgba($black, .4);
    }

    .ng-dropdown-panel-items {
      .ng-option {
        background-color: var(--#{$prefix}component-bg);
        color: var(--#{$prefix}component-color);

        &:hover,
        &.ng-option-marked {
          background-color: var(--#{$prefix}tertiary-bg);
          color: var(--#{$prefix}component-color);
        }

        &.ng-option-selected,
        &.ng-option-selected.ng-option-marked {
          background-color: rgba(#007bff, .15);
          color: var(--#{$prefix}component-color);

          [data-bs-theme="dark"] & {
            background-color: rgba(#007bff, .25);
          }
        }

        &.ng-option-disabled {
          color: var(--#{$prefix}secondary-color);
          opacity: .6;
        }
      }
    }

    // Header/Footer del panel
    .ng-dropdown-header,
    .ng-dropdown-footer {
      background-color: var(--#{$prefix}component-bg);
      border-color: var(--#{$prefix}component-border-color);
      color: var(--#{$prefix}component-color);
    }
  }
}
```

---

## 3. Anti-patrón frecuente: overrides con colores hardcodeados en SCSS de componente

```scss
/* ❌ Incorrecto — rompe dark mode */
.ng-select-custom {
  .ng-select-container {
    border: 1px solid #ced4da;
    background-color: #e9ecef;
  }
  .ng-dropdown-panel {
    background-color: white;
    border: 1px solid rgba(0,0,0,.15);
  }
  .ng-option:hover { background-color: #f8f9fa; }
  .ng-option-selected { background-color: #e7f3ff; }
}
```

**Solución**: dejar en los SCSS de componente solo reglas de layout (tamaños, padding,
border-radius); todo lo relacionado a colores va en `angular.scss` con variables CSS.

---

## Notas

- Aplicar la clase `ng-select-custom` directamente en el `<ng-select>` del HTML.
- Los overrides de `angular.scss` aplican a todos los ng-select del proyecto que tengan esa clase.
