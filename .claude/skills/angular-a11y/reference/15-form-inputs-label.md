# Axe forms: Form elements must have labels

## Regla

`axe/forms` — WCAG 1.3.1 y 4.1.2  
Todo `<input>`, `<select>` y `<textarea>` debe tener un nombre accesible.
MDN: *"When including inputs, it is an accessibility requirement to add labels alongside."*

## Anti-patrones

```html
<!-- MAL: label vacío — existe la asociación for/id pero no hay texto -->
<input type="checkbox" id="appDarkMode" />
<label for="appDarkMode"></label>

<!-- MAL: placeholder como sustituto de label (desaparece al escribir) -->
<input type="text" placeholder="Nombre" />

<!-- MAL: title como label (no es reconocido como nombre accesible primario) -->
<input type="text" title="Nombre" />
```

## Metodos correctos (orden de preferencia)

### 1. `<label>` con texto visible (mejor práctica, preferida por MDN)

```html
<label for="username">Username</label>
<input type="text" id="username" />

<!-- o implícito: input dentro del label -->
<label>Username <input type="text" /></label>
```

### 2. `aria-label` — cuando el texto visible está en otro elemento

Cuando el texto descriptivo ya existe visualmente pero en un elemento hermano
(ej. Bootstrap form-switch donde el `<label>` es el slider visual y el texto
está en una columna adyacente):

```html
<input type="checkbox" id="appDarkMode" aria-label="Dark Mode" />
<label class="form-check-label" for="appDarkMode"></label>
```

### 3. `aria-labelledby` — apunta al elemento que ya contiene el texto

```html
<div id="darkModeLabel">Dark Mode</div>
<input type="checkbox" id="appDarkMode" aria-labelledby="darkModeLabel" />
```

## Caso Bootstrap form-switch

Bootstrap usa `<label class="form-check-label">` como el slider visual del toggle.
Si el texto descriptivo está en un elemento hermano (no en el `<label>`), el label
queda vacío y axe lo marca como sin nombre accesible.

**Solución:** `aria-label` en el `<input>` con el mismo texto que ya se muestra visualmente.

```html
<!-- Bootstrap form-switch con texto en columna adyacente -->
<div class="row">
  <div class="col-8">Dark Mode</div>       <!-- texto visible -->
  <div class="col-4">
    <div class="form-check form-switch">
      <input type="checkbox" id="appThemeDarkMode"
             aria-label="Dark Mode" />     <!-- nombre accesible explícito -->
      <label class="form-check-label" for="appThemeDarkMode"></label>
    </div>
  </div>
</div>
```

## Aplicacion en este repo (theme-panel.component.html)

| Input id | aria-label aplicado |
|---|---|
| appThemeDarkMode | `aria-label="Dark Mode"` |
| appHeaderFixed | `aria-label="Header Fixed"` |
| appHeaderInverse | `aria-label="Header Inverse"` |
| appSidebarFixed | `aria-label="Sidebar Fixed"` |
| appSidebarGrid | `aria-label="Sidebar Grid"` |
| appGradientEnabled | `aria-label="Gradient Enabled"` |
| appRtlEnabled | `aria-label="RTL Enabled"` |

## Referencias

- MDN `<input>` — accessibility:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/input
- axe rule label:
  https://dequeuniversity.com/rules/axe/4.11/label
- WCAG 1.3.1 Info and Relationships:
  https://www.w3.org/WAI/WCAG21/Understanding/info-and-relationships.html
