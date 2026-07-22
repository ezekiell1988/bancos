# Color Admin CSS Mix - Hybrid Desktop/Mobile Styling

Sistema de utilidades CSS dual para aplicaciones híbridas que funcionan tanto en desktop (Color Admin/Bootstrap) como mobile (Ionic Framework).

## When to Use

Use this skill when:
- Building hybrid apps that run on both desktop and mobile
- Implementing responsive layouts that switch between Color Admin and Ionic
- Working with dynamic CSS loading based on platform detection
- Styling components that need to adapt to desktop or mobile mode
- Troubleshooting CSS conflicts between Bootstrap and Ionic
- Implementing dark mode across both platforms
- Need to know which utility classes to use for desktop vs mobile

## Core Principles

✅ **Platform-Aware** - Different CSS utilities for desktop (>768px) vs mobile (≤768px)  
✅ **Zero Conflicts** - Only one CSS framework loaded at a time  
✅ **Dynamic Loading** - CSS loaded based on viewport width detection  
✅ **Unified Dark Mode** - Same dark mode system across both platforms  
✅ **Utility-First** - Use predefined classes instead of custom CSS  

## Architecture Overview

```
Application Flow
├── styles.css (FontAwesome + minimal globals only)
│
├── Platform Detection (on load & resize)
│   ├── Desktop (>768px)
│   │   ├── Body class: desktop-mode
│   │   ├── CSS: desktop.css (Color Admin + Bootstrap)
│   │   └── Utilities: Bootstrap/Color Admin classes
│   │
│   └── Mobile (≤768px)
│       ├── Body class: ionic-mode
│       ├── CSS: mobile.css (Ionic Framework)
│       └── Utilities: Ionic classes (ion-* prefixed)
│
└── On Resize (crossing 768px threshold)
    ├── Clean ALL styles
    ├── Switch body class
    └── Load appropriate CSS bundle
```

## Platform Detection

### Body Classes

The platform is identified by body classes:

```typescript
// Check current mode
const isDesktop = document.body.classList.contains('desktop-mode');
const isMobile = document.body.classList.contains('ionic-mode');

// Never both true at same time
```

### Conditional Styling in Components

```typescript
import { Component, inject } from '@angular/core';
import { AppSettingsService } from '../../service';

@Component({
  selector: 'app-example',
  templateUrl: './example.html'
})
export class ExampleComponent {
  private appSettings = inject(AppSettingsService);
  
  // Access current mode
  get isDesktop() {
    return this.appSettings.currentMode === 'desktop';
  }
  
  get isMobile() {
    return this.appSettings.currentMode === 'mobile';
  }
}
```

```html
<!-- In template -->
<div [class.desktop-layout]="isDesktop" 
     [class.mobile-layout]="isMobile">
  Content
</div>
```

## CSS Utility Classes

### Spacing - Desktop (Bootstrap/Color Admin)

Use these when in `desktop-mode`:

```html
<!-- Margin -->
<div class="m-0">No margin</div>
<div class="m-1">Margin 0.25rem (4px)</div>
<div class="m-2">Margin 0.5rem (8px)</div>
<div class="m-3">Margin 1rem (16px)</div>
<div class="m-4">Margin 1.5rem (24px)</div>
<div class="m-5">Margin 3rem (48px)</div>

<!-- Margin specific sides -->
<div class="mt-3">Margin top</div>
<div class="mb-3">Margin bottom</div>
<div class="ms-3">Margin start (left in LTR)</div>
<div class="me-3">Margin end (right in LTR)</div>
<div class="mx-3">Margin horizontal</div>
<div class="my-3">Margin vertical</div>

<!-- Padding -->
<div class="p-0">No padding</div>
<div class="p-1">Padding 0.25rem</div>
<div class="p-2">Padding 0.5rem</div>
<div class="p-3">Padding 1rem</div>
<div class="p-4">Padding 1.5rem</div>
<div class="p-5">Padding 3rem</div>

<!-- Padding specific sides -->
<div class="pt-3 pb-3 px-4">Top/bottom 1rem, horizontal 1.5rem</div>
```

### Spacing - Mobile (Ionic)

