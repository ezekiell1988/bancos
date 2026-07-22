# CSS Troubleshooting Guide

Guía completa para diagnosticar y solucionar problemas comunes con la carga dinámica de CSS y utilidades.

## Table of Contents

- [CSS Not Loading](#css-not-loading)
- [Wrong CSS Loaded](#wrong-css-loaded)
- [CSS Not Switching on Resize](#css-not-switching-on-resize)
- [Style Conflicts](#style-conflicts)
- [Dark Mode Issues](#dark-mode-issues)
- [Performance Issues](#performance-issues)
- [Debugging Tools](#debugging-tools)

---

## CSS Not Loading

### Symptoms

- Page appears unstyled
- Only FontAwesome icons visible
- Console shows no CSS errors
- Spinner/loading indicator stuck

### Diagnosis

```javascript
// Check loaded stylesheets
const cssFiles = Array.from(document.querySelectorAll('link[rel="stylesheet"]'))
  .map(link => ({
    id: link.id,
    href: link.href.split('/').pop(),
    loaded: !!link.sheet,
    disabled: link.disabled
  }));

console.table(cssFiles);

// Expected output for desktop:
// { id: 'desktop-dynamic-desktop', href: 'desktop.css', loaded: true, disabled: false }

// Expected output for mobile:
// { id: 'ionic-dynamic-mobile', href: 'mobile.css', loaded: true, disabled: false }
```

### Solutions

#### 1. Check stylesLoaded flag

```typescript
// In component
console.log('Styles loaded:', this.appSettings.stylesLoaded);

// If false, CSS loading failed or is incomplete
```

#### 2. Force CSS reload

```typescript
// In browser console
localStorage.clear();
location.reload();
```

#### 3. Verify CSS files exist

```javascript
// Check if CSS file is accessible
fetch('desktop.css')
  .then(r => console.log('Desktop CSS:', r.ok ? '✅ Found' : '❌ Not found'))
  .catch(e => console.error('Desktop CSS error:', e));

fetch('mobile.css')
  .then(r => console.log('Mobile CSS:', r.ok ? '✅ Found' : '❌ Not found'))
  .catch(e => console.error('Mobile CSS error:', e));
```

#### 4. Check angular.json config

```json
// Verify these are set to inject: false
"styles": [
  {
    "input": "src/styles.css",
    "bundleName": "styles",
    "inject": true  // ✅ Only this should be true
  },
  {
    "input": "src/scss/angular.scss",
    "bundleName": "desktop",
    "inject": false  // ✅ Must be false
  },
  {
    "input": "src/scss/ionic.scss",
    "bundleName": "mobile",
    "inject": false  // ✅ Must be false
  }
]
```

#### 5. Manual load fallback

```typescript
// Emergency CSS load
const link = document.createElement('link');
link.rel = 'stylesheet';
link.href = window.innerWidth > 768 ? 'desktop.css' : 'mobile.css';
link.id = 'emergency-css';
document.head.appendChild(link);
```

---

## Wrong CSS Loaded

### Symptoms

- Desktop showing Ionic styles
- Mobile showing Bootstrap styles
- Body has wrong class (`desktop-mode` vs `ionic-mode`)

### Diagnosis

```javascript
// Check current state
const state = {
  width: window.innerWidth,
  expectedMode: window.innerWidth > 768 ? 'desktop' : 'mobile',
  bodyClass: document.body.classList.contains('desktop-mode') ? 'desktop' 
            : document.body.classList.contains('ionic-mode') ? 'mobile' 
            : 'none',
  desktopCSS: document.querySelectorAll('link[id^="desktop-dynamic"]').length,
  mobileCSS: document.querySelectorAll('link[id^="ionic-dynamic"]').length
};

console.table(state);

// Check for mismatch
if (state.expectedMode !== state.bodyClass) {
  console.error('❌ Mode mismatch detected!');
}

if (state.desktopCSS > 0 && state.mobileCSS > 0) {
  console.error('❌ Both CSS frameworks loaded!');
}
```

### Solutions

#### 1. Clean all styles and reload

```typescript
// Force clean restart
document.querySelectorAll('link[id*="-dynamic-"]').forEach(link => {
  link.remove();
});

// Reset body classes
document.body.classList.remove('desktop-mode', 'ionic-mode');

// Reload
location.reload();
```

#### 2. Check PlatformDetectorService

```typescript
// Verify detection logic
const detector = inject(PlatformDetectorService);
console.log('Detected mode:', detector.getCurrentMode());
console.log('Window width:', window.innerWidth);
console.log('Threshold:', 768);

// Should match
if (window.innerWidth > 768 && detector.getCurrentMode() !== 'desktop') {
  console.error('Detection logic error');
}
```

---

## CSS Not Switching on Resize

### Symptoms

- Resize window crossing 768px threshold
- Body class doesn't update
- CSS doesn't reload
- UI remains in old mode

### Diagnosis

```javascript
// Test resize detection
let resizeCount = 0;
window.addEventListener('resize', () => {
  resizeCount++;
  console.log(`Resize #${resizeCount}:`, {
    width: window.innerWidth,
    mode: document.body.classList.contains('desktop-mode') ? 'desktop' : 'mobile'
  });
});

// Manually resize and watch console
```

### Solutions

#### 1. Verify resize listener exists

```typescript
// Check if PlatformDetectorService is listening
// Should be in constructor or ngOnInit
fromEvent(window, 'resize')
  .pipe(debounceTime(300))
  .subscribe(() => {
    this.handleResize();
  });
```

#### 2. Check threshold logic

```typescript
// Ensure threshold is correctly set
const MOBILE_BREAKPOINT = 768; // ✅ Standard

// Verify comparison
if (window.innerWidth <= MOBILE_BREAKPOINT) {
  // Mobile
} else {
  // Desktop
}
```

#### 3. Force mode switch

```typescript
// Manual mode switch in console
// For desktop
document.querySelectorAll('link[id^="ionic-dynamic"]').forEach(l => l.remove());
document.body.classList.remove('ionic-mode');
document.body.classList.add('desktop-mode');
// Then load desktop CSS

// For mobile
document.querySelectorAll('link[id^="desktop-dynamic"]').forEach(l => l.remove());
document.body.classList.remove('desktop-mode');
document.body.classList.add('ionic-mode');
// Then load mobile CSS
```

---

## Style Conflicts

### Symptoms

- Elements styled incorrectly
- Padding/margin doubled
- Colors wrong
- Layout broken

### Diagnosis

```javascript
// Check for CSS specificity conflicts
const element = document.querySelector('.problem-element');
const styles = window.getComputedStyle(element);

console.log({
  padding: styles.padding,
  margin: styles.margin,
  display: styles.display,
  background: styles.backgroundColor
});

// Check which classes are applied
console.log('Classes:', element.className);
```

### Solutions

#### 1. Remove conflicting classes

```html
<!-- ❌ BAD - Mixing frameworks -->
<div class="card ion-padding">

<!-- ✅ GOOD - One framework per element -->
<div class="card p-3"><!-- Desktop -->
<ion-card class="ion-padding"><!-- Mobile -->
```

#### 2. Use conditional rendering

```html
<!-- ✅ BEST - Separate elements for each mode -->
<div *ngIf="isDesktop" class="card p-3">
  Desktop version
</div>

<ion-card *ngIf="isMobile" class="ion-padding">
  Mobile version
</ion-card>
```

#### 3. Check CSS load order

```javascript
// Verify only one framework CSS is loaded
const cssLinks = document.querySelectorAll('link[rel="stylesheet"]');
const frameworks = {
  desktop: 0,
  mobile: 0
};

cssLinks.forEach(link => {
  if (link.href.includes('desktop')) frameworks.desktop++;
  if (link.href.includes('mobile')) frameworks.mobile++;
});

console.log('Framework CSS count:', frameworks);
// Should show: { desktop: 1, mobile: 0 } OR { desktop: 0, mobile: 1 }

if (frameworks.desktop > 0 && frameworks.mobile > 0) {
  console.error('❌ Both frameworks loaded - conflict!');
}
```

---

## Dark Mode Issues

### Symptoms

- Dark mode toggle doesn't work
- Colors don't change
- Partial dark mode (some elements dark, others light)

### Diagnosis

```javascript
// Check dark mode setup
const darkModeCheck = {
  attribute: document.documentElement.getAttribute('data-bs-theme'),
  attributeLocation: document.documentElement.hasAttribute('data-bs-theme') 
    ? 'html' 
    : document.body.hasAttribute('data-bs-theme') 
    ? 'body' 
    : 'none',
  localStorage: localStorage.getItem('appDarkMode'),
  systemPreference: window.matchMedia('(prefers-color-scheme: dark)').matches
};

console.table(darkModeCheck);

// Should show:
// attribute: 'dark' or null
// attributeLocation: 'html'
// localStorage: 'true' or 'false'
```

### Solutions

#### 1. Verify attribute location

```javascript
// ✅ CORRECT
document.documentElement.setAttribute('data-bs-theme', 'dark');

// ❌ WRONG
document.body.setAttribute('data-bs-theme', 'dark');

// Fix if wrong
if (document.body.hasAttribute('data-bs-theme')) {
  const value = document.body.getAttribute('data-bs-theme');
  document.body.removeAttribute('data-bs-theme');
  document.documentElement.setAttribute('data-bs-theme', value);
}
```

#### 2. Check CSS selectors

```scss
// ✅ CORRECT
[data-bs-theme="dark"] {
  .element { ... }
}

// ❌ WRONG
body[data-bs-theme="dark"] {
  .element { ... }
}
```

#### 3. Verify dark mode CSS is loaded

```javascript
// Desktop: Check if desktop.css includes dark mode styles
// Mobile: Check if mobile.css includes dark mode palette

// Test by toggling
document.documentElement.setAttribute('data-bs-theme', 'dark');
// Colors should change immediately
```

---

## Performance Issues

### Symptoms

- Slow CSS loading
- Lag when switching modes
- Page freezes during resize
- High memory usage

### Diagnosis

```javascript
// Measure CSS load time
const startTime = performance.now();

const link = document.createElement('link');
link.rel = 'stylesheet';
link.href = 'desktop.css';
link.onload = () => {
  const loadTime = performance.now() - startTime;
  console.log(`CSS loaded in ${loadTime.toFixed(2)}ms`);
};
document.head.appendChild(link);

// Good: < 500ms
// Acceptable: 500-1000ms
// Slow: > 1000ms
```

### Solutions

#### 1. Add debounce to resize

```typescript
// ✅ Already implemented
fromEvent(window, 'resize')
  .pipe(debounceTime(300)) // Wait 300ms after last resize
  .subscribe(() => {
    this.handleResize();
  });
```

#### 2. Preload CSS on hover

```typescript
// Preload opposite mode CSS when user hovers near edge
// (Advanced optimization)
@HostListener('mousemove', ['$event'])
onMouseMove(event: MouseEvent) {
  const threshold = 768;
  const currentWidth = window.innerWidth;
  
  // If near threshold and in desktop mode, preload mobile
  if (currentWidth > threshold && currentWidth < threshold + 50) {
    this.preloadCSS('mobile.css');
  }
  // If near threshold and in mobile mode, preload desktop
  else if (currentWidth <= threshold && currentWidth > threshold - 50) {
    this.preloadCSS('desktop.css');
  }
}

preloadCSS(href: string) {
  const link = document.createElement('link');
  link.rel = 'preload';
  link.as = 'style';
  link.href = href;
  document.head.appendChild(link);
}
```

#### 3. Optimize CSS file size

```bash
# Check CSS file sizes
ls -lh dist/*.css

# desktop.css should be ~2-3MB
# mobile.css should be ~1-2MB

# If larger, consider:
# 1. Removing unused CSS
# 2. Enabling purgeCSS
# 3. Minification
```

---

## Debugging Tools

### DevTools CSS Inspector

```javascript
// Add to bookmark for quick debugging
javascript:(function(){
  const state = {
    mode: document.body.classList.contains('desktop-mode') ? 'Desktop' : 'Mobile',
    width: window.innerWidth + 'px',
    theme: document.documentElement.getAttribute('data-bs-theme') || 'light',
    desktopCSS: document.querySelectorAll('link[id^="desktop-dynamic"]').length,
    mobileCSS: document.querySelectorAll('link[id^="ionic-dynamic"]').length,
    stylesLoaded: 'Check appSettings.stylesLoaded',
    totalCSS: document.querySelectorAll('link[rel="stylesheet"]').length
  };
  console.table(state);
  alert('Check console for CSS state');
})();
```

### CSS Validation Script

```typescript
// Add to app.component.ts for development
ngOnInit() {
  if (!environment.production) {
    this.validateCSSState();
    
    // Re-validate on resize
    fromEvent(window, 'resize')
      .pipe(debounceTime(1000))
      .subscribe(() => this.validateCSSState());
  }
}

validateCSSState() {
  const width = window.innerWidth;
  const expectedMode = width > 768 ? 'desktop' : 'mobile';
  const bodyClass = document.body.classList.contains('desktop-mode') ? 'desktop'
                  : document.body.classList.contains('ionic-mode') ? 'mobile'
                  : 'unknown';
  
  const desktopCSS = document.querySelectorAll('link[id^="desktop-dynamic"]').length;
  const mobileCSS = document.querySelectorAll('link[id^="ionic-dynamic"]').length;
  
  // Validate
  const errors = [];
  
  if (expectedMode !== bodyClass) {
    errors.push(`Mode mismatch: expected ${expectedMode}, got ${bodyClass}`);
  }
  
  if (expectedMode === 'desktop' && desktopCSS === 0) {
    errors.push('Desktop mode but no desktop CSS loaded');
  }
  
  if (expectedMode === 'mobile' && mobileCSS === 0) {
    errors.push('Mobile mode but no mobile CSS loaded');
  }
  
  if (desktopCSS > 0 && mobileCSS > 0) {
    errors.push('Both desktop and mobile CSS loaded (conflict)');
  }
  
  if (errors.length > 0) {
    console.error('❌ CSS State Validation Errors:', errors);
  } else {
    console.log('✅ CSS State Valid');
  }
}
```

### Network Monitor

```javascript
// Monitor CSS requests
const observer = new PerformanceObserver((list) => {
  list.getEntries().forEach((entry) => {
    if (entry.name.endsWith('.css')) {
      console.log('CSS loaded:', {
        name: entry.name.split('/').pop(),
        duration: entry.duration.toFixed(2) + 'ms',
        size: entry.transferSize ? (entry.transferSize / 1024).toFixed(2) + 'KB' : 'cached'
      });
    }
  });
});

observer.observe({ entryTypes: ['resource'] });
```

### Console Helper Functions

```javascript
// Add to browser console for debugging

// Quick state check
window.cssState = () => {
  console.table({
    width: window.innerWidth,
    mode: document.body.classList.contains('desktop-mode') ? 'desktop' : 'mobile',
    darkMode: document.documentElement.getAttribute('data-bs-theme') === 'dark',
    desktopCSS: document.querySelectorAll('link[id^="desktop-dynamic"]').length,
    mobileCSS: document.querySelectorAll('link[id^="ionic-dynamic"]').length
  });
};

// Force mode switch
window.forceDesktop = () => {
  document.querySelectorAll('link[id^="ionic-dynamic"]').forEach(l => l.remove());
  document.body.classList.remove('ionic-mode');
  document.body.classList.add('desktop-mode');
  console.log('Switched to desktop mode (reload CSS manually)');
};

window.forceMobile = () => {
  document.querySelectorAll('link[id^="desktop-dynamic"]').forEach(l => l.remove());
  document.body.classList.remove('desktop-mode');
  document.body.classList.add('ionic-mode');
  console.log('Switched to mobile mode (reload CSS manually)');
};

// Clean everything
window.cleanCSS = () => {
  document.querySelectorAll('link[id*="-dynamic-"]').forEach(l => l.remove());
  document.body.classList.remove('desktop-mode', 'ionic-mode');
  console.log('All dynamic CSS removed. Reload page.');
};

// Usage:
// cssState()
// forceDesktop()
// forceMobile()
// cleanCSS()
```

---

## Common Error Messages

### "Cannot read property 'sheet' of null"

**Cause:** Trying to access stylesheet before it's loaded

**Solution:**
```typescript
// Wait for load
link.onload = () => {
  if (link.sheet) {
    // Safe to use
  }
};
```

### "ResizeObserver loop limit exceeded"

**Cause:** Too many resize events

**Solution:**
```typescript
// Add debounce
fromEvent(window, 'resize')
  .pipe(debounceTime(300))
  .subscribe(...);
```

### "CSS file not found (404)"

**Cause:** Build didn't create CSS file or path wrong

**Solution:**
```bash
# Check dist folder
ls dist/*.css

# Should show:
# desktop.css
# mobile.css
# styles.css

# If missing, rebuild
ng build
```

---

## Prevention Checklist

Before deployment, verify:

- [ ] `angular.json` has correct `inject: false` for desktop/mobile CSS
- [ ] `styles.css` has NO framework imports
- [ ] PlatformDetectorService has resize listener with debounce
- [ ] Both `desktop.css` and `mobile.css` exist in dist/
- [ ] Dark mode attribute on `<html>`, not `<body>`
- [ ] No mixed framework classes in templates
- [ ] AppSettings service tracks `stylesLoaded` correctly
- [ ] CSS file sizes reasonable (<5MB each)
- [ ] LocalStorage used for dark mode persistence
- [ ] Tested resize across 768px threshold
- [ ] Tested dark mode toggle in both modes
- [ ] No console errors on load
- [ ] No console errors on resize
- [ ] Performance acceptable (<1s CSS load)

---

## Emergency Reset

If everything is broken:

```javascript
// Nuclear option - reset everything
localStorage.clear();
sessionStorage.clear();
document.querySelectorAll('link[id*="-dynamic-"]').forEach(l => l.remove());
document.body.className = '';
document.documentElement.removeAttribute('data-bs-theme');
location.reload(true); // Force reload from server
```

---

## Getting Help

When reporting CSS issues, include:

```javascript
// Run this and include output
const diagnostics = {
  userAgent: navigator.userAgent,
  screenSize: `${window.screen.width}x${window.screen.height}`,
  windowSize: `${window.innerWidth}x${window.innerHeight}`,
  mode: document.body.className,
  darkMode: document.documentElement.getAttribute('data-bs-theme'),
  cssFiles: Array.from(document.querySelectorAll('link[rel="stylesheet"]'))
    .map(l => ({ id: l.id, href: l.href.split('/').pop(), loaded: !!l.sheet })),
  localStorage: Object.keys(localStorage).reduce((obj, key) => {
    obj[key] = localStorage[key];
    return obj;
  }, {}),
  errors: performance.getEntriesByType('resource')
    .filter(r => r.name.endsWith('.css') && r.transferSize === 0)
};

console.log(JSON.stringify(diagnostics, null, 2));
```

Copy the console output when asking for help.
