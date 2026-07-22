# Dark Mode Implementation Patterns

Patrones y mejores prácticas para implementar y gestionar el modo oscuro en aplicaciones híbridas.

## Table of Contents

- [Unified Dark Mode System](#unified-dark-mode-system)
- [Dark Mode Toggle Component](#dark-mode-toggle-component)
- [Styling Dark Mode](#styling-dark-mode)
- [Component-Specific Dark Styles](#component-specific-dark-styles)
- [Testing Dark Mode](#testing-dark-mode)

---

## Unified Dark Mode System

### Single Source of Truth

Both desktop and mobile modes use the same dark mode attribute:

```typescript
// ✅ CORRECT - Set on <html> element
document.documentElement.setAttribute('data-bs-theme', 'dark');

// ❌ WRONG - Don't set on body
document.body.setAttribute('data-bs-theme', 'dark');
```

### Dark Mode Service

```typescript
import { Injectable, signal, effect } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DarkModeService {
  // Signal for reactive dark mode state
  darkMode = signal<boolean>(false);
  
  constructor() {
    // Load saved preference
    const saved = localStorage.getItem('appDarkMode');
    if (saved) {
      this.darkMode.set(saved === 'true');
    } else {
      // Check system preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.darkMode.set(prefersDark);
    }
    
    // Effect to sync with DOM
    effect(() => {
      this.applyDarkMode(this.darkMode());
    });
    
    // Listen for system preference changes
    window.matchMedia('(prefers-color-scheme: dark)')
      .addEventListener('change', (e) => {
        if (!localStorage.getItem('appDarkMode')) {
          this.darkMode.set(e.matches);
        }
      });
  }
  
  toggle(): void {
    const newValue = !this.darkMode();
    this.darkMode.set(newValue);
    localStorage.setItem('appDarkMode', String(newValue));
  }
  
  enable(): void {
    this.darkMode.set(true);
    localStorage.setItem('appDarkMode', 'true');
  }
  
  disable(): void {
    this.darkMode.set(false);
    localStorage.setItem('appDarkMode', 'false');
  }
  
  private applyDarkMode(enabled: boolean): void {
    if (enabled) {
      document.documentElement.setAttribute('data-bs-theme', 'dark');
    } else {
      document.documentElement.removeAttribute('data-bs-theme');
      // Or explicitly set light
      // document.documentElement.setAttribute('data-bs-theme', 'light');
    }
  }
}
```

---

## Dark Mode Toggle Component

### Desktop Toggle (Bootstrap)

```typescript
import { Component, inject } from '@angular/core';
import { DarkModeService } from '../../service';

@Component({
  selector: 'app-dark-mode-toggle',
  template: `
    <div class="form-check form-switch">
      <input 
        class="form-check-input" 
        type="checkbox" 
        role="switch"
        id="darkModeSwitch"
        [checked]="darkModeService.darkMode()"
        (change)="darkModeService.toggle()"
      />
      <label class="form-check-label" for="darkModeSwitch">
        <i class="fa fa-moon" *ngIf="!darkModeService.darkMode()"></i>
        <i class="fa fa-sun" *ngIf="darkModeService.darkMode()"></i>
        {{ darkModeService.darkMode() ? 'Dark' : 'Light' }} Mode
      </label>
    </div>
  `
})
export class DarkModeToggleComponent {
  darkModeService = inject(DarkModeService);
}
```

### Mobile Toggle (Ionic)

```typescript
import { Component, inject } from '@angular/core';
import { DarkModeService } from '../../service';

@Component({
  selector: 'app-dark-mode-toggle',
  template: `
    <ion-item>
      <ion-icon 
        [name]="darkModeService.darkMode() ? 'moon' : 'sunny'" 
        slot="start"
      ></ion-icon>
      <ion-label>Dark Mode</ion-label>
      <ion-toggle 
        slot="end"
        [checked]="darkModeService.darkMode()"
        (ionChange)="darkModeService.toggle()"
      ></ion-toggle>
    </ion-item>
  `
})
export class DarkModeToggleComponent {
  darkModeService = inject(DarkModeService);
}
```

### Combined Toggle (Works Both Modes)

```typescript
import { Component, inject } from '@angular/core';
import { DarkModeService } from '../../service';
import { AppSettingsService } from '../../service';

@Component({
  selector: 'app-dark-mode-toggle',
  template: `
    <!-- Desktop version -->
    <div *ngIf="appSettings.currentMode === 'desktop'" 
         class="form-check form-switch">
      <input 
        class="form-check-input" 
        type="checkbox" 
        [checked]="darkMode.darkMode()"
        (change)="darkMode.toggle()"
      />
      <label class="form-check-label">
        Dark Mode
      </label>
    </div>
    
    <!-- Mobile version -->
    <ion-item *ngIf="appSettings.currentMode === 'mobile'">
      <ion-icon 
        [name]="darkMode.darkMode() ? 'moon' : 'sunny'" 
        slot="start"
      ></ion-icon>
      <ion-label>Dark Mode</ion-label>
      <ion-toggle 
        slot="end"
        [checked]="darkMode.darkMode()"
        (ionChange)="darkMode.toggle()"
      ></ion-toggle>
    </ion-item>
  `
})
export class DarkModeToggleComponent {
  darkMode = inject(DarkModeService);
  appSettings = inject(AppSettingsService);
}
```

---

## Styling Dark Mode

### Global Dark Mode Styles

```scss
// ✅ CORRECT - Selector on <html>
[data-bs-theme="dark"] {
  // Desktop-specific dark mode
  .app-header {
    background: var(--bs-dark);
    color: var(--bs-light);
  }
  
  .card {
    background: var(--bs-gray-800);
    color: var(--bs-light);
  }
  
  // Mobile-specific dark mode
  ion-toolbar {
    --background: var(--bs-dark);
    --color: var(--bs-light);
  }
  
  ion-content {
    --background: var(--bs-gray-900);
  }
  
  ion-card {
    --background: var(--bs-gray-800);
    --color: var(--bs-light);
  }
}
```

### Dark Mode with CSS Variables

Define consistent variables for both platforms:

```scss
:root {
  // Light mode (default)
  --app-background: #ffffff;
  --app-text: #212529;
  --app-surface: #f8f9fa;
  --app-border: #dee2e6;
}

[data-bs-theme="dark"] {
  // Dark mode
  --app-background: #212529;
  --app-text: #f8f9fa;
  --app-surface: #343a40;
  --app-border: #495057;
}

// Use in components
.my-component {
  background: var(--app-background);
  color: var(--app-text);
  border: 1px solid var(--app-border);
}
```

---

## Component-Specific Dark Styles

### Desktop Component

```scss
// component.scss
.dashboard-widget {
  background: var(--bs-white);
  color: var(--bs-dark);
  border: 1px solid var(--bs-gray-300);
}

[data-bs-theme="dark"] {
  .dashboard-widget {
    background: var(--bs-gray-800);
    color: var(--bs-light);
    border: 1px solid var(--bs-gray-700);
  }
}
```

### Mobile Component

```scss
// component.scss
ion-card.custom-card {
  --background: #ffffff;
  --color: #000000;
}

[data-bs-theme="dark"] {
  ion-card.custom-card {
    --background: #1e1e1e;
    --color: #ffffff;
  }
}
```

### Inline Dark Mode Styles

For quick adjustments without separate dark mode blocks:

```scss
.element {
  background: light-dark(white, black);
  color: light-dark(#212529, #f8f9fa);
  border: 1px solid light-dark(#dee2e6, #495057);
}
```

*Note: `light-dark()` is a newer CSS function with limited support. Check caniuse.com*

---

## Testing Dark Mode

### Manual Testing Checklist

```typescript
// Testing script in browser console
const darkModeTests = {
  // 1. Check attribute location
  attributeOnHtml: document.documentElement.hasAttribute('data-bs-theme'),
  attributeOnBody: document.body.hasAttribute('data-bs-theme'),
  
  // 2. Check value
  currentTheme: document.documentElement.getAttribute('data-bs-theme'),
  
  // 3. Check localStorage
  savedPreference: localStorage.getItem('appDarkMode'),
  
  // 4. Check system preference
  systemPrefersDark: window.matchMedia('(prefers-color-scheme: dark)').matches,
  
  // 5. Check CSS variables
  bgColor: getComputedStyle(document.body).backgroundColor,
  textColor: getComputedStyle(document.body).color,
  
  // 6. Verify toggle works
  toggleTest: () => {
    const before = document.documentElement.getAttribute('data-bs-theme');
    // Trigger toggle
    document.querySelector('[type="checkbox"]')?.click();
    const after = document.documentElement.getAttribute('data-bs-theme');
    return before !== after;
  }
};

console.table(darkModeTests);
```

### Automated Tests

```typescript
describe('DarkModeService', () => {
  let service: DarkModeService;
  
  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DarkModeService);
    localStorage.clear();
  });
  
  it('should create', () => {
    expect(service).toBeTruthy();
  });
  
  it('should toggle dark mode', () => {
    const initialValue = service.darkMode();
    service.toggle();
    expect(service.darkMode()).toBe(!initialValue);
  });
  
  it('should save preference to localStorage', () => {
    service.enable();
    expect(localStorage.getItem('appDarkMode')).toBe('true');
    
    service.disable();
    expect(localStorage.getItem('appDarkMode')).toBe('false');
  });
  
  it('should apply dark mode to document', () => {
    service.enable();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('dark');
    
    service.disable();
    expect(document.documentElement.hasAttribute('data-bs-theme')).toBe(false);
  });
  
  it('should load saved preference', () => {
    localStorage.setItem('appDarkMode', 'true');
    
    const newService = new DarkModeService();
    expect(newService.darkMode()).toBe(true);
  });
});
```

---

## Common Patterns

### Detect Dark Mode in Component

```typescript
export class MyComponent {
  get isDarkMode(): boolean {
    return document.documentElement.getAttribute('data-bs-theme') === 'dark';
  }
  
  // Or use service
  darkMode = inject(DarkModeService);
  
  ngOnInit() {
    // React to changes
    effect(() => {
      if (this.darkMode.darkMode()) {
        console.log('Dark mode enabled');
      } else {
        console.log('Light mode enabled');
      }
    });
  }
}
```

### Conditional Images

```html
<!-- Show different logo based on mode -->
<img 
  *ngIf="!darkMode.darkMode()" 
  src="assets/logo-light.png" 
  alt="Logo"
/>
<img 
  *ngIf="darkMode.darkMode()" 
  src="assets/logo-dark.png" 
  alt="Logo"
/>
```

### CSS-Only Image Switching

```scss
.logo {
  content: url('assets/logo-light.png');
}

[data-bs-theme="dark"] .logo {
  content: url('assets/logo-dark.png');
}
```

```html
<div class="logo"></div>
```

### Theme-Aware Icons

```html
<!-- Desktop -->
<i class="fa" 
   [class.fa-sun]="!darkMode.darkMode()"
   [class.fa-moon]="darkMode.darkMode()">
</i>

<!-- Mobile -->
<ion-icon 
  [name]="darkMode.darkMode() ? 'moon' : 'sunny'">
</ion-icon>
```

---

## Best Practices

### ✅ DO

```scss
// 1. Use CSS variables for consistency
:root {
  --text-color: #000;
  --bg-color: #fff;
}

[data-bs-theme="dark"] {
  --text-color: #fff;
  --bg-color: #000;
}

.element {
  color: var(--text-color);
  background: var(--bg-color);
}

// 2. Test both modes during development
// 3. Use semantic color names
--app-primary-bg: ...;
--app-secondary-bg: ...;

// 4. Provide fallbacks
background: var(--bg-color, white);
```

### ❌ DON'T

```scss
// 1. Don't hardcode colors
.element {
  color: #000; // ❌ Won't change in dark mode
}

// 2. Don't use body selector
body[data-bs-theme="dark"] { // ❌ Wrong element
  ...
}

// 3. Don't forget mobile styles
[data-bs-theme="dark"] {
  .desktop-only { // ❌ Also style Ionic components
    ...
  }
}

// 4. Don't use !important
.element {
  color: white !important; // ❌ Can't be overridden
}
```

---

## Troubleshooting

### Dark mode not applying

```javascript
// Check setup
console.log('Attribute set:', document.documentElement.hasAttribute('data-bs-theme'));
console.log('Current value:', document.documentElement.getAttribute('data-bs-theme'));
console.log('CSS loaded:', !!document.querySelector('link[href*="dark"]'));
```

### Styles partially working

```scss
// Make sure selector is correct
[data-bs-theme="dark"] { // ✅ On <html>
  body { /* styles */ }
}

body[data-bs-theme="dark"] { // ❌ Attribute not on body
  /* Won't work */
}
```

### Toggle not persisting

```typescript
// Check localStorage
console.log(localStorage.getItem('appDarkMode'));

// Verify it's being saved
darkModeService.toggle();
console.log('After toggle:', localStorage.getItem('appDarkMode'));
```

### Color contrast issues

Use browser DevTools to check contrast ratios:
1. Inspect element
2. Look at "Accessibility" panel
3. Check contrast ratio (should be ≥4.5:1 for normal text)

---

## Resources

- [MDN: prefers-color-scheme](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme)
- [Bootstrap Dark Mode](https://getbootstrap.com/docs/5.3/customize/color-modes/)
- [Ionic Dark Mode](https://ionicframework.com/docs/theming/dark-mode)
- [WCAG Contrast Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum.html)