Use these when in `ionic-mode`:

```html
<!-- Padding (16px default) -->
<div class="ion-padding">All sides</div>
<div class="ion-padding-top">Top only</div>
<div class="ion-padding-bottom">Bottom only</div>
<div class="ion-padding-start">Start (left in LTR)</div>
<div class="ion-padding-end">End (right in LTR)</div>
<div class="ion-padding-vertical">Top and bottom</div>
<div class="ion-padding-horizontal">Left and right</div>
<div class="ion-no-padding">Remove padding</div>

<!-- Margin (16px default) -->
<div class="ion-margin">All sides</div>
<div class="ion-margin-top">Top only</div>
<div class="ion-margin-bottom">Bottom only</div>
<div class="ion-margin-vertical">Top and bottom</div>
<div class="ion-margin-horizontal">Left and right</div>
<div class="ion-no-margin">Remove margin</div>
```

### Display - Desktop (Bootstrap)

```html
<!-- Basic display -->
<div class="d-none">Hidden</div>
<div class="d-block">Block</div>
<div class="d-inline">Inline</div>
<div class="d-inline-block">Inline-block</div>
<div class="d-flex">Flexbox</div>
<div class="d-grid">Grid</div>

<!-- Responsive display -->
<div class="d-none d-md-block">Hidden on mobile, visible on tablet+</div>
<div class="d-block d-lg-none">Visible on mobile/tablet, hidden on desktop</div>
```

### Display - Mobile (Ionic)

```html
<!-- Basic display -->
<div class="ion-display-none">Hidden</div>
<div class="ion-display-block">Block</div>
<div class="ion-display-inline">Inline</div>
<div class="ion-display-inline-block">Inline-block</div>
<div class="ion-display-flex">Flexbox</div>
<div class="ion-display-grid">Grid</div>

<!-- Responsive display -->
<div class="ion-display-none ion-display-md-block">Hidden <768px, visible ≥768px</div>
<div class="ion-display-block ion-display-lg-none">Visible <992px, hidden ≥992px</div>
```

### Flexbox - Desktop (Bootstrap)

```html
<div class="d-flex justify-content-between align-items-center">
  <span>Start</span>
  <span>End</span>
</div>

<div class="d-flex flex-column flex-lg-row">
  <div class="flex-fill">Column 1</div>
  <div class="flex-fill">Column 2</div>
</div>

<!-- Justify content -->
<div class="d-flex justify-content-start">Start</div>
<div class="d-flex justify-content-end">End</div>
<div class="d-flex justify-content-center">Center</div>
<div class="d-flex justify-content-between">Space between</div>
<div class="d-flex justify-content-around">Space around</div>

<!-- Align items -->
<div class="d-flex align-items-start">Align top</div>
<div class="d-flex align-items-center">Align middle</div>
<div class="d-flex align-items-end">Align bottom</div>
<div class="d-flex align-items-stretch">Stretch</div>
```

### Flexbox - Mobile (Ionic)

```html
<div class="ion-display-flex ion-justify-content-between ion-align-items-center">
  <span>Start</span>
  <span>End</span>
</div>

<div class="ion-display-flex ion-flex-column ion-flex-lg-row">
  <div class="ion-flex-1">Column 1</div>
  <div class="ion-flex-1">Column 2</div>
</div>

<!-- Justify content -->
<div class="ion-display-flex ion-justify-content-start">Start</div>
<div class="ion-display-flex ion-justify-content-end">End</div>
<div class="ion-display-flex ion-justify-content-center">Center</div>
<div class="ion-display-flex ion-justify-content-between">Space between</div>
<div class="ion-display-flex ion-justify-content-around">Space around</div>

<!-- Align items -->
<div class="ion-display-flex ion-align-items-start">Align top</div>
<div class="ion-display-flex ion-align-items-center">Align middle</div>
<div class="ion-display-flex ion-align-items-end">Align bottom</div>
<div class="ion-display-flex ion-align-items-stretch">Stretch</div>
```

### Text Alignment - Desktop

