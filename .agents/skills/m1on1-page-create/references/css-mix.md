# Reference: CSS Mix — Utilidades CSS Híbridas Desktop/Mobile

Sistema de utilidades CSS dual para aplicaciones híbridas que funcionan tanto en **desktop**
(Color Admin / Bootstrap) como en **mobile** (Ionic Framework).

## Cuándo usar

- Construir apps híbridas que corren en desktop y mobile
- Implementar layouts responsivos que alternan entre Color Admin e Ionic
- Trabajar con carga dinámica de CSS basada en detección de plataforma
- Aplicar clases de utilidad en componentes que se adaptan a desktop o mobile
- Resolver conflictos CSS entre Bootstrap e Ionic
- Implementar dark mode en ambas plataformas
- Elegir qué clase usar según la plataforma activa

---

## Principios fundamentales

- **Platform-Aware** — distintas utilidades CSS para desktop (>768px) vs mobile (≤768px)
- **Zero Conflicts** — solo un framework CSS cargado a la vez
- **Dynamic Loading** — CSS cargado según el ancho de viewport
- **Unified Dark Mode** — mismo sistema de dark mode en ambas plataformas
- **Utility-First** — usar clases predefinidas en lugar de CSS personalizado

---

## Arquitectura general

```
Application Flow
├── styles.css (FontAwesome + mínimos globales)
│
├── Detección de plataforma (al cargar y al redimensionar)
│   ├── Desktop (>768px)
│   │   ├── Body class: desktop-mode
│   │   ├── CSS: desktop.css (Color Admin + Bootstrap)
│   │   └── Utilidades: clases Bootstrap / Color Admin
│   │
│   └── Mobile (≤768px)
│       ├── Body class: ionic-mode
│       ├── CSS: mobile.css (Ionic Framework)
│       └── Utilidades: clases Ionic (prefijo ion-*)
│
└── Al redimensionar (cruzando umbral 768px)
    ├── Limpiar TODOS los estilos dinámicos
    ├── Cambiar body class
    └── Cargar el bundle CSS apropiado
```

---

## Detección de plataforma

### Body classes

```typescript
// Comprobar modo actual
const isDesktop = document.body.classList.contains('desktop-mode');
const isMobile  = document.body.classList.contains('ionic-mode');
// Nunca ambas true al mismo tiempo
```

### Uso en componentes Angular

```typescript
import { Component, inject } from '@angular/core';
import { AppSettingsService } from '../../service';

@Component({
  selector: 'app-example',
  templateUrl: './example.html'
})
export class ExampleComponent {
  private appSettings = inject(AppSettingsService);

  get isDesktop() { return this.appSettings.currentMode === 'desktop'; }
  get isMobile()  { return this.appSettings.currentMode === 'mobile'; }
}
```

```html
<!-- En el template -->
<div [class.desktop-layout]="isDesktop"
     [class.mobile-layout]="isMobile">
  Contenido
</div>
```

---

## Clases de utilidad CSS

### Espaciado — Desktop (Bootstrap / Color Admin)

```html
<!-- Margin (m-0 … m-5) -->
<div class="m-0">Sin margin</div>
<div class="m-1">0.25rem (4px)</div>
<div class="m-2">0.5rem (8px)</div>
<div class="m-3">1rem (16px)</div>
<div class="m-4">1.5rem (24px)</div>
<div class="m-5">3rem (48px)</div>

<!-- Margin por lado -->
<div class="mt-3">top</div>
<div class="mb-3">bottom</div>
<div class="ms-3">start (izq. en LTR)</div>
<div class="me-3">end (der. en LTR)</div>
<div class="mx-3">horizontal</div>
<div class="my-3">vertical</div>

<!-- Padding (p-0 … p-5) -->
<div class="p-0">Sin padding</div>
<div class="p-3">1rem</div>
<div class="pt-3 pb-3 px-4">top/bottom 1rem, horizontal 1.5rem</div>
```

### Espaciado — Mobile (Ionic)

