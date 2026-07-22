# Reference: Dark Mode — ngx-datatable con Color Admin

## El Problema Raíz

Color Admin activa el dark mode poniendo `data-bs-theme="dark"` en `<html>`. El selector
SCSS correcto es:

```scss
[data-bs-theme="dark"] &   // ✅ CORRECTO
.dark-mode &               // ❌ NUNCA — esa clase no existe en el DOM de este proyecto
```

---

## 1. Dónde poner los overrides

En `src/scss/angular.scss`, dentro (o justo después) del bloque `.ngx-datatable.bootstrap { ... }`.
**No** en SCSS de componente con `::ng-deep` y colores hardcodeados.

---

## 2. Override SCSS completo

```scss
.ngx-datatable.bootstrap {
  font-size: $font-size-base;

  // ── Header ──────────────────────────────────────────
  & .datatable-header {
    height: auto !important;

    & .datatable-header-inner {
      & .datatable-header-cell {
        padding: $table-cell-padding-y $table-cell-padding-x;
        font-weight: 600;
        border-bottom: 1px solid $table-border-color;
        vertical-align: top;
        line-height: inherit;

        [data-bs-theme="dark"] & {
          background: var(--#{$prefix}component-bg);
          color: var(--#{$prefix}component-color);
          border-bottom-color: rgba($white, .15);
        }
      }
    }
  }

  // ── Body: filas ─────────────────────────────────────
  & .datatable-body {
    & .datatable-body-row {
      border-top: none;

      &.datatable-row-even {
        background: $table-striped-bg;            // light mode

        [data-bs-theme="dark"] & {
          background: rgba($white, .05);          // dark: overlay sutil
        }
      }

      &.datatable-row-odd {
        [data-bs-theme="dark"] & {
          background: var(--#{$prefix}component-bg);
        }
      }

      & .datatable-row-center {
        & .datatable-body-cell {
          padding: $table-cell-padding-y $table-cell-padding-x;
          vertical-align: top;
          border-top: none;
          border-bottom: 1px solid $table-border-color;
          line-height: inherit;

          [data-bs-theme="dark"] & {
            border-bottom-color: rgba($white, .15);
            color: var(--#{$prefix}component-color);
          }
        }
      }
    }
  }

  // ── Footer (paginación + totales) ───────────────────
  & .datatable-footer {
    background: none;         // anula el #424242 hardcodeado de bootstrap.css
    color: inherit;
    margin-top: rem(-3px);
    padding: 0 $spacer;

    [data-bs-theme="dark"] & {
      border-top: 1px solid rgba($white, .15);
      color: var(--#{$prefix}component-color);
    }

    & .datatable-footer-inner {
      [data-bs-theme="dark"] & { color: var(--#{$prefix}component-color); }

      & .page-count {
        [data-bs-theme="dark"] & { color: var(--#{$prefix}component-color); }
      }
    }

    & .datatable-pager {
      & ul li {
        & a {
          [data-bs-theme="dark"] & {
            color: var(--#{$prefix}component-color);
            background: var(--#{$prefix}component-bg);
            border-color: rgba($white, .2);
          }
          &:hover {
            [data-bs-theme="dark"] & {
              color: $white;
              background: rgba($white, .15) !important;
              border-color: rgba($white, .35);
            }
          }
        }
        &.disabled a {
          [data-bs-theme="dark"] & {
            color: rgba($white, .3);
            border-color: rgba($white, .1);
            background: transparent;
          }
        }
      }
    }
  }

  // ── Empty row ────────────────────────────────────────
  & .empty-row {
    padding: $spacer;
    border-bottom: 1px solid $border-color;

    [data-bs-theme="dark"] & {
      border-bottom-color: rgba($white, .15);
      color: var(--#{$prefix}component-color);
    }
  }
}
```

---

## 3. Anti-patrón frecuente: `::ng-deep` con colores hardcodeados

```scss
/* ❌ Incorrecto — colores fijos rompen dark mode */
::ng-deep .ngx-datatable.bootstrap {
  .datatable-header { background-color: #1a2229; color: #fff; }
  .datatable-footer { background-color: #f8f9fa; color: #555; }
  .empty-row { color: #aaa; }
}
```

**Solución**: reemplazar por variables CSS:

```scss
/* ✅ Correcto — responde a data-bs-theme automáticamente */
::ng-deep .ngx-datatable.bootstrap {
  .datatable-header,
  .datatable-header-cell {
    background-color: var(--bs-component-bg);
    color: var(--bs-component-color);
  }
  .datatable-footer {
    background-color: var(--bs-component-bg);
    border-top: 1px solid var(--bs-component-border-color);
    color: var(--bs-component-color);
  }
  .datatable-body-row:hover .datatable-body-cell {
    background-color: rgba(0, 0, 0, 0.065);

    [data-bs-theme="dark"] & {
      background-color: rgba(255, 255, 255, 0.08);
    }
  }
  .empty-row { color: var(--bs-secondary-color); }
}
```

> **Buscar overrides de componente antes de depurar dark mode:**
> ```bash
> grep -rn "::ng-deep.*ngx-datatable\|datatable-footer\|datatable-header" src/app --include="*.scss"
> ```

---

## 4. NO importar `themes/bootstrap.css` de ngx-datatable

El paquete incluye estilos con colores hardcodeados que rompen el dark mode:

```css
/* bootstrap.css rompe dark mode */
.ngx-datatable.bootstrap .datatable-footer {
  background: #424242;   /* siempre oscuro */
  color: #ededed;
}
```

Verificar que **no** está en `angular.json` ni en ningún `@import`:

```bash
grep -rn "themes/bootstrap\|swimlane/ngx-datatable/themes" src/ --include="*.scss" --include="*.css"
# → debe devolver VACÍO
```

---

## 5. Áreas inline (beforeBody / row-detail)

Para zonas con fondo personalizado usar variables CSS inline, **no** clases Bootstrap como `bg-light`:

```html
<!-- ✅ Correcto -->
<div beforeBody style="background: var(--bs-component-bg); border-bottom: 1px solid var(--bs-component-border-color);">
  ...
</div>
```

---

## Variables CSS usadas

| Variable | Significado |
|---|---|
| `var(--bs-component-bg)` | Fondo del componente (blanco en light, oscuro en dark) |
| `var(--bs-component-color)` | Color de texto |
| `var(--bs-component-border-color)` | Borde del componente |
| `rgba($white, .05)` | Fondo sutil para filas pares en dark |
| `rgba($white, .15)` | Borde sutil en dark |