```html
<div class="text-start">Left aligned</div>
<div class="text-center">Center aligned</div>
<div class="text-end">Right aligned</div>
<div class="text-justify">Justified</div>

<!-- Responsive -->
<div class="text-center text-md-start">Center on mobile, left on tablet+</div>
```

### Text Alignment - Mobile

```html
<div class="ion-text-left">Left aligned</div>
<div class="ion-text-center">Center aligned</div>
<div class="ion-text-right">Right aligned</div>
<div class="ion-text-justify">Justified</div>

<!-- RTL-aware -->
<div class="ion-text-start">Start (left in LTR, right in RTL)</div>
<div class="ion-text-end">End (right in LTR, left in RTL)</div>

<!-- Responsive -->
<div class="ion-text-center ion-text-md-start">Center on mobile, left on tablet+</div>
```

### Width & Height - Desktop

```html
<!-- Percentage -->
<div class="w-25">25% width</div>
<div class="w-50">50% width</div>
<div class="w-75">75% width</div>
<div class="w-100">100% width</div>

<!-- Pixels -->
<div class="w-100px">100px width</div>
<div class="w-200px">200px width</div>
<div class="w-300px">300px width</div>

<!-- Viewport -->
<div class="vw-50">50vw width</div>
<div class="vh-100">100vh height</div>

<!-- Height -->
<div class="h-25">25% height</div>
<div class="h-50">50% height</div>
<div class="h-100">100% height</div>
<div class="h-200px">200px height</div>
```

### Borders & Radius - Desktop

```html
<!-- Borders -->
<div class="border">All borders</div>
<div class="border-top">Top border only</div>
<div class="border-0">No border</div>
<div class="border border-primary border-3">Primary border 3px</div>

<!-- Border radius -->
<div class="rounded">Rounded</div>
<div class="rounded-0">No radius</div>
<div class="rounded-1">Small radius</div>
<div class="rounded-3">Large radius</div>
<div class="rounded-circle">Circle</div>
<div class="rounded-pill">Pill shape</div>
```

### Colors - Desktop

```html
<!-- Background -->
<div class="bg-primary">Primary background</div>
<div class="bg-secondary">Secondary background</div>
<div class="bg-success">Success background</div>
<div class="bg-danger">Danger background</div>
<div class="bg-warning">Warning background</div>
<div class="bg-info">Info background</div>
<div class="bg-light">Light background</div>
<div class="bg-dark">Dark background</div>

<!-- Text -->
<span class="text-primary">Primary text</span>
<span class="text-secondary">Secondary text</span>
<span class="text-success">Success text</span>
<span class="text-danger">Danger text</span>
<span class="text-muted">Muted text</span>
```

### Visibility - Desktop

```html
<div class="visible">Visible</div>
<div class="invisible">Invisible (takes space)</div>
<div class="opacity-0">Fully transparent</div>
<div class="opacity-25">25% opacity</div>
<div class="opacity-50">50% opacity</div>
<div class="opacity-75">75% opacity</div>
<div class="opacity-100">Fully opaque</div>
```

## Responsive Breakpoints

### Desktop (Bootstrap)

| Breakpoint | Min Width | Class Infix |
|------------|-----------|-------------|
| X-Small | <576px | (none) |
| Small | ≥576px | `sm` |
| Medium | ≥768px | `md` |
| Large | ≥992px | `lg` |
| X-Large | ≥1200px | `xl` |
| XX-Large | ≥1400px | `xxl` |

**Usage:**
```html
<div class="d-none d-md-block d-xl-flex">
  <!-- Hidden <768px, block 768-1199px, flex ≥1200px -->
</div>
```

### Mobile (Ionic)

| Breakpoint | Min Width | Class Infix |
|------------|-----------|-------------|
| X-Small | <576px | (none) |
| Small | ≥576px | `sm` |
| Medium | ≥768px | `md` |
| Large | ≥992px | `lg` |
| X-Large | ≥1200px | `xl` |

**Usage:**
```html
<div class="ion-display-none ion-display-md-block ion-display-xl-flex">
  <!-- Hidden <768px, block 768-1199px, flex ≥1200px -->
</div>
```

## Dark Mode Implementation

Both platforms use the same dark mode system:

### Unified Dark Mode Control

