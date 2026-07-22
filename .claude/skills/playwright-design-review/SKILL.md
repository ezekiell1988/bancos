---
name: playwright-design-review
description: Use when inspecting frontend UI with Playwright for visual design fidelity, responsive layout, horizontal overflow, clipped content, screenshots, mobile/desktop viewport checks, CSS regressions, or when a user says they like using Playwright to see how the design is going. Especially useful for Angular/Ionic/Color Admin screens, mock API responses, and comparing implementation against a reference screenshot.
---

# Playwright Design Review

## Purpose

Use Playwright as a design microscope: render the real app, capture screenshots, detect overflow/clipping, and verify responsive behavior before declaring UI work done.

## Workflow

1. Identify the route, viewport, and data needed to reproduce the UI.
2. Start the dev server if the app needs one. Reuse an existing server when available.
3. If backend data is needed, mock route responses with `page.route()` or use `scripts/design-review.mjs --mock-url ... --mock-file ...`.
4. Capture screenshots at the reference viewport and at least one narrower mobile viewport.
5. Check `document.documentElement.scrollWidth === window.innerWidth` and inspect elements whose bounding boxes leave the viewport.
6. Fix CSS/layout issues. Then:
   - **Build Angular** to compile the changes into `dist/browser/`:
     ```bash
     cd src/VoiceBot.Web && npm run ng -- build --configuration development
     ```
   - **Verify the fix is in the compiled JS** (find the new chunk hash first):
     ```bash
     grep -rl "my-class-or-selector" src/VoiceBot.Web/dist/browser/*.js
     ```
   - **Hard reload in the browser** (clears browser cache by navigating through `about:blank`):
     ```js
     await page.goto('about:blank');
     await page.waitForTimeout(300);
     await page.goto('https://localhost:8000/cleo-chat', { waitUntil: 'networkidle' });
     ```
7. Rerun the same checks (screenshot + overflow check) after the hard reload.
8. Report screenshot path(s), viewport(s), overflow result, and any remaining visual risk.

> **Note:** After a rebuild the chunk filename hash changes (e.g., `chunk-UO7Z3NMH.js` → `chunk-UYMV5KTJ.js`). Always use `grep -rl` to find the new chunk rather than referencing the old filename. See `references/stale-build-debug.md` for the full stale-build checklist.

## Quick Script

Use the bundled script for repeatable checks:

```bash
node .agents/skills/playwright-design-review/scripts/design-review.mjs \
  --url http://127.0.0.1:4210/conversations/116 \
  --viewport 674x1456 \
  --selector app-mobile-purchase-success \
  --out tmp/design-review
```

With mocked API data:

```bash
node .agents/skills/playwright-design-review/scripts/design-review.mjs \
  --url http://127.0.0.1:4210/conversations/116 \
  --viewport 674x1456 \
  --selector app-mobile-purchase-success \
  --mock-url '**/api/backoffice/conversations/116' \
  --mock-file tmp/conversation-116.json \
  --local-storage backoffice_token=mock-token \
  --local-storage 'backoffice_user={"nombre":"QA","telefono":"88888888"}' \
  --out tmp/design-review
```

The script writes a PNG screenshot and a JSON report with:

- viewport/document width
- horizontal overflow status
- first offscreen elements under `--selector`
- selected element bounding box

## Manual Checks

Use inline Playwright when the script is too generic. Minimum useful checks:

```js
const result = await page.evaluate(() => {
  const vw = window.innerWidth;
  const offenders = [...document.querySelectorAll('body *')]
    .map(el => ({ el, r: el.getBoundingClientRect() }))
    .filter(x => x.r.right > vw + 1 || x.r.left < -1)
    .map(x => ({
      tag: x.el.tagName.toLowerCase(),
      cls: x.el.className?.toString?.() ?? '',
      left: Math.round(x.r.left),
      right: Math.round(x.r.right),
      width: Math.round(x.r.width),
      text: x.el.textContent.trim().slice(0, 80),
    }));
  return {
    vw,
    docScroll: document.documentElement.scrollWidth,
    bodyScroll: document.body.scrollWidth,
    offenders: offenders.slice(0, 20),
  };
});
```