```html
<div class="ion-padding">Todos los lados (16px)</div>
<div class="ion-padding-top">Solo top</div>
<div class="ion-padding-bottom">Solo bottom</div>
<div class="ion-padding-start">Start (izq. en LTR)</div>
<div class="ion-padding-end">End (der. en LTR)</div>
<div class="ion-padding-vertical">Top y bottom</div>
<div class="ion-padding-horizontal">Left y right</div>
<div class="ion-no-padding">Sin padding</div>

<div class="ion-margin">Todos los lados</div>
<div class="ion-margin-top">Solo top</div>
<div class="ion-margin-bottom">Solo bottom</div>
<div class="ion-margin-vertical">Top y bottom</div>
<div class="ion-margin-horizontal">Left y right</div>
<div class="ion-no-margin">Sin margin</div>
```

### Display — Desktop (Bootstrap)

```html
<div class="d-none">Oculto</div>
<div class="d-block">Block</div>
<div class="d-inline">Inline</div>
<div class="d-inline-block">Inline-block</div>
<div class="d-flex">Flexbox</div>
<div class="d-grid">Grid</div>

<!-- Responsivo -->
<div class="d-none d-md-block">Oculto <768px, visible ≥768px</div>
<div class="d-block d-lg-none">Visible <992px, oculto ≥992px</div>
```

### Display — Mobile (Ionic)

```html
<div class="ion-display-none">Oculto</div>
<div class="ion-display-block">Block</div>
<div class="ion-display-inline">Inline</div>
<div class="ion-display-inline-block">Inline-block</div>
<div class="ion-display-flex">Flexbox</div>
<div class="ion-display-grid">Grid</div>

<!-- Responsivo -->
<div class="ion-display-none ion-display-md-block">Oculto <768px, visible ≥768px</div>
<div class="ion-display-block ion-display-lg-none">Visible <992px, oculto ≥992px</div>
```

### Flexbox — Desktop (Bootstrap)

```html
<div class="d-flex justify-content-between align-items-center">
  <span>Start</span>
  <span>End</span>
</div>

<div class="d-flex flex-column flex-lg-row">
  <div class="flex-fill">Columna 1</div>
  <div class="flex-fill">Columna 2</div>
</div>

<!-- justify-content -->
<div class="d-flex justify-content-start">Start</div>
<div class="d-flex justify-content-end">End</div>
<div class="d-flex justify-content-center">Center</div>
<div class="d-flex justify-content-between">Space between</div>
<div class="d-flex justify-content-around">Space around</div>

<!-- align-items -->
<div class="d-flex align-items-start">Top</div>
<div class="d-flex align-items-center">Middle</div>
<div class="d-flex align-items-end">Bottom</div>
<div class="d-flex align-items-stretch">Stretch</div>
```

### Flexbox — Mobile (Ionic)

```html
<div class="ion-display-flex ion-justify-content-between ion-align-items-center">
  <span>Start</span>
  <span>End</span>
</div>

<div class="ion-display-flex ion-flex-column ion-flex-lg-row">
  <div class="ion-flex-1">Columna 1</div>
  <div class="ion-flex-1">Columna 2</div>
</div>

<!-- justify-content -->
<div class="ion-display-flex ion-justify-content-start">Start</div>
<div class="ion-display-flex ion-justify-content-end">End</div>
<div class="ion-display-flex ion-justify-content-center">Center</div>
<div class="ion-display-flex ion-justify-content-between">Space between</div>
<div class="ion-display-flex ion-justify-content-around">Space around</div>

<!-- align-items -->
<div class="ion-display-flex ion-align-items-start">Top</div>
<div class="ion-display-flex ion-align-items-center">Middle</div>
<div class="ion-display-flex ion-align-items-end">Bottom</div>
<div class="ion-display-flex ion-align-items-stretch">Stretch</div>
```

### Alineación de texto — Desktop

```html
<div class="text-start">Izquierda</div>
<div class="text-center">Centro</div>
<div class="text-end">Derecha</div>
<div class="text-justify">Justificado</div>

<!-- Responsivo -->
<div class="text-center text-md-start">Centro en mobile, izq. en tablet+</div>
```

### Alineación de texto — Mobile

