# Angular 22 — Colores y Theming desde Environment

## Estrategia: CSS Custom Properties inyectadas desde `environment.ts`

Los colores vienen de `environment.ts` y se aplican como **CSS custom properties** en el
`<html>` en el bootstrap de la app. El SCSS solo hace referencia a `var(--color-*)`.
Cambiar de tema en prod = solo cambiar `environment.prod.ts`.

---

## 1. Definir colores en environment

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',

  // Paleta de colores del proyecto
  colors: {
    primary:         '#1a73e8',
    primaryDark:     '#1557b0',
    primaryLight:    '#4a90e2',
    primaryContrast: '#ffffff',

    secondary:         '#34a853',
    secondaryDark:     '#1e8e3e',
    secondaryLight:    '#66bb6a',
    secondaryContrast: '#ffffff',

    accent:          '#fbbc04',
    accentDark:      '#f29900',
    accentContrast:  '#000000',

    danger:          '#ea4335',
    dangerDark:      '#c62828',
    dangerContrast:  '#ffffff',

    warning:         '#ff9800',
    warningContrast: '#000000',

    success:         '#34a853',
    successContrast: '#ffffff',

    // Superficies
    background:      '#f8f9fa',
    surface:         '#ffffff',
    surfaceAlt:      '#f1f3f4',

    // Texto
    textPrimary:     '#202124',
    textSecondary:   '#5f6368',
    textDisabled:    '#9aa0a6',

    // Bordes
    border:          '#dadce0',
    borderFocus:     '#1a73e8',

    // Sidebar / navbar (pueden diferir del primary)
    sidebarBg:       '#1a1a2e',
    sidebarText:     '#e8eaed',
    sidebarActive:   '#1a73e8',
    navbarBg:        '#ffffff',
    navbarText:      '#202124',
  },
} as const;
```

```typescript
// src/environments/environment.prod.ts
import { environment as base } from './environment';

export const environment = {
  ...base,
  production: true,
  apiUrl: 'https://api.marketing1on1.com',

  // Sobreescribir colores para producción si difieren
  colors: {
    ...base.colors,
    primary:         '#0d47a1',
    primaryDark:     '#002171',
    primaryLight:    '#5472d3',
  },
} as const;
```

---

## 2. Inyectar CSS custom properties en bootstrap

```typescript
// src/app/app.config.ts
import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { environment } from '../environments/environment';

function injectCssTokens(): () => void {
  return () => {
    const root = document.documentElement;
    const colors = environment.colors;

    // Inyectar cada color como CSS custom property
    (Object.entries(colors) as [string, string][]).forEach(([key, value]) => {
      // camelCase → kebab-case: primaryDark → --color-primary-dark
      const cssVar = '--color-' + key.replace(/([A-Z])/g, '-$1').toLowerCase();
      root.style.setProperty(cssVar, value);
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    // ... otros providers
    {
      provide: APP_INITIALIZER,
      useFactory: injectCssTokens,
      multi: true,
    },
  ],
};
```

---

## 3. Alternativa: servicio de theming con signals

Permite cambiar tema en runtime (multi-tenant, modo oscuro, etc.).

```typescript
// src/app/core/services/theme.ts
import { Service, signal, effect } from '@angular/core';
import { environment } from '../../../environments/environment';

export type ColorPalette = typeof environment.colors;

@Service()
export class Theme {
  private _colors = signal<ColorPalette>(environment.colors);

  readonly colors = this._colors.asReadonly();

  constructor() {
    // Aplicar al DOM cada vez que cambien los colores
    effect(() => {
      this.applyToDocument(this._colors());
    });
  }

  setColors(palette: Partial<ColorPalette>): void {
    this._colors.update(current => ({ ...current, ...palette }));
  }

  private applyToDocument(colors: ColorPalette): void {
    const root = document.documentElement;
    (Object.entries(colors) as [string, string][]).forEach(([key, value]) => {
      const cssVar = '--color-' + key.replace(/([A-Z])/g, '-$1').toLowerCase();
      root.style.setProperty(cssVar, value);
    });
  }
}
```

---

## 4. SCSS — solo referencias a `var(--color-*)`

```scss
// src/styles/_tokens.scss
// Este archivo documenta los tokens; los valores vienen del JS runtime.
// NUNCA hardcodear hex aquí — siempre usar var(--color-*)

:root {
  // Tipografía (estas SÍ son estáticas)
  --font-family-base: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-family-mono: 'JetBrains Mono', 'Fira Code', monospace;

  --font-size-xs:   0.75rem;   // 12px
  --font-size-sm:   0.875rem;  // 14px
  --font-size-base: 1rem;      // 16px
  --font-size-lg:   1.125rem;  // 18px
  --font-size-xl:   1.25rem;   // 20px
  --font-size-2xl:  1.5rem;    // 24px

  // Espaciado
  --space-1:  0.25rem;   // 4px
  --space-2:  0.5rem;    // 8px
  --space-3:  0.75rem;   // 12px
  --space-4:  1rem;      // 16px
  --space-6:  1.5rem;    // 24px
  --space-8:  2rem;      // 32px
  --space-12: 3rem;      // 48px

  // Radios
  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 12px;
  --radius-xl: 16px;
  --radius-full: 9999px;

  // Sombras
  --shadow-sm:  0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-md:  0 4px 6px rgba(0, 0, 0, 0.07);
  --shadow-lg:  0 10px 15px rgba(0, 0, 0, 0.1);

  // Transiciones
  --transition-fast:   150ms ease;
  --transition-base:   250ms ease;
  --transition-slow:   400ms ease;

  // Z-index scale
  --z-dropdown: 1000;
  --z-sticky:   1020;
  --z-overlay:  1040;
  --z-modal:    1050;
  --z-toast:    1070;
}
```

```scss
// src/styles/styles.scss
@use 'tokens';
@use 'reset';

// Aplicar colores base al body (los var() ya están en :root desde JS)
body {
  background-color: var(--color-background);
  color: var(--color-text-primary);
  font-family: var(--font-family-base);
  font-size: var(--font-size-base);
  line-height: 1.5;
}

a {
  color: var(--color-primary);
  &:hover { color: var(--color-primary-dark); }
}
```

---

## 5. Uso en componentes

```scss
// src/app/features/invoices/pages/list/list.scss

:host {
  display: block;
  padding: var(--space-6);
}

.invoice-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--space-4);
  box-shadow: var(--shadow-sm);
  transition: box-shadow var(--transition-fast);

  &:hover {
    box-shadow: var(--shadow-md);
  }

  &.is-overdue {
    border-left: 4px solid var(--color-danger);
  }

  &.is-paid {
    border-left: 4px solid var(--color-success);
  }
}

