# Reference: Helper CSS — Clases CSS predefinidas

Página de referencia de todas las clases CSS predefinidas de Color Admin, organizadas en 7 categorías mediante nav-tabs.

## Uso básico (estructura de tabs)

```html
<ul class="nav nav-tabs nav-tabs-inverse">
  <li class="nav-item"><a href="#general-tab" data-bs-toggle="tab" class="nav-link active">General</a></li>
  <li class="nav-item"><a href="#width-height-tab" data-bs-toggle="tab" class="nav-link">Width & Height</a></li>
  <!-- más tabs... -->
</ul>
<div class="tab-content rounded-bottom p-3 card rounded-0 border-0">
  <div class="tab-pane fade active show" id="general-tab">...</div>
  <div class="tab-pane fade" id="width-height-tab">...</div>
</div>
```

---

## Tab 1 — General

### Row Space (gutter)
`.row.gx-1` `.row.gx-2` `.row.gx-3` `.row.gx-4` `.row.gx-5`

### Table utilities
`.align-baseline` `.align-top` `.align-middle` `.align-bottom` `.align-text-top` `.align-text-bottom`
`.table-thead-sticky` `.table-tfoot-sticky` `.table-thead-bordered` `.table-tbody-bordered` `.table-tfoot-bordered`
`.table-px-{1-20}px` `.table-py-{1-20}px`

### Float
`.float-start` `.float-end` `.float-none`

### Border Radius
`.rounded-0` `.rounded-1` `.rounded-2` `.rounded-3`
`.rounded-top` `.rounded-end` `.rounded-bottom` `.rounded-start`
`.rounded-circle` `.rounded-pill`

### Display
`.d-none` `.d-inline` `.d-inline-block` `.d-block` `.d-grid`
`.d-table` `.d-table-cell` `.d-table-row` `.d-flex` `.d-inline-flex`

### Overflow
`.overflow-auto` `.overflow-hidden` `.overflow-visible` `.overflow-scroll`

### Flex
`.flex-row` `.flex-row-reverse` `.flex-column` `.flex-column-reverse`
`.justify-content-start` `.justify-content-end` `.justify-content-center` `.justify-content-between` `.justify-content-around` `.justify-content-evenly`
`.align-items-start` `.align-items-end` `.align-items-center` `.align-items-baseline` `.align-items-stretch`
`.align-self-start` `.align-self-end` `.align-self-center` `.align-self-baseline` `.align-self-stretch`
`.flex-grow-1` `.flex-grow-0` `.flex-shrink-1` `.flex-shrink-0`
`.flex-nowrap` `.flex-wrap` `.flex-wrap-reverse`
`.order-{1|2|3|4|5}`

### Borders
`.border` `.border-top` `.border-end` `.border-bottom` `.border-start`
`.border-0` `.border-top-0` `.border-end-0` `.border-bottom-0` `.border-start-0`
`.border-1` `.border-2` `.border-3` `.border-4` `.border-5`
`.border-primary` `.border-secondary` `.border-success` `.border-danger` `.border-warning` `.border-info` `.border-light` `.border-dark` `.border-theme` `.border-white`

### Position
`.position-static` `.position-relative` `.position-absolute` `.position-fixed` `.position-sticky`
`.top-0` `.top-50` `.top-100`
`.end-0` `.end-50` `.end-100`
`.bottom-0` `.bottom-50` `.bottom-100`
`.start-0` `.start-50` `.start-100`
`.translate-middle` `.translate-middle-x` `.translate-middle-y`

### Interactions
`.user-select-all` `.user-select-auto` `.user-select-none`
`.pe-none` `.pe-auto`

### Shadows
`.shadow-none` `.shadow-sm` `.shadow` `.shadow-lg`

### Visibility
`.visible` `.invisible`

---

## Tab 2 — Width & Height

### Width (porcentaje)
`.w-100` `.w-75` `.w-50` `.w-25` `.w-auto`
`.vw-100` `.min-vw-100`
`.mw-75` `.mw-50` `.mw-25`

### Width (píxeles, pasos cortos)
`.w-5px` `.w-10px` `.w-15px` `.w-20px` `.w-25px` `.w-30px` `.w-35px` `.w-40px` `.w-45px` `.w-50px`

### Width (píxeles, pasos largos)
`.w-100px` `.w-150px` `.w-200px` `.w-250px` `.w-300px` `.w-350px` `.w-400px` `.w-450px` `.w-500px` `.w-550px` `.w-600px`

### Height (porcentaje)
`.h-100` `.h-75` `.h-50` `.h-25` `.h-auto`
`.vh-100` `.min-vh-100`
`.mh-75` `.mh-50` `.mh-25`

### Height (píxeles, pasos cortos)
`.h-5px` `.h-10px` `.h-15px` `.h-20px` `.h-25px` `.h-30px` `.h-35px` `.h-40px` `.h-45px` `.h-50px`

### Height (píxeles, pasos largos)
`.h-100px` `.h-150px` `.h-200px` `.h-250px` `.h-300px` `.h-350px` `.h-400px` `.h-450px` `.h-500px` `.h-550px` `.h-600px`

---