```html
<div class="ion-text-left">Izquierda</div>
<div class="ion-text-center">Centro</div>
<div class="ion-text-right">Derecha</div>
<div class="ion-text-justify">Justificado</div>
<div class="ion-text-start">Start (RTL-aware)</div>
<div class="ion-text-end">End (RTL-aware)</div>

<!-- Responsivo -->
<div class="ion-text-center ion-text-md-start">Centro mobile, izq. tablet+</div>
```

### Ancho y alto — Desktop

```html
<!-- Porcentaje -->
<div class="w-25">25%</div>
<div class="w-50">50%</div>
<div class="w-75">75%</div>
<div class="w-100">100%</div>

<!-- Píxeles (utility classes de Color Admin helper) -->
<div class="w-100px">100px</div>
<div class="w-200px">200px</div>
<div class="w-300px">300px</div>

<!-- Viewport -->
<div class="vw-50">50vw</div>
<div class="vh-100">100vh</div>

<!-- Alto -->
<div class="h-25">25%</div>
<div class="h-50">50%</div>
<div class="h-100">100%</div>
<div class="h-200px">200px</div>
```

### Bordes y radios — Desktop

```html
<div class="border">Todos los bordes</div>
<div class="border-top">Solo top</div>
<div class="border-0">Sin borde</div>
<div class="border border-primary border-3">Borde primary 3px</div>

<!-- Border radius -->
<div class="rounded">Redondeado</div>
<div class="rounded-0">Sin radio</div>
<div class="rounded-1">Radio pequeño</div>
<div class="rounded-3">Radio grande</div>
<div class="rounded-circle">Círculo</div>
<div class="rounded-pill">Pill</div>
```

### Colores — Desktop

```html
<!-- Fondo -->
<div class="bg-primary">Primary</div>
<div class="bg-secondary">Secondary</div>
<div class="bg-success">Success</div>
<div class="bg-danger">Danger</div>
<div class="bg-warning">Warning</div>
<div class="bg-info">Info</div>
<div class="bg-light">Light</div>
<div class="bg-dark">Dark</div>

<!-- Texto -->
<span class="text-primary">Primary</span>
<span class="text-secondary">Secondary</span>
<span class="text-success">Success</span>
<span class="text-danger">Danger</span>
<span class="text-muted">Muted</span>
```

### Visibilidad — Desktop

```html
<div class="visible">Visible</div>
<div class="invisible">Invisible (ocupa espacio)</div>
<div class="opacity-0">Transparente</div>
<div class="opacity-25">25% opacidad</div>
<div class="opacity-50">50% opacidad</div>
<div class="opacity-75">75% opacidad</div>
<div class="opacity-100">Opaco</div>
```

---

## Breakpoints responsivos

### Desktop (Bootstrap)

| Breakpoint | Ancho mín. | Infix |
|------------|------------|-------|
| X-Small    | <576px     | (ninguno) |
| Small      | ≥576px     | `sm`  |
| Medium     | ≥768px     | `md`  |
| Large      | ≥992px     | `lg`  |
| X-Large    | ≥1200px    | `xl`  |
| XX-Large   | ≥1400px    | `xxl` |

```html
<div class="d-none d-md-block d-xl-flex">
  <!-- Oculto <768px, block 768-1199px, flex ≥1200px -->
</div>
```

### Mobile (Ionic)

| Breakpoint | Ancho mín. | Infix |
|------------|------------|-------|
| X-Small    | <576px     | (ninguno) |
| Small      | ≥576px     | `sm`  |
| Medium     | ≥768px     | `md`  |
| Large      | ≥992px     | `lg`  |
| X-Large    | ≥1200px    | `xl`  |

```html
<div class="ion-display-none ion-display-md-block ion-display-xl-flex">
  <!-- Oculto <768px, block 768-1199px, flex ≥1200px -->
</div>
```

---

## Implementación de Dark Mode

### Control unificado (funciona para ambas plataformas)