## Design Review Heuristics

- **Testing clickable cards (sin botones):** Cuando una card actúa como botón (`role="button"` en el div, sin `<button>` interno), verificar el toggle con:
  ```js
  // Banner/row card — primer item
  await page.locator('.product-card-banner').first().click();
  // Ionic card
  await page.locator('ion-card').first().click();
  // Verificar activación con teclado
  await page.locator('.product-card-banner').first().press('Enter');
  await page.locator('.product-card-banner').first().press('Space');
  ```
  El atributo `aria-pressed` debe cambiar de `"false"` a `"true"` tras el click. El badge de cantidad también debe aparecer.

- Prefer fixing the element that creates overflow over hiding overflow globally.
- Use `minmax(0, 1fr)`, `min-width: 0`, `overflow-wrap`, and ellipsis for grid/flex children with long content.
- Avoid `min-width` on cards inside mobile grids unless the container intentionally scrolls.
- If horizontal scrolling is intentional, make it visually obvious and keep it confined to the inner strip, not the full document.
- Verify fixed footers do not cover important content. Add bottom spacer matching the footer height.
- Capture the same viewport dimensions as the reference image when comparing visual fidelity.

## Abrir el browser integrado de VS Code

Usa las tools `open_browser_page`, `run_playwright_code` y `screenshot_page` del agente (NO el script de terminal) para inspección rápida interactiva.

### Versión web (desktop)

```js
// open_browser_page
{ url: "https://localhost:8000/ruta" }

// Viewport desktop por defecto (suele ser 1280×720).
// Si necesitas un tamaño específico:
await page.setViewportSize({ width: 1280, height: 800 });
await page.reload();
await page.waitForLoadState('networkidle');
```

### Versión mobile — iPhone 14 (390×844)

```js
// 1. Abrir la página (open_browser_page)
{ url: "https://localhost:8000/ruta" }

// 2. Ajustar viewport con run_playwright_code
await page.setViewportSize({ width: 390, height: 844 });
await page.reload();
await page.waitForLoadState('networkidle');

// 3. Capturar screenshot (screenshot_page)
{ pageId: "<id>" }
```

### Viewports de referencia habituales

| Dispositivo | Ancho | Alto |
|-------------|-------|------|
| iPhone SE | 375 | 667 |
| iPhone 14 | 390 | 844 |
| iPhone 14 Pro Max | 430 | 932 |
| Pixel 7 | 412 | 915 |
| iPad Mini | 768 | 1024 |
| Desktop HD | 1280 | 800 |

> **Nota:** `open_browser_page` abre el browser embebido de VS Code. Cada llamada retorna un `pageId` que debes reutilizar en todas las tools subsiguientes (`run_playwright_code`, `screenshot_page`, `navigate_page`, etc.).

## References

Read `references/angular-ionic.md` when auditing Angular/Ionic screens in this repo.

Read `references/stale-build-debug.md` when a Playwright test still shows a UI bug after applying a fix and rebuilding. Covers: duplicate web/mobile components, Angular compiler cache (`.angular/cache`), browser cache hard-reload technique, and `.NET SPA RootPath` mismatch.

Read `cleo-chat/cleo-promos.md` when you need to navigate to the **ViewDailyPromos** step in Cleo Chat. Contains step-by-step Playwright code, rebuild workflow, and implementation notes for the product card layouts.

> **Credenciales de prueba Cleo Chat** (usar siempre estas — no inventar ni pedir otras):
> - Teléfono: **`83681485`**
> - OTP: `475957` *(solo si la sesión expiró — normalmente no se pide)*

Read `cleo-chat/cleo-menu.md` when you need to navigate to the **BrowseMenu** step (menú completo con categorías). Extends `cleo-promos.md` with the extra step: click `button:has-text("Ver menú completo")` + `waitForTimeout(8000)`. Contains the full flow from scratch, category selectors, and screenshots reference.

> Mismas credenciales aplican para el menú: teléfono **`83681485`**.
