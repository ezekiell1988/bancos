---
name: color-admin-ngx-datatable-darkmode
description: >
  Guía definitiva para hacer que ngx-datatable funcione correctamente con el sistema de
  dark mode de Color Admin en Angular. Cubre el problema del selector incorrecto (.dark-mode vs
  data-bs-theme), los overrides SCSS necesarios, y las variables CSS para filas, bordes, header
  y footer. Usar SIEMPRE que el dark mode no funcione en una tabla ngx-datatable dentro de
  un proyecto Color Admin.
---

# Dark Mode en ngx-datatable con Color Admin

## El Problema Raíz

Color Admin activa el dark mode poniendo `data-bs-theme="dark"` en `<html>`:

```typescript
// desktop-layout.component.ts
if (this.appSettings.appDarkMode) {
  document.documentElement.setAttribute("data-bs-theme", "dark");
} else {
  document.documentElement.removeAttribute("data-bs-theme");
}
```

El selector SCSS correcto es **`[data-bs-theme="dark"] &`**, NO `.dark-mode &`.

> **NUNCA** usar `.dark-mode &` en este proyecto — esa clase no existe en el DOM.

---

## 1. Dónde poner los overrides

En `src/scss/angular.scss`, dentro (o justo después) del bloque `.ngx-datatable.bootstrap { ... }`.

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

        // CORRECTO: data-bs-theme, no .dark-mode
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

      // Filas pares: fondo rayado
      &.datatable-row-even {
        background: $table-striped-bg;            // light mode

        [data-bs-theme="dark"] & {
          background: rgba($white, .05);          // dark mode: overlay claro sutil
        }
      }

      // Filas impares: fondo sólido del componente
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
    background: none;         // anula el #424242 hardcodeado del bootstrap.css
    color: inherit;
    margin-top: rem(-3px);
    padding: 0 $spacer;

    [data-bs-theme="dark"] & {
      border-top: 1px solid rgba($white, .15);
      color: var(--#{$prefix}component-color);
    }

    // Texto "N total" (page-count)
    & .datatable-footer-inner {
      [data-bs-theme="dark"] & { color: var(--#{$prefix}component-color); }

      & .page-count {
        [data-bs-theme="dark"] & { color: var(--#{$prefix}component-color); }
      }
    }

    & .datatable-pager {
      & ul li {
        & a {
          // dark mode: botones de paginación
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

## 3. ⚠️ Cuidado con overrides en SCSS de componente

Un problema frecuente es tener un bloque `::ng-deep .ngx-datatable.bootstrap { ... }` en el
SCSS del propio componente (p.ej. `clients-config.scss`) con colores hardcodeados que
**sobreescriben** los de `angular.scss`:

```scss
/* ❌ Incorrecto — colores hardcodeados rompen dark mode */
::ng-deep .ngx-datatable.bootstrap {
  .datatable-header {
    background-color: #1a2229;   // fijo oscuro
    color: #fff;
  }
  .datatable-footer {
    background-color: #f8f9fa;   // fijo claro — visible en dark mode
    color: #555;
  }
  .empty-row {
    color: #aaa;
  }
}
```

**Solución**: reemplazar todos los valores hardcodeados por variables CSS:

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

    .page-count {
      color: var(--bs-component-color);
    }
  }
  .datatable-body-row:hover .datatable-body-cell {
    background-color: rgba(0, 0, 0, 0.065);   // light

    [data-bs-theme="dark"] & {
      background-color: rgba(255, 255, 255, 0.08);
    }
  }
  .empty-row {
    color: var(--bs-secondary-color);
  }
}
```

> **Siempre buscar** overrides de componente antes de depurar dark mode:
> ```bash
> grep -rn "::ng-deep.*ngx-datatable\|datatable-footer\|datatable-header" src/app \
>   --include="*.scss" | grep -v node_modules
> ```

---

## 4. NO importar bootstrap.css de ngx-datatable

El paquete `@swimlane/ngx-datatable` incluye `themes/bootstrap.css` con colores hardcodeados:

```css
/* ❌ Estos valores rompen el dark mode */
.ngx-datatable.bootstrap .datatable-body-row.datatable-row-even {
  background-color: rgba(0, 0, 0, 0.05);
}
.ngx-datatable.bootstrap .datatable-footer {
  background: #424242;   /* siempre oscuro, incluso en light mode */
  color: #ededed;
}
```

**Verificar** que `themes/bootstrap.css` NO está en `angular.json` ni en ningún `@import`:

```bash
grep -rn "themes/bootstrap\|swimlane/ngx-datatable/themes" src/ --include="*.scss" --include="*.css"
# → debe devolver VACÍO
```

Si existe, **eliminarlo**. Los estilos de `angular.scss` son suficientes.

---

## 4. Variables CSS que se usan

| Variable | Significado |
|---|---|
| `var(--bs-component-bg)` | Fondo del panel/componente (blanco en light, oscuro en dark) |
| `var(--bs-component-color)` | Color de texto del componente |
| `var(--bs-component-border-color)` | Borde del componente |
| `rgba($white, .05)` | Fondo sutil para filas pares en dark |
| `rgba($white, .15)` | Borde sutil en dark |

Estas variables las redefine Color Admin automáticamente cuando se activa `data-bs-theme="dark"`.

---

## 5. Áreas inline del HTML (beforeBody / row-detail)

Para las zonas con fondo personalizado (barra de herramientas, row-detail de productos), usar
variables CSS inline — **no** clases Bootstrap como `bg-light`:

```html
<!-- ✅ Correcto: variables CSS que responden a data-bs-theme -->
<div beforeBody style="background: var(--bs-component-bg); border-bottom: 1px solid var(--bs-component-border-color);">
  ...
</div>

<div #outsideBody>
  <ngx-datatable ...>
    <ngx-datatable-row-detail rowHeight="auto">
      <ng-template let-row="row" ngx-datatable-row-detail-template>
        <div style="background: var(--bs-component-bg); border-top: 1px solid var(--bs-component-border-color);">
          ...
        </div>
      </ng-template>
    </ngx-datatable-row-detail>
    ...
  </ngx-datatable>
</div>
```

```html
<!-- ❌ Incorrecto: bg-light es blanco fijo, no cambia con dark mode -->
<div beforeBody class="bg-light border-bottom">...</div>
```

---

## 6. Cómo verificar que funciona

1. Compilar SCSS sin errores:
   ```bash
   npx sass src/scss/angular.scss /dev/null --no-source-map \
     --load-path=node_modules \
     --load-path=src/scss/apple
   # → sin output = sin errores
   ```

2. En el navegador, activar dark mode desde el panel de temas.

3. Inspeccionar una fila par (`datatable-row-even`): su `background` debe cambiar de
   `rgba(0,0,0,0.05)` a `rgba(255,255,255,0.05)`.

4. El header (`.datatable-header-cell`) debe tener el mismo fondo oscuro que el panel.

5. El footer no debe tener el fondo `#424242` en light mode.

---

## 8. Checklist de corrección

- [ ] Todos los selectores de dark mode son `[data-bs-theme="dark"] &`
- [ ] No existe ningún `.dark-mode &` en los bloques de ngx-datatable
- [ ] `themes/bootstrap.css` de `@swimlane/ngx-datatable` NO está importado
- [ ] Áreas inline (`beforeBody`, row-detail) usan `var(--bs-component-bg)`
- [ ] **No hay `::ng-deep .ngx-datatable` en SCSS de componente con colores hardcodeados**
- [ ] SCSS compila sin errores con `npx sass`