```typescript
// Activar dark mode
document.documentElement.setAttribute('data-bs-theme', 'dark');

// Desactivar dark mode
document.documentElement.removeAttribute('data-bs-theme');
// o
document.documentElement.setAttribute('data-bs-theme', 'light');

// Comprobar modo actual
const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
```

### Selectores CSS para dark mode

```scss
// ✅ CORRECTO — selector en <html>
[data-bs-theme="dark"] {
  .my-component {
    background: var(--bs-dark);
  }

  ion-toolbar {
    --background: var(--bs-dark);
  }
}

// ❌ INCORRECTO — busca el atributo en <body>
body[data-bs-theme="dark"] {
  // No funciona
}
```

### Archivos CSS de dark mode

- Desktop: `desktop.css` ya incluye soporte dark mode
- Mobile: importar paleta dark en `mobile.css` (compilado desde `ionic.scss`)

```scss
// En ionic.scss
@import '@ionic/angular/css/palettes/dark.class.css';
```

---

## Carga dinámica de CSS

### Cómo funciona

1. **Carga inicial** — solo `styles.css` (FontAwesome + mínimos globales)
2. **Detección de plataforma** — comprueba `window.innerWidth`
3. **Carga de CSS** — carga `desktop.css` OR `mobile.css`
4. **Redimensionado** — limpia y recarga al cruzar el umbral 768px

### Métodos clave del servicio

```typescript
// Comprobar si los estilos están cargados
if (this.appSettings.stylesLoaded) {
  // Seguro para renderizar
}

// Obtener modo actual
const mode = this.appSettings.currentMode; // 'desktop' | 'mobile'

// Escuchar cambios de modo
this.appSettings.modeChanged$.subscribe(mode => {
  console.log('Modo cambiado a:', mode);
});
```

### Verificación en DevTools

```javascript
// Modo desktop (>768px)
document.querySelectorAll('link[id^="desktop-dynamic"]').length // 1
document.querySelectorAll('link[id^="ionic-dynamic"]').length   // 0
document.body.classList.contains('desktop-mode')                // true
document.body.classList.contains('ionic-mode')                  // false

// Modo mobile (≤768px)
document.querySelectorAll('link[id^="ionic-dynamic"]').length   // 1
document.querySelectorAll('link[id^="desktop-dynamic"]').length // 0
document.body.classList.contains('ionic-mode')                  // true
document.body.classList.contains('desktop-mode')                // false
```

---

## Patrones comunes

### Layout adaptativo

```html
<!-- Desktop: horizontal, Mobile: vertical -->
<div class="d-flex flex-column flex-lg-row
            ion-display-flex ion-flex-column ion-flex-lg-row">
  <div class="flex-fill ion-flex-1">Sección 1</div>
  <div class="flex-fill ion-flex-1">Sección 2</div>
</div>
```

### Contenido condicional por plataforma

```html
<!-- UI de escritorio con Bootstrap cards -->
<div *ngIf="isDesktop" class="card">
  <div class="card-body">...</div>
</div>

<!-- UI mobile con Ionic cards -->
<ion-card *ngIf="isMobile">
  <ion-card-content>...</ion-card-content>
</ion-card>
```

### Espaciado responsivo combinado

```html
<!-- Clases de ambas plataformas (redundante pero seguro) -->
<div class="p-3 m-2 ion-padding ion-margin">
  Contenido
</div>
```

### Desarrollo utility-first

```html
<!-- ✅ CORRECTO — usar utility classes -->
<div class="d-flex justify-content-between align-items-center p-3 bg-light">
  <span class="text-primary fw-bold">Título</span>
  <button class="btn btn-sm btn-primary">Acción</button>
</div>

<!-- ❌ EVITAR — CSS personalizado -->
<div class="custom-header">
  <span class="custom-title">Título</span>
  <button class="custom-button">Acción</button>
</div>
<!-- Requeriría CSS propio en desktop.scss e ionic.scss -->
```

---

## Buenas prácticas

### ✅ HACER

```typescript
// 1. Comprobar plataforma antes de aplicar estilos
if (this.appSettings.currentMode === 'desktop') {
  // Usar clases Bootstrap
} else {
  // Usar clases Ionic
}

// 2. Esperar a que los estilos estén cargados
if (!this.appSettings.stylesLoaded) {
  return; // No renderizar aún
}

// 3. Usar utility classes en lugar de CSS personalizado
```

