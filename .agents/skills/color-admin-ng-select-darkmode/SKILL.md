---
name: color-admin-ng-select-darkmode
description: >
  Guía definitiva para hacer que ng-select funcione correctamente con el sistema de
  dark mode de Color Admin en Angular. Cubre el problema del selector incorrecto,
  dónde ponen los overrides SCSS, las variables CSS para contenedor, dropdown, opciones
  y tags. Usar SIEMPRE que ng-select no responda al dark mode en un proyecto Color Admin.
---

# Dark Mode en ng-select con Color Admin

## El Problema Raíz

`@ng-select/ng-select` viene con estilos propios que usan colores hardcodeados (`white`, `#f5f5f5`, etc.).
Color Admin activa el dark mode poniendo `data-bs-theme="dark"` en `<html>`:

```typescript
// desktop-layout.component.ts
if (this.appSettings.appDarkMode) {
  document.documentElement.setAttribute("data-bs-theme", "dark");
} else {
  document.documentElement.removeAttribute("data-bs-theme");
}
```

El selector SCSS correcto en esta arquitectura es **`[data-bs-theme="dark"] &`**.

> **NUNCA** usar `.dark-mode &` — esa clase no existe en el DOM de este proyecto.

---

## 1. Dónde poner los overrides

En `src/scss/angular.scss`, en un bloque `.ng-select-custom { ... }` global.  
**No** en los SCSS de componente — los overrides de componente con `::ng-deep` y colores hardcodeados rompen el dark mode.

---

## 2. Override SCSS completo

```scss
/* ── ng-select dark mode ─────────────────────────────── */

.ng-select-custom {
  // Contenedor principal — TODOS los estados (default, opened, focused)
  // ⚠️  ng-select aplica background:#fff en .ng-select-opened y .ng-select-focused
  //     con mayor especificidad. Hay que usar !important o cubrir todos los selectores.
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
      color: var(--#{$prefix}component-color) !important;  // ng-select aplica color:#333 con alta especificidad
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

    &:hover {
      color: $danger;
    }
  }

  // Flecha indicador
  .ng-arrow-wrapper .ng-arrow {
    border-color: var(--#{$prefix}secondary-color) transparent transparent;
  }
  &.ng-select-opened .ng-arrow-wrapper .ng-arrow {
    border-color: transparent transparent var(--#{$prefix}secondary-color);
  }

  // Focus: box-shadow y borde — cubrir los estados con selectores directos
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

## 3. ⚠️ Eliminar colores hardcodeados de home.scss (u otros SCSS de componente)

Un problema frecuente: `home.scss` o un SCSS de componente tiene un bloque `.ng-select-custom`
con colores fijos que sobreescriben los de `angular.scss`:

```scss
/* ❌ Incorrecto — rompe dark mode */
.ng-select-custom {
  .ng-select-container {
    border: 1px solid #ced4da;       // fijo
    background-color: #e9ecef;       // fijo
  }
  .ng-value {
    background-color: #007bff;
    color: white;                    // OK, pero el contenedor sí es problema
  }
  .ng-dropdown-panel {
    background-color: white;         // fijo — no cambia en dark
    border: 1px solid rgba(0,0,0,.15);
  }
  .ng-option:hover {
    background-color: #f8f9fa;       // fijo claro — invisible en dark
  }
  .ng-option-selected {
    background-color: #e7f3ff;       // fijo claro
    color: #007bff;
  }
  .ng-placeholder { color: #6c757d; }
  .ng-clear-wrapper { color: #6c757d; }
  .ng-arrow-wrapper .ng-arrow { border-color: #6c757d transparent transparent; }
}
```

**Solución**: dejar en los SCSS de componente solo reglas de layout (tamaños, padding,
border-radius) y mover todo lo relacionado a colores a `angular.scss` con variables CSS.

```scss
/* ✅ Correcto en home.scss u otro SCSS de componente */
.ng-select-custom {
  width: 100%;

  .ng-select-container {
    min-height: 38px;
    border-radius: 0.25rem;   // solo forma, sin color
  }

  .ng-value {
    border-radius: 0.25rem;
    padding: 0.25rem 0.5rem;
    margin: 2px;
    font-size: 0.875rem;      // solo tipografía y espaciado
  }

  .ng-dropdown-panel .ng-dropdown-panel-items {
    max-height: 300px;        // solo altura máxima
  }
}
```

---

## 4. Variables CSS que se usan

| Variable | Significado |
|---|---|
| `var(--bs-component-bg)` | Fondo del panel/componente (blanco en light, oscuro en dark) |
| `var(--bs-component-color)` | Color de texto del componente |
| `var(--bs-component-border-color)` | Borde del componente |
| `var(--bs-secondary-color)` | Color de texto secundario (placeholder, iconos) |
| `var(--bs-secondary-bg)` | Fondo de elementos deshabilitados |
| `var(--bs-tertiary-bg)` | Fondo hover de opciones |

Color Admin redefine estas variables automáticamente al activar `data-bs-theme="dark"`.

---

## 5. Cómo verificar que funciona

1. Compilar SCSS sin errores:
   ```bash
   npx sass src/scss/angular.scss /dev/null --no-source-map \
     --load-path=node_modules \
     --load-path=src/scss/apple
   # → sin output = sin errores
   ```

2. Activar dark mode desde el panel de temas.

3. Inspeccionar `.ng-select-container`: su `background-color` debe ser el color oscuro del componente.

4. Abrir el dropdown: `.ng-dropdown-panel` debe tener fondo oscuro.

5. Pasar el mouse sobre una opción: el hover debe ser visible (no blanco sobre blanco).

---

## 6. Checklist de corrección

- [ ] Bloque `.ng-select-custom` con variables CSS existe en `angular.scss`
- [ ] `.ng-select-container` cubre los estados `opened` y `focused` con `!important`
- [ ] `.ng-input > input` tiene `color` y `background-color` con `!important` (ng-select aplica `color:#333` con alta especificidad)
- [ ] Bloque `focus` usa selectores directos `&.ng-select-focused > .ng-select-container` con `!important`
- [ ] No hay `background-color: white`, `#f8f9fa`, `#e9ecef`, `#ced4da` en overrides de ng-select
- [ ] No hay `color: #6c757d`, `color: #007bff` hardcodeados en reglas de ng-select de componente
- [ ] Los SCSS de componente solo tienen reglas de layout para `.ng-select-custom`
- [ ] SCSS compila sin errores con `npx sass`
- [ ] Selector dark mode es `[data-bs-theme="dark"] &`, NUNCA `.dark-mode &`