## Tab 3 — Text & Font

### Font Size
`.fs-1` `.fs-2` `.fs-3` `.fs-4` `.fs-5` `.fs-6`
`.fs-{1px, 2px... to 80px}`

### Font Weight
`.fw-bold` `.fw-bolder` `.fw-normal` `.fw-light` `.fw-lighter`
`.fw-{100 to 800}`

### Text Align
`.text-center` `.text-start` `.text-end`

### Text Overflow
`.text-wrap` `.text-nowrap` `.text-ellipsis`

### Line Height
`.lh-1` `.lh-sm` `.lh-base` `.lh-lg`

### Italics
`.fst-italic` `.fst-normal`

### Text Decoration
`.text-decoration-underline` `.text-decoration-line-through` `.text-decoration-none`

### Reset Color
`.reset-link`

### Text Transform
`.text-lowercase` `.text-uppercase` `.text-capitalize`

### Word Break
`.text-break`

### Monospace
`.font-monospace`

---

## Tab 4 — Margin

Patrón: `m`, `mt`, `me`, `mb`, `ms` (todos disponibles con los mismos valores).

### Valores estándar Bootstrap
`.m-0` `.m-1` `.m-2` `.m-3` `.m-4` `.m-5` `.m-auto`

### Valores en píxeles (Color Admin extras)
`.m-{1px, 2px... to 10px}` `.m-{15px, 20px... to 50px}`

Aplica igualmente con prefijos direccionales:
- Top → `.mt-*`
- End (right) → `.me-*`
- Bottom → `.mb-*`
- Start (left) → `.ms-*`

---

## Tab 5 — Padding

Mismo patrón que Margin con prefijos `p`, `pt`, `pe`, `pb`, `ps`.

### Valores estándar Bootstrap
`.p-0` `.p-1` `.p-2` `.p-3` `.p-4` `.p-5` `.p-auto`

### Valores en píxeles (Color Admin extras)
`.p-{1px, 2px... to 10px}` `.p-{15px, 20px... to 50px}`

---

## Tab 6 — Background Color

### Colores con variantes 100–900 + gradiente
Patrón: `.bg-{color}-{100|200|300|400|500|600|700|800|900}` y `.bg-gradient-{color}`  
El tono 500 también acepta el alias corto: `.bg-{color}`

Colores disponibles:
`blue` `indigo` `purple` `cyan` `teal` `green` `lime` `orange` `yellow` `red` `pink` `black` `gray` `silver` `white`

```html
<!-- Ejemplo: fondo azul medio -->
<div class="bg-blue"></div>
<!-- Fondo azul claro -->
<div class="bg-blue-100"></div>
<!-- Gradiente azul -->
<div class="bg-gradient-blue"></div>
```

### Extra
`.bg-none` `.bg-transparent` `.bg-theme`

### Custom Gradients (combinaciones)
`.bg-gradient-red-pink` `.bg-gradient-orange-red` `.bg-gradient-yellow-orange` `.bg-gradient-yellow-red`
`.bg-gradient-teal-green` `.bg-gradient-yellow-green` `.bg-gradient-blue-purple`
`.bg-gradient-cyan-blue` `.bg-gradient-cyan-purple` `.bg-gradient-cyan-indigo`
`.bg-gradient-blue-indigo` `.bg-gradient-purple-indigo` `.bg-gradient-silver-black`

### Background Utilities (dirección de gradiente)
`.bg-gradient-to-r` `.bg-gradient-to-l` `.bg-gradient-to-t` `.bg-gradient-to-b`
`.bg-gradient-to-tl` `.bg-gradient-to-tr` `.bg-gradient-to-bl` `.bg-gradient-to-br`
`.bg-gradient-to-radial` `.bg-gradient-to-conic` `.bg-gradient-to-45` `.bg-gradient-135`
`.bg-gradient-from-{any bootstrap color}` `.bg-gradient-to-{any bootstrap color}`

### Blur
`.bg-blur-1` `.bg-blur-2` `.bg-blur-3`

---

## Tab 7 — Text Color

### Colores con variantes 100–900
Patrón: `.text-{color}-{100|200|...|900}` y alias corto `.text-{color}` (= 500)

Colores disponibles:
`blue` `indigo` `purple` `cyan` `teal` `green` `lime` `orange` `yellow` `red` `pink` `black` `gray` `silver` `white`

```html
<span class="text-blue">azul</span>
<span class="text-red-300">rojo claro</span>
<span class="text-green-700">verde oscuro</span>
```

---

## Notas

- Todas las clases predefinidas usan `!important` internamente, por lo que sobreescriben cualquier estilo definido en tus clases a menos que también declares `!important` en tu CSS.
- `.table-condensed` es una clase de Color Admin (no Bootstrap 5 nativo) que reduce el padding de celdas.
- `.bg-none` elimina el fondo definitivamente; `.bg-transparent` aplica `background-color: transparent`.
- Los aliases cortos como `.bg-blue` / `.text-blue` equivalen siempre a la variante `-500`.
- Las width/height en px de Color Admin van más allá de las Bootstrap utilities estándar (`.w-50px`, `.h-300px`, etc.).