```typescript
// Set dark mode (works for both platforms)
document.documentElement.setAttribute('data-bs-theme', 'dark');

// Remove dark mode
document.documentElement.removeAttribute('data-bs-theme');
// or
document.documentElement.setAttribute('data-bs-theme', 'light');

// Check current mode
const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
```

### Dark Mode CSS Selectors

```scss
// ✅ CORRECT - Selector on <html>
[data-bs-theme="dark"] {
  .my-component {
    background: var(--bs-dark);
  }
  
  ion-toolbar {
    --background: var(--bs-dark);
  }
}

// ❌ WRONG - Would look for attribute on <body>
body[data-bs-theme="dark"] {
  // Won't work!
}
```

### Dark Mode CSS Files

Desktop: Already includes dark mode support in `desktop.css`  
Mobile: Import dark mode palette in `mobile.css` (compiled from `ionic.scss`)

```scss
// In ionic.scss
@import '@ionic/angular/css/palettes/dark.class.css';
```

## Dynamic CSS Loading

### How It Works

1. **Initial Load** - Only `styles.css` (FontAwesome + minimal globals)
2. **Platform Detection** - Check `window.innerWidth`
3. **CSS Loading** - Load desktop.css OR mobile.css
4. **Resize Handling** - Clean & reload when crossing 768px threshold

### Key Service Methods

```typescript
// Check if styles are loaded
if (this.appSettings.stylesLoaded) {
  // Safe to render
}

// Get current mode
const mode = this.appSettings.currentMode; // 'desktop' | 'mobile'

// Listen for mode changes
this.appSettings.modeChanged$.subscribe(mode => {
  console.log('Mode changed to:', mode);
});
```

### Verification in DevTools

```javascript
// Desktop mode verification (>768px)
document.querySelectorAll('link[id^="desktop-dynamic"]').length // Should be 1
document.querySelectorAll('link[id^="ionic-dynamic"]').length   // Should be 0
document.body.classList.contains('desktop-mode')                // true
document.body.classList.contains('ionic-mode')                  // false

// Mobile mode verification (≤768px)
document.querySelectorAll('link[id^="ionic-dynamic"]').length   // Should be 1
document.querySelectorAll('link[id^="desktop-dynamic"]').length // Should be 0
document.body.classList.contains('ionic-mode')                  // true
document.body.classList.contains('desktop-mode')                // false
```

## Common Patterns

### Adaptive Layout

```html
<!-- Desktop: horizontal layout, Mobile: vertical stack -->
<div class="d-flex flex-column flex-lg-row 
            ion-display-flex ion-flex-column ion-flex-lg-row">
  <div class="flex-fill ion-flex-1">Section 1</div>
  <div class="flex-fill ion-flex-1">Section 2</div>
</div>
```

### Conditional Content

```html
<!-- Show different content based on platform -->
<div *ngIf="isDesktop" class="card">
  <div class="card-body">
    <!-- Desktop-specific UI with Bootstrap cards -->
  </div>
</div>

<ion-card *ngIf="isMobile">
  <ion-card-content>
    <!-- Mobile-specific UI with Ionic cards -->
  </ion-card-content>
</ion-card>
```

### Responsive Spacing

```html
<!-- Desktop spacing -->
<div class="p-3 m-2">
  Desktop padding/margin
</div>

<!-- Mobile spacing -->
<div class="ion-padding ion-margin">
  Mobile padding/margin
</div>

<!-- Combined (works in both modes) -->
<div class="p-3 m-2 ion-padding ion-margin">
  Redundant but safe
</div>
```

### Utility-First Development

```html
<!-- ✅ GOOD - Use utility classes -->
<div class="d-flex justify-content-between align-items-center p-3 bg-light">
  <span class="text-primary fw-bold">Title</span>
  <button class="btn btn-sm btn-primary">Action</button>
</div>

<!-- ❌ AVOID - Custom CSS -->
<div class="custom-header">
  <span class="custom-title">Title</span>
  <button class="custom-button">Action</button>
</div>
<!-- Would need custom CSS in both desktop.scss and ionic.scss -->
```

## Best Practices

### ✅ DO

