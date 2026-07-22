# Complete Utility Classes Reference

Referencia completa de todas las clases de utilidad disponibles en ambas plataformas.

## Table of Contents

- [Spacing Reference](#spacing-reference)
- [Typography Reference](#typography-reference)
- [Layout Reference](#layout-reference)
- [Color Reference](#color-reference)
- [Border Reference](#border-reference)
- [Conversion Table](#conversion-table)

---

## Spacing Reference

### Desktop (Bootstrap) - Spacing Scale

**Scale:** `0.25rem` = 4px per unit

| Class | Value | Pixels |
|-------|-------|--------|
| `m-0`, `p-0` | 0 | 0px |
| `m-1`, `p-1` | 0.25rem | 4px |
| `m-2`, `p-2` | 0.5rem | 8px |
| `m-3`, `p-3` | 1rem | 16px |
| `m-4`, `p-4` | 1.5rem | 24px |
| `m-5`, `p-5` | 3rem | 48px |

**Sides:**
- `t` = top
- `b` = bottom
- `s` = start (left in LTR)
- `e` = end (right in LTR)
- `x` = horizontal (left + right)
- `y` = vertical (top + bottom)

**Examples:**
```html
<div class="mt-3">Margin top 16px</div>
<div class="px-4">Padding horizontal 24px</div>
<div class="my-2">Margin vertical 8px</div>
<div class="p-0">No padding</div>
```

### Desktop - Custom Pixel Spacing

**Row spacing:**
```html
<div class="row row-space-0">0px gap</div>
<div class="row row-space-2">2px gap</div>
<div class="row row-space-4">4px gap</div>
<div class="row row-space-6">6px gap</div>
<div class="row row-space-8">8px gap</div>
<div class="row row-space-10">10px gap</div>
<div class="row row-space-12">12px gap</div>
<div class="row row-space-14">14px gap</div>
<div class="row row-space-16">16px gap</div>
```

### Mobile (Ionic) - Spacing

**Default value:** `16px` (from `--ion-padding` and `--ion-margin`)

| Class | CSS Property | Value |
|-------|-------------|-------|
| `ion-padding` | `padding` | 16px all sides |
| `ion-padding-top` | `padding-top` | 16px |
| `ion-padding-bottom` | `padding-bottom` | 16px |
| `ion-padding-start` | `padding-start` | 16px |
| `ion-padding-end` | `padding-end` | 16px |
| `ion-padding-vertical` | `padding` | 16px 0 |
| `ion-padding-horizontal` | `padding` | 0 16px |
| `ion-no-padding` | `padding` | 0 |
| `ion-margin` | `margin` | 16px all sides |
| `ion-margin-top` | `margin-top` | 16px |
| `ion-margin-bottom` | `margin-bottom` | 16px |
| `ion-margin-start` | `margin-start` | 16px |
| `ion-margin-end` | `margin-end` | 16px |
| `ion-margin-vertical` | `margin` | 16px 0 |
| `ion-margin-horizontal` | `margin` | 0 16px |
| `ion-no-margin` | `margin` | 0 |

**Customize:**
```scss
:root {
  --ion-padding: 20px; // Change default padding
  --ion-margin: 20px;  // Change default margin
}
```

---

## Typography Reference

### Desktop - Font Size

| Class | Value | Use Case |
|-------|-------|----------|
| `fs-8px` | 8px | Labels, badges |
| `fs-10px` | 10px | Small text |
| `fs-12px` | 12px | Captions |
| `fs-14px` | 14px | Body text small |
| `fs-16px` | 16px | Body text |
| `fs-18px` | 18px | Headings small |
| `fs-20px` | 20px | Headings |
| `fs-24px` | 24px | Headings large |
| `fs-30px` | 30px | Display text |
| `fs-36px` | 36px | Hero text |

### Desktop - Font Weight

| Class | Value | Name |
|-------|-------|------|
| `fw-100` | 100 | Thin |
| `fw-200` | 200 | Extra Light |
| `fw-300` | 300 | Light |
| `fw-400` | 400 | Normal |
| `fw-500` | 500 | Medium |
| `fw-600` | 600 | Semi Bold |
| `fw-700` | 700 | Bold |
| `fw-800` | 800 | Extra Bold |
| `fw-900` | 900 | Black |
| `fw-bold` | bold | Bold |
| `fw-normal` | normal | Normal |

### Desktop - Line Height

| Class | Value |
|-------|-------|
| `lh-1` | 1 |
| `lh-sm` | 1.25 |
| `lh-base` | 1.5 |
| `lh-lg` | 2 |

### Desktop - Text Utilities

| Class | Effect |
|-------|--------|
| `text-truncate` | Truncate with ellipsis |
| `text-wrap` | Allow wrapping |
| `text-nowrap` | No wrapping |
| `text-break` | Break long words |
| `text-lowercase` | lowercase text |
| `text-uppercase` | UPPERCASE TEXT |
| `text-capitalize` | Capitalize Each Word |
| `text-decoration-none` | No underline |
| `text-decoration-underline` | Underline |
| `text-decoration-line-through` | Strike through |
| `fst-italic` | Italic text |
| `fst-normal` | Normal (not italic) |

### Mobile - Text Utilities

| Class | Effect |
|-------|--------|
| `ion-text-wrap` | Allow wrapping (white-space: normal) |
| `ion-text-nowrap` | No wrapping (white-space: nowrap) |
| `ion-text-lowercase` | lowercase text |
| `ion-text-uppercase` | UPPERCASE TEXT |
| `ion-text-capitalize` | Capitalize Each Word |

---

## Layout Reference

### Desktop - Width Classes

**Percentage:**
```html
<div class="w-0">0%</div>
<div class="w-25">25%</div>
<div class="w-50">50%</div>
<div class="w-75">75%</div>
<div class="w-100">100%</div>
<div class="w-auto">auto</div>
```

**Pixels (50px - 500px in 50px increments):**
```html
<div class="w-50px">50px</div>
<div class="w-100px">100px</div>
<div class="w-150px">150px</div>
<div class="w-200px">200px</div>
<div class="w-250px">250px</div>
<div class="w-300px">300px</div>
<div class="w-350px">350px</div>
<div class="w-400px">400px</div>
<div class="w-450px">450px</div>
<div class="w-500px">500px</div>
```

**Viewport Width (10vw - 100vw):**
```html
<div class="vw-10">10vw</div>
<div class="vw-20">20vw</div>
<div class="vw-25">25vw</div>
<div class="vw-30">30vw</div>
<div class="vw-40">40vw</div>
<div class="vw-50">50vw</div>
<div class="vw-60">60vw</div>
<div class="vw-70">70vw</div>
<div class="vw-75">75vw</div>
<div class="vw-80">80vw</div>
<div class="vw-90">90vw</div>
<div class="vw-100">100vw</div>
```

### Desktop - Height Classes

**Percentage:**
```html
<div class="h-0">0%</div>
<div class="h-25">25%</div>
<div class="h-50">50%</div>
<div class="h-75">75%</div>
<div class="h-100">100%</div>
<div class="h-auto">auto</div>
```

**Pixels:** Same as width (h-50px, h-100px, etc.)

**Viewport Height:**
```html
<div class="vh-10">10vh</div>
<div class="vh-20">20vh</div>
<div class="vh-25">25vh</div>
<div class="vh-30">30vh</div>
<div class="vh-40">40vh</div>
<div class="vh-50">50vh</div>
<div class="vh-60">60vh</div>
<div class="vh-70">70vh</div>
<div class="vh-75">75vh</div>
<div class="vh-80">80vh</div>
<div class="vh-90">90vh</div>
<div class="vh-100">100vh</div>
```

### Desktop - Position

| Class | Position |
|-------|----------|
| `position-static` | static |
| `position-relative` | relative |
| `position-absolute` | absolute |
| `position-fixed` | fixed |
| `position-sticky` | sticky |

**Positioning:**
```html
<div class="position-absolute top-0 start-0">Top left</div>
<div class="position-absolute top-0 end-0">Top right</div>
<div class="position-absolute bottom-0 start-0">Bottom left</div>
<div class="position-absolute bottom-0 end-0">Bottom right</div>
<div class="position-absolute top-50 start-50 translate-middle">Center</div>
```

### Mobile - Flex Order

```html
<div class="ion-order-first">-1 (first)</div>
<div class="ion-order-0">0 (default)</div>
<div class="ion-order-1">1</div>
<div class="ion-order-2">2</div>
<!-- ... up to ion-order-12 -->
<div class="ion-order-last">13 (last)</div>
```

---

## Color Reference

### Desktop - Background Colors

| Class | Color |
|-------|-------|
| `bg-primary` | Primary brand color |
| `bg-secondary` | Secondary color |
| `bg-success` | Green (success) |
| `bg-danger` | Red (error) |
| `bg-warning` | Yellow (warning) |
| `bg-info` | Blue (info) |
| `bg-light` | Light gray |
| `bg-dark` | Dark gray/black |
| `bg-white` | White |
| `bg-transparent` | Transparent |

**Gray shades:**
```html
<div class="bg-gray-100">Lightest gray</div>
<div class="bg-gray-200"></div>
<div class="bg-gray-300"></div>
<div class="bg-gray-400"></div>
<div class="bg-gray-500">Medium gray</div>
<div class="bg-gray-600"></div>
<div class="bg-gray-700"></div>
<div class="bg-gray-800"></div>
<div class="bg-gray-900">Darkest gray</div>
```

### Desktop - Text Colors

| Class | Color |
|-------|-------|
| `text-primary` | Primary brand color |
| `text-secondary` | Secondary color |
| `text-success` | Green |
| `text-danger` | Red |
| `text-warning` | Yellow |
| `text-info` | Blue |
| `text-light` | Light gray |
| `text-dark` | Dark gray/black |
| `text-white` | White |
| `text-muted` | Muted gray |
| `text-body` | Default body color |
| `text-reset` | Reset inherited color |

### Mobile - Ionic Colors

Ionic uses CSS variables for theming. Colors are applied via component properties:

```html
<ion-button color="primary">Primary</ion-button>
<ion-button color="secondary">Secondary</ion-button>
<ion-button color="tertiary">Tertiary</ion-button>
<ion-button color="success">Success</ion-button>
<ion-button color="warning">Warning</ion-button>
<ion-button color="danger">Danger</ion-button>
<ion-button color="light">Light</ion-button>
<ion-button color="medium">Medium</ion-button>
<ion-button color="dark">Dark</ion-button>
```

**Define custom colors:**
```scss
:root {
  --ion-color-custom: #5260ff;
  --ion-color-custom-rgb: 82, 96, 255;
  --ion-color-custom-contrast: #ffffff;
  --ion-color-custom-contrast-rgb: 255, 255, 255;
  --ion-color-custom-shade: #4854e0;
  --ion-color-custom-tint: #6370ff;
}

.ion-color-custom {
  --ion-color-base: var(--ion-color-custom);
  --ion-color-base-rgb: var(--ion-color-custom-rgb);
  --ion-color-contrast: var(--ion-color-custom-contrast);
  --ion-color-contrast-rgb: var(--ion-color-custom-contrast-rgb);
  --ion-color-shade: var(--ion-color-custom-shade);
  --ion-color-tint: var(--ion-color-custom-tint);
}
```

---

## Border Reference

### Desktop - Border Classes

**Add borders:**
```html
<div class="border">All sides</div>
<div class="border-top">Top only</div>
<div class="border-end">Right only</div>
<div class="border-bottom">Bottom only</div>
<div class="border-start">Left only</div>
```

**Remove borders:**
```html
<div class="border-0">No border</div>
<div class="border-top-0">No top border</div>
<div class="border-end-0">No right border</div>
<div class="border-bottom-0">No bottom border</div>
<div class="border-start-0">No left border</div>
```

**Border width:**
```html
<div class="border border-1">1px</div>
<div class="border border-2">2px</div>
<div class="border border-3">3px</div>
<div class="border border-4">4px</div>
<div class="border border-5">5px</div>
```

**Border color:**
```html
<div class="border border-primary">Primary border</div>
<div class="border border-secondary">Secondary border</div>
<div class="border border-success">Success border</div>
<div class="border border-danger">Danger border</div>
<div class="border border-warning">Warning border</div>
<div class="border border-info">Info border</div>
<div class="border border-light">Light border</div>
<div class="border border-dark">Dark border</div>
<div class="border border-white">White border</div>
```

**Border radius:**
```html
<div class="rounded">Default rounded</div>
<div class="rounded-0">No radius</div>
<div class="rounded-1">Small radius</div>
<div class="rounded-2">Medium radius</div>
<div class="rounded-3">Large radius</div>
<div class="rounded-4">Extra large radius</div>
<div class="rounded-5">XXL radius</div>
<div class="rounded-top">Top corners only</div>
<div class="rounded-end">Right corners only</div>
<div class="rounded-bottom">Bottom corners only</div>
<div class="rounded-start">Left corners only</div>
<div class="rounded-circle">Perfect circle</div>
<div class="rounded-pill">Pill shape</div>
```

### Mobile - Border Classes

```html
<ion-header class="ion-no-border">
  <ion-toolbar>Header without border</ion-toolbar>
</ion-header>

<ion-footer class="ion-no-border">
  <ion-toolbar>Footer without border</ion-toolbar>
</ion-footer>
```

---

## Conversion Table

Quick reference for converting between Bootstrap and Ionic classes.

| Purpose | Desktop (Bootstrap) | Mobile (Ionic) |
|---------|-------------------|----------------|
| Padding all | `p-3` (16px) | `ion-padding` (16px) |
| Padding top | `pt-3` | `ion-padding-top` |
| Margin all | `m-3` | `ion-margin` |
| No padding | `p-0` | `ion-no-padding` |
| No margin | `m-0` | `ion-no-margin` |
| Hide | `d-none` | `ion-display-none` |
| Show block | `d-block` | `ion-display-block` |
| Show flex | `d-flex` | `ion-display-flex` |
| Flex row | `flex-row` | `ion-flex-row` |
| Flex column | `flex-column` | `ion-flex-column` |
| Justify center | `justify-content-center` | `ion-justify-content-center` |
| Align center | `align-items-center` | `ion-align-items-center` |
| Text center | `text-center` | `ion-text-center` |
| Text left | `text-start` | `ion-text-start` |
| Text right | `text-end` | `ion-text-end` |
| No wrap | `text-nowrap` | `ion-text-nowrap` |
| Uppercase | `text-uppercase` | `ion-text-uppercase` |
| Float left | `float-start` | `ion-float-start` |
| Float right | `float-end` | `ion-float-end` |
| Full width | `w-100` | (use inline style) |
| Full height | `h-100` | (use inline style) |
| Responsive MD | `d-md-block` | `ion-display-md-block` |
| Responsive LG | `d-lg-flex` | `ion-display-lg-flex` |

### Mixed Usage Example

```html
<!-- Works in both modes (separate classes for each) -->
<div class="d-flex justify-content-between p-3 
            ion-display-flex ion-justify-content-between ion-padding">
  <span>Start</span>
  <span>End</span>
</div>

<!-- Better: Use platform detection -->
<div *ngIf="isDesktop" class="d-flex justify-content-between p-3">
  <span>Start</span>
  <span>End</span>
</div>

<div *ngIf="isMobile" class="ion-display-flex ion-justify-content-between ion-padding">
  <span>Start</span>
  <span>End</span>
</div>
```

## Notes

1. **Bootstrap scale** is based on rem units (configurable)
2. **Ionic padding/margin** default is 16px (configurable via CSS variables)
3. **Responsive classes** use same breakpoints in both frameworks
4. **RTL support** - Use `start`/`end` instead of `left`/`right` when possible
5. **Dark mode** - Both frameworks support unified dark mode via `[data-bs-theme="dark"]`
