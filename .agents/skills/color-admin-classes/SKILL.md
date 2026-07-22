````skill
# Color Admin - Clases CSS Predefinidas

Referencia completa de todas las clases CSS utilitarias disponibles en el tema Color Admin (Bootstrap-based). Estas clases sobreescriben los estilos CSS definidos en tus propias clases, a menos que uses `!important`.

## When to Use

Use this skill when:
- Necesitas aplicar estilos rápidos sin escribir CSS personalizado
- Buscas la clase correcta para márgenes, padding, colores, tipografía o layout
- Quieres saber qué variantes de color están disponibles en Color Admin
- Necesitas clases para flexbox, posicionamiento, bordes o sombras
- Buscas clases de ancho/alto fijo en píxeles o porcentaje

---

## 1. General

### Row Space (Gutter)
```html
.row.gx-1   <!-- gutter horizontal mínimo -->
.row.gx-2
.row.gx-3
.row.gx-4
.row.gx-5   <!-- gutter horizontal máximo -->
```

### Table Utilities
```html
.align-baseline
.align-top
.align-middle
.align-bottom
.align-text-top
.align-text-bottom
.table-thead-sticky        <!-- encabezado fijo -->
.table-tfoot-sticky        <!-- pie fijo -->
.table-thead-bordered
.table-tbody-bordered
.table-tfoot-bordered
.table-px-{1-20}px         <!-- padding horizontal de celdas -->
.table-py-{1-20}px         <!-- padding vertical de celdas -->
```

### Float
```html
.float-start
.float-end
.float-none
```

### Border Radius
```html
.rounded-0
.rounded-1
.rounded-2
.rounded-3
.rounded-top
.rounded-end
.rounded-bottom
.rounded-start
.rounded-circle
.rounded-pill
```

### Display
```html
.d-none
.d-inline
.d-inline-block
.d-block
.d-grid
.d-table
.d-table-cell
.d-table-row
.d-flex
.d-inline-flex
```

### Overflow
```html
.overflow-auto
.overflow-hidden
.overflow-visible
.overflow-scroll
```

### Visibility
```html
.visible
.invisible
```

### Shadows
```html
.shadow-none
.shadow-sm
.shadow
.shadow-lg
```

---

## 2. Flexbox

```html
/* Dirección */
.flex-row
.flex-row-reverse
.flex-column
.flex-column-reverse

/* Justify Content */
.justify-content-start
.justify-content-end
.justify-content-center
.justify-content-between
.justify-content-around
.justify-content-evenly

/* Align Items */
.align-items-start
.align-items-end
.align-items-center
.align-items-baseline
.align-items-stretch

/* Align Self */
.align-self-start
.align-self-end
.align-self-center
.align-self-baseline
.align-self-stretch

/* Grow / Shrink */
.flex-grow-1
.flex-grow-0
.flex-shrink-1
.flex-shrink-0

/* Wrap */
.flex-nowrap
.flex-wrap
.flex-wrap-reverse

/* Order */
.order-{1|2|3|4|5}
```

---

## 3. Borders

```html
/* Agregar bordes */
.border
.border-top
.border-end
.border-bottom
.border-start

/* Quitar bordes */
.border-0
.border-top-0
.border-end-0
.border-bottom-0
.border-start-0

/* Grosor */
.border-1
.border-2
.border-3
.border-4
.border-5

/* Color de borde */
.border-primary
.border-secondary
.border-success
.border-danger
.border-warning
.border-info
.border-light
.border-dark
.border-theme
.border-white
```

---

## 4. Position

```html
/* Tipo de posición */
.position-static
.position-relative
.position-absolute
.position-fixed
.position-sticky

/* Coordenadas */
.top-0      .top-50      .top-100
.end-0      .end-50      .end-100
.bottom-0   .bottom-50   .bottom-100
.start-0    .start-50    .start-100

/* Transform helpers */
.translate-middle
.translate-middle-x
.translate-middle-y
```

---

## 5. Interactions (Pointer & Select)

```html
.user-select-all
.user-select-auto
.user-select-none
.pe-none     <!-- pointer-events: none -->
.pe-auto     <!-- pointer-events: auto -->
```

---

## 6. Width & Height

### Ancho (Width)
```html
/* Porcentaje */
.w-100   .w-75   .w-50   .w-25   .w-auto
.vw-100  .min-vw-100
.mw-75   .mw-50  .mw-25   <!-- max-width -->

/* Píxeles pequeños (5px a 50px de 5 en 5) */
.w-5px   .w-10px  .w-15px  .w-20px  .w-25px
.w-30px  .w-35px  .w-40px  .w-45px  .w-50px

/* Píxeles grandes (100px a 600px de 50 en 50) */
.w-100px  .w-150px  .w-200px  .w-250px  .w-300px
.w-350px  .w-400px  .w-450px  .w-500px  .w-550px  .w-600px
```