```typescript
// 1. Check platform before applying styles
if (this.appSettings.currentMode === 'desktop') {
  // Use Bootstrap classes
} else {
  // Use Ionic classes
}

// 2. Use platform-specific components
// Desktop
<div class="card">...</div>
<button class="btn btn-primary">...</button>

// Mobile
<ion-card>...</ion-card>
<ion-button color="primary">...</ion-button>

// 3. Wait for styles to load
if (!this.appSettings.stylesLoaded) {
  return; // Don't render yet
}

// 4. Use utility classes instead of custom CSS
<div class="d-flex gap-3 p-3">...</div>
<div class="ion-display-flex ion-padding">...</div>
```

### ❌ DON'T

```typescript
// 1. Never mix framework classes on same element
<div class="card ion-padding">❌ Mixed Bootstrap + Ionic</div>

// 2. Never manually load CSS
// ❌ Don't do this
const link = document.createElement('link');
link.href = 'desktop.css';
document.head.appendChild(link);

// 3. Never import frameworks in styles.css
// ❌ Don't do this in styles.css
@import '@ionic/angular/css/core.css';
@import 'bootstrap/dist/css/bootstrap.css';

// 4. Never use !important to force styles
// ❌ Avoid
.my-class {
  padding: 20px !important;
}

// ✅ Instead, use correct utility class
<div class="p-4">...</div> <!-- Bootstrap -->
<div class="ion-padding">...</div> <!-- Ionic -->
```

## Troubleshooting

### CSS Not Loading

```javascript
// Check loaded stylesheets
document.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
  console.log(link.id, link.href, link.sheet ? '✅ loaded' : '⏳ loading');
});

// Force reload
location.reload();
```

### Conflicting Styles

```javascript
// Clean all dynamic styles
document.querySelectorAll('link[id*="-dynamic-"]').forEach(l => l.remove());

// Reload page
location.reload();
```

### Wrong Platform Detected

```javascript
// Check detection logic
console.log('Width:', window.innerWidth);
console.log('Expected mode:', window.innerWidth > 768 ? 'desktop' : 'mobile');
console.log('Actual mode:', document.body.classList.contains('desktop-mode') ? 'desktop' : 'mobile');
```

### Styles Not Switching on Resize

```javascript
// Verify resize listener
window.addEventListener('resize', () => {
  console.log('Resize detected:', window.innerWidth);
});

// Check if crossing threshold
const threshold = 768;
console.log('Crossing threshold:', window.innerWidth === threshold);
```

## Quick Reference

### Desktop (>768px) - Bootstrap/Color Admin

- Prefix: None (standard Bootstrap)
- Spacing: `m-{0-5}`, `p-{0-5}`, `mt-`, `mb-`, etc.
- Display: `d-{none|block|flex|grid}`
- Flex: `d-flex`, `justify-content-*`, `align-items-*`
- Text: `text-{start|center|end}`
- Colors: `bg-{primary|secondary|...}`, `text-{primary|...}`
- Responsive: `d-md-*`, `w-lg-*`, etc.

### Mobile (≤768px) - Ionic

- Prefix: `ion-`
- Spacing: `ion-padding-*`, `ion-margin-*`, `ion-no-padding`
- Display: `ion-display-{none|block|flex|grid}`
- Flex: `ion-display-flex`, `ion-justify-content-*`, `ion-align-items-*`
- Text: `ion-text-{left|center|right|start|end}`
- Borders: `ion-no-border`
- Responsive: `ion-display-md-*`, `ion-text-lg-*`, etc.

## File Locations

- Desktop CSS: `dist/desktop.css` (compiled from `src/scss/angular.scss`)
- Mobile CSS: `dist/mobile.css` (compiled from `src/scss/ionic.scss`)
- Global Styles: `src/styles.css`
- Platform Service: `src/app/service/platform-detector.service.ts`
- App Settings: `src/app/service/app-settings.service.ts`

## References

See `references/` directory for:
- `utility-classes.md` - Complete list of all utility classes
- `dark-mode-patterns.md` - Dark mode implementation patterns
- `responsive-layouts.md` - Responsive layout examples
- `troubleshooting.md` - Common issues and solutions