.invoice-amount {
  color: var(--color-primary);
  font-weight: 600;
}

.badge {
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);

  &.badge-success {
    background: var(--color-success);
    color: var(--color-success-contrast);
  }

  &.badge-danger {
    background: var(--color-danger);
    color: var(--color-danger-contrast);
  }
}
```

---

## 6. TypeScript helper — acceder a colores en código

```typescript
// src/app/core/services/theme.ts — método helper

getColor(key: keyof ColorPalette): string {
  return this._colors()[key];
}

// O directamente desde environment (solo lectura, sin cambios en runtime)
import { environment } from '../../../environments/environment';

const primaryColor = environment.colors.primary;
```

---

## 7. CSS custom properties en componentes via HostBinding

```typescript
import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // Inyectar color como CSS variable local al componente
    '[style.--badge-bg]':    'bgColor()',
    '[style.--badge-color]': 'textColor()',
  },
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-full);
      background: var(--badge-bg);
      color: var(--badge-color);
    }
  `,
  template: `<ng-content />`,
})
export class StatusBadge {
  type = input<'success' | 'danger' | 'warning' | 'info'>('info');

  bgColor = computed(() => {
    const map = {
      success: 'var(--color-success)',
      danger:  'var(--color-danger)',
      warning: 'var(--color-warning)',
      info:    'var(--color-primary)',
    };
    return map[this.type()];
  });

  textColor = computed(() => {
    const map = {
      success: 'var(--color-success-contrast)',
      danger:  'var(--color-danger-contrast)',
      warning: 'var(--color-warning-contrast)',
      info:    'var(--color-primary-contrast)',
    };
    return map[this.type()];
  });
}
```

---

## 8. Modo oscuro desde environment

```typescript
// environment.ts — soporte multi-tema
export const environment = {
  // ...
  theme: {
    defaultMode: 'light' as 'light' | 'dark' | 'auto',
    dark: {
      background:    '#121212',
      surface:       '#1e1e1e',
      surfaceAlt:    '#2a2a2a',
      textPrimary:   '#e8eaed',
      textSecondary: '#9aa0a6',
      border:        '#3c3c3c',
    },
  },
};
```

```typescript
// theme service — toggle
setMode(mode: 'light' | 'dark'): void {
  document.documentElement.setAttribute('data-theme', mode);
  if (mode === 'dark') {
    const darkColors = environment.theme.dark;
    Object.entries(darkColors).forEach(([key, value]) => {
      const cssVar = '--color-' + key.replace(/([A-Z])/g, '-$1').toLowerCase();
      document.documentElement.style.setProperty(cssVar, value);
    });
  } else {
    this.applyToDocument(environment.colors);
  }
}
```

---

## Reglas

1. **NUNCA** hardcodear colores hex en SCSS de componentes — siempre `var(--color-*)`.
2. **NUNCA** importar `environment` directamente en componentes de feature — usar el `Theme` service.
3. Los tokens de **tipografía, espaciado, radios y sombras** sí pueden ser estáticos en `_tokens.scss`.
4. Los tokens de **color** siempre vienen del environment, inyectados al `:root` en bootstrap.
5. Para componentes que necesiten colores dinámicos, pasarlos como CSS variables locales via `host`.