### Alto (Height)
```html
/* Porcentaje */
.h-100   .h-75   .h-50   .h-25   .h-auto
.vh-100  .min-vh-100
.mh-75   .mh-50  .mh-25   <!-- max-height -->

/* Píxeles pequeños (5px a 50px de 5 en 5) */
.h-5px   .h-10px  .h-15px  .h-20px  .h-25px
.h-30px  .h-35px  .h-40px  .h-45px  .h-50px

/* Píxeles grandes (100px a 600px de 50 en 50) */
.h-100px  .h-150px  .h-200px  .h-250px  .h-300px
.h-350px  .h-400px  .h-450px  .h-500px  .h-550px  .h-600px
```

---

## 7. Text & Font

### Font Size
```html
/* Semánticos Bootstrap */
.fs-1  .fs-2  .fs-3  .fs-4  .fs-5  .fs-6

/* Píxeles (1px a 80px) */
.fs-1px  .fs-2px  ...  .fs-80px
```

### Font Weight
```html
.fw-bold
.fw-bolder
.fw-normal
.fw-light
.fw-lighter
.fw-100  .fw-200  .fw-300  .fw-400  .fw-500  .fw-600  .fw-700  .fw-800
```

### Text Align
```html
.text-center
.text-start
.text-end
```

### Text Overflow
```html
.text-wrap
.text-nowrap
.text-ellipsis
```

### Line Height
```html
.lh-1
.lh-sm
.lh-base
.lh-lg
```

### Italics
```html
.fst-italic
.fst-normal
```

### Text Decoration
```html
.text-decoration-underline
.text-decoration-line-through
.text-decoration-none
```

### Reset Color
```html
.reset-link   <!-- quita el color heredado de un enlace -->
```

### Text Transform
```html
.text-lowercase
.text-uppercase
.text-capitalize
```

### Word Break
```html
.text-break
```

### Monospace
```html
.font-monospace
```

---

## 8. Margin

> Patrón: `m{lado}-{tamaño}`  
> Lados: vacío (todos), `t` (top), `e` (end/right), `b` (bottom), `s` (start/left)

```html
/* Bootstrap estándar (0-5) */
.m-0   .m-1   .m-2   .m-3   .m-4   .m-5   .m-auto
.mt-0  .mt-1  .mt-2  .mt-3  .mt-4  .mt-5  .mt-auto
.me-0  .me-1  .me-2  .me-3  .me-4  .me-5  .me-auto
.mb-0  .mb-1  .mb-2  .mb-3  .mb-4  .mb-5  .mb-auto
.ms-0  .ms-1  .ms-2  .ms-3  .ms-4  .ms-5  .ms-auto

/* Píxeles exactos 1px a 10px */
.m-1px   .m-2px  ...  .m-10px
.mt-1px  ...  .mt-10px
.me-1px  ...  .me-10px
.mb-1px  ...  .mb-10px
.ms-1px  ...  .ms-10px

/* Píxeles exactos 15px a 50px (de 5 en 5) */
.m-15px   .m-20px   .m-25px   .m-30px   .m-35px   .m-40px   .m-45px   .m-50px
.mt-15px  ...  .mt-50px
.me-15px  ...  .me-50px
.mb-15px  ...  .mb-50px
.ms-15px  ...  .ms-50px
```

---

## 9. Padding

> Mismo patrón que Margin pero con `p` en lugar de `m`

```html
/* Bootstrap estándar (0-5) */
.p-0   .p-1   .p-2   .p-3   .p-4   .p-5   .p-auto
.pt-0  .pt-1  .pt-2  .pt-3  .pt-4  .pt-5  .pt-auto
.pe-0  .pe-1  .pe-2  .pe-3  .pe-4  .pe-5  .pe-auto
.pb-0  .pb-1  .pb-2  .pb-3  .pb-4  .pb-5  .pb-auto
.ps-0  .ps-1  .ps-2  .ps-3  .ps-4  .ps-5  .ps-auto

/* Píxeles exactos 1px a 10px */
.p-1px   .p-2px  ...  .p-10px
.pt-1px  ...  .pt-10px
.pe-1px  ...  .pe-10px
.pb-1px  ...  .pb-10px
.ps-1px  ...  .ps-10px

/* Píxeles exactos 15px a 50px (de 5 en 5) */
.p-15px  ...  .p-50px
.pt-15px ...  .pt-50px
.pe-15px ...  .pe-50px
.pb-15px ...  .pb-50px
.ps-15px ...  .ps-50px
```