```html
<!-- Desktop -->
<div class="card"><button class="btn btn-primary">...</button></div>

<!-- Mobile -->
<ion-card><ion-button color="primary">...</ion-button></ion-card>

<!-- Utility classes -->
<div class="d-flex gap-3 p-3">...</div>
<div class="ion-display-flex ion-padding">...</div>
```

### ❌ NO HACER

```html
<!-- 1. Nunca mezclar clases de frameworks en el mismo elemento -->
<div class="card ion-padding">❌ Bootstrap + Ionic mezclados</div>
```

```typescript
// 2. Nunca cargar CSS manualmente
const link = document.createElement('link');
link.href = 'desktop.css';
document.head.appendChild(link); // ❌

// 3. Nunca importar frameworks en styles.css
// ❌ En styles.css:
// @import '@ionic/angular/css/core.css';
// @import 'bootstrap/dist/css/bootstrap.css';

// 4. Nunca usar !important para forzar estilos
// ❌
.my-class { padding: 20px !important; }

// ✅ En su lugar:
// <div class="p-4">...</div>         <!-- Bootstrap -->
// <div class="ion-padding">...</div> <!-- Ionic -->
```

---

## Troubleshooting

### CSS no carga

```javascript
// Verificar hojas de estilo cargadas
document.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
  console.log(link.id, link.href, link.sheet ? '✅ cargado' : '⏳ cargando');
});

// Forzar recarga
location.reload();
```

### Estilos conflictivos

```javascript
// Limpiar todos los estilos dinámicos
document.querySelectorAll('link[id*="-dynamic-"]').forEach(l => l.remove());
location.reload();
```

### Plataforma detectada incorrectamente

```javascript
console.log('Ancho:', window.innerWidth);
console.log('Modo esperado:', window.innerWidth > 768 ? 'desktop' : 'mobile');
console.log('Modo actual:', document.body.classList.contains('desktop-mode') ? 'desktop' : 'mobile');
```

### Estilos no cambian al redimensionar

```javascript
// Verificar listener de resize
window.addEventListener('resize', () => {
  console.log('Resize detectado:', window.innerWidth);
});

// Comprobar si cruza el umbral
console.log('Cruzando umbral:', window.innerWidth === 768);
```

---

## Quick Reference

### Desktop (>768px) — Bootstrap / Color Admin

| Categoría  | Clases |
|------------|--------|
| Spacing    | `m-{0-5}`, `p-{0-5}`, `mt-`, `mb-`, `ms-`, `me-`, `mx-`, `my-` |
| Display    | `d-{none\|block\|flex\|grid}` |
| Flex       | `d-flex`, `justify-content-*`, `align-items-*` |
| Text       | `text-{start\|center\|end}` |
| Colors     | `bg-{primary\|secondary\|…}`, `text-{primary\|…}` |
| Responsive | `d-md-*`, `w-lg-*`, etc. |

### Mobile (≤768px) — Ionic

| Categoría  | Clases |
|------------|--------|
| Spacing    | `ion-padding-*`, `ion-margin-*`, `ion-no-padding` |
| Display    | `ion-display-{none\|block\|flex\|grid}` |
| Flex       | `ion-display-flex`, `ion-justify-content-*`, `ion-align-items-*` |
| Text       | `ion-text-{left\|center\|right\|start\|end}` |
| Borders    | `ion-no-border` |
| Responsive | `ion-display-md-*`, `ion-text-lg-*`, etc. |

---

## Ubicación de archivos

| Archivo | Ruta |
|---------|------|
| Desktop CSS | `dist/desktop.css` (compilado desde `src/scss/angular.scss`) |
| Mobile CSS  | `dist/mobile.css` (compilado desde `src/scss/ionic.scss`) |
| Global Styles | `src/styles.css` |
| Platform Service | `src/app/service/platform-detector.service.ts` |
| App Settings | `src/app/service/app-settings.service.ts` |