---

## 10. Background Colors

> Patrón: `.bg-{color}-{tono}` donde tono va de 100 a 900  
> Alias corto: `.bg-{color}` equivale a `.bg-{color}-500`

### Colores disponibles
```
blue | indigo | purple | cyan (aqua) | teal
green | lime | orange | yellow | red
pink | black | gray | silver | white
```

### Ejemplos
```html
<!-- Azul -->
.bg-blue-100  .bg-blue-200  .bg-blue-300  .bg-blue-400
.bg-blue-500  <!-- = .bg-blue -->
.bg-blue-600  .bg-blue-700  .bg-blue-800  .bg-blue-900
.bg-gradient-blue

<!-- Mismo patrón para todos los colores -->
.bg-{color}-100 ... .bg-{color}-900
.bg-gradient-{color}
```

### Colores extra
```html
.bg-none         <!-- sin fondo -->
.bg-transparent  <!-- transparente -->
.bg-theme        <!-- color del tema activo -->
```

### Gradientes personalizados (dos colores)
```html
.bg-gradient-red-pink
.bg-gradient-orange-red
.bg-gradient-yellow-orange
.bg-gradient-yellow-red
.bg-gradient-teal-green
.bg-gradient-yellow-green
.bg-gradient-blue-purple
.bg-gradient-cyan-blue
.bg-gradient-cyan-purple
.bg-gradient-cyan-indigo
.bg-gradient-blue-indigo
.bg-gradient-purple-indigo
.bg-gradient-silver-black
```

### Utilidades de gradiente (dirección)
```html
/* Dirección cardinal */
.bg-gradient-to-r    <!-- izquierda → derecha -->
.bg-gradient-to-l    <!-- derecha → izquierda -->
.bg-gradient-to-t    <!-- abajo → arriba -->
.bg-gradient-to-b    <!-- arriba → abajo -->

/* Diagonal */
.bg-gradient-to-tl
.bg-gradient-to-tr
.bg-gradient-to-bl
.bg-gradient-to-br

/* Ángulo fijo */
.bg-gradient-to-45
.bg-gradient-135

/* Especiales */
.bg-gradient-to-radial
.bg-gradient-to-conic

/* Definir colores de inicio/fin (cualquier color Bootstrap) */
.bg-gradient-from-{color}
.bg-gradient-to-{color}
```

### Blur de fondo
```html
.bg-blur-1
.bg-blur-2
.bg-blur-3
```

---

## 11. Text Colors

> Patrón: `.text-{color}-{tono}` donde tono va de 100 a 900  
> Alias corto: `.text-{color}` equivale a `.text-{color}-500`

### Colores disponibles
```
blue | indigo | purple | cyan | teal
green | lime | orange | yellow | red
pink | black | gray | silver | white
```

### Ejemplos
```html
.text-blue-100  .text-blue-200  ...  .text-blue-900
.text-blue      <!-- = .text-blue-500 -->

<!-- Mismo patrón para todos los colores -->
.text-{color}-100 ... .text-{color}-900
.text-{color}
```

### Colores especiales
```html
.text-theme        <!-- color del tema activo -->
.text-theme-color  <!-- variante del color del tema -->
```

### Texto con gradiente
```html
/* Combinar con una clase de gradiente de fondo */
.text-gradient .bg-gradient-orange-red
.text-gradient .bg-gradient-blue-indigo
.text-gradient .bg-gradient-black

/* Con clase de dirección personalizada */
.text-gradient .bg-gradient-to-r .bg-gradient-from-teal .bg-gradient-to-blue
```

---

## Quick Reference - Combinaciones Comunes

```html
<!-- Centrar horizontalmente con flex -->
<div class="d-flex justify-content-center align-items-center">...</div>

<!-- Badge de color -->
<span class="bg-blue-500 text-white rounded-pill px-2 py-1 fs-12px fw-600">...</span>

<!-- Sección con fondo degradado -->
<div class="bg-gradient-blue-indigo p-3 rounded-3 text-white">...</div>

<!-- Texto con gradiente -->
<h1 class="text-gradient bg-gradient-orange-red fw-bold fs-3">Título</h1>

<!-- Contenedor de ancho fijo centrado -->
<div class="w-500px mx-auto">...</div>

<!-- Elemento con sombra y borde redondeado -->
<div class="shadow rounded-3 p-3">...</div>

<!-- Ícono con color y tamaño -->
<i class="fa fa-home text-blue-500 fs-20px"></i>

<!-- Texto truncado con ellipsis -->
<span class="text-ellipsis text-nowrap d-block w-200px">Texto largo...</span>
```
````
