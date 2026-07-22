# Debugging Stale Builds in Angular + .NET Static SPA

Use this reference when Playwright shows a UI bug that is **not explained by the current source files** — i.e., the code looks correct but the browser still renders the wrong thing.

## Symptoms

- A template condition (`@if`, `*ngIf`) is correct in the `.html` source but the compiled JS still contains the old condition.
- After a rebuild, the button/element you expected to disappear is still visible in Playwright.
- `grep` on the source confirms the fix; `grep` on `dist/browser/*.js` shows the OLD condition.

## Root Causes (in order of frequency)

### 1. Duplicate component (web vs mobile)

Angular apps that support both web and mobile often have **two separate copies** of the same component:

```
src/app/features/feature/components/my-component/        ← web
src/app/features/feature/mobile/components/my-component/ ← mobile
```

The build will include **whichever copy is imported** by the active route/module. If only the web copy was edited, the mobile copy still compiles the old template.

**Fix:** Always search both paths when editing a component:
```bash
find src -name "my-component.component.html"
```
Apply the same logic change to both copies.

> **Cleo Chat product cards (2026-05-23 onward):** La carpeta `components/` (web) fue eliminada por completo. Solo existe `mobile/components/` como fuente de verdad para: `cleo-product-card`, `cleo-product-group`, `cleo-product-grid`, `cleo-product-principal`, `cleo-chat-widget`, `cleo-avatar`, `chat-bubble`, `chat-options`. El dev page `/dev/cleo-products` también fue migrado a `MobileCleoProductGroupComponent`. No hay riesgo de dual-copy para estos componentes.

### 2. Angular compiler cache (`.angular/cache`)

Angular CLI caches compiled templates in `.angular/cache/`. If a file timestamp is unchanged (e.g., after a `git stash pop` or a copy that preserves mtime), the cache may serve the old compiled output even though the file content changed.

**Fix:** Delete the cache and rebuild:
```bash
rm -rf .angular/cache && npm run ng -- build --configuration development
```

### 3. Browser cache (development builds use predictable filenames)

Angular development builds output `chunk-XXXXXXXX.js` with **content-derived hashes**. If the compiled output hash matches what the browser already cached, it may skip re-fetching.

**Fix (Playwright):** Navigate to `about:blank` before going to the target URL to force a fresh load:
```javascript
await page.goto('about:blank');
await page.waitForTimeout(300);
await page.goto('https://localhost:8000/cleo-chat', { waitUntil: 'networkidle' });
```

Alternatively, intercept all JS requests to disable cache:
```javascript
await page.route('**/*.js', route =>
  route.continue({ headers: { ...route.request().headers(), 'pragma': 'no-cache', 'cache-control': 'no-cache' } })
);
```

> **Note:** `Storage.getCookies` is not available in all Playwright contexts. Use the navigation approach instead.

### 4. Wrong SPA RootPath (.NET serving old files)

If `appsettings.json` `Spa:RootPath` points to a path different from Angular's `outputPath` in `angular.json`, the .NET server serves files from a stale location.

**Verify:**
```bash
# Angular output location
grep -r "outputPath" src/VoiceBot.Web/angular.json

# .NET SPA root
grep -r "RootPath" src/VoiceBot.Api/appsettings.Development.json
```

Both should resolve to the same physical directory. In this repo:
- Angular outputs to: `src/VoiceBot.Web/dist/browser`
- .NET serves from: `../VoiceBot.Web/dist/browser` (relative to `ContentRootPath`)

These are equivalent. If they ever diverge, the .NET server will serve stale files on every request (it uses `PhysicalFileProvider` — no in-memory caching, but reads from disk at the path configured at startup).

## Verification Checklist

After editing a template condition and rebuilding:

```bash
# 1. Confirm the source has the fix
grep "myCondition" src/.../my-component.component.html

# 2. Find ALL copies of the component (web + mobile)
find src -name "my-component.component.html"

# 3. Confirm the compiled JS has the fix (in BOTH chunks if there are two copies)
grep -l "my-component\|ClassName" dist/browser/*.js | xargs grep -o 'conditional.*-1'

# 4. If compiled JS still shows old condition, clear cache and rebuild
rm -rf .angular/cache && npm run ng -- build --configuration development

# 5. After rebuild, re-verify compiled JS
grep -l "my-component\|ClassName" dist/browser/*.js | xargs grep -o 'conditional.*-1'
```

## Quick Playwright Debug Snippet

Use this to verify the DOM after fresh navigation:

```javascript
await page.goto('about:blank');
await page.waitForTimeout(300);
await page.goto('https://localhost:8000/TARGET_ROUTE', { waitUntil: 'networkidle' });
// ... navigate to the UI state you want to verify ...
const count = await page.getByText('Button Text').count();
return JSON.stringify({ count }); // expect 0 if button should be hidden
```

## Case Study: "Confirmar selección" button (2026-05-23)

**Bug:** "Confirmar selección" footer button appeared on promo banners (`displayType: "normal"`) even after adding `&& group().displayType !== 'normal'` to the `@if` condition in the template.

**Investigation:**
1. Source file had the correct condition ✅
2. `rm -rf .angular/cache` + rebuild → JS still showed old condition ❌
3. Found that there are **two separate component copies**:
   - `components/cleo-product-group/` — web version (was fixed)
   - `mobile/components/cleo-product-group/` — mobile version (was NOT fixed)
4. The Playwright page was using the mobile version (Ionic route).
5. Applied same `@if` condition fix + auto-confirm `onToggle()` to the mobile copy.
6. Rebuild → both chunks now compiled with `!== "normal"` ✅
7. Playwright confirmed: `confirmBtn: 0` (button no longer visible) ✅

**Lesson:** In this repo, always `find src -name "component-name*.html"` to locate ALL copies before assuming a single edit is sufficient.

**Follow-up (2026-05-23 — consolidación):** Después del fix, se eliminó toda la carpeta web `components/` de cleo-chat. Los tipos `ProductCardVariant` y `ChainInfo` (que vivían en los componentes web) fueron migrados a los archivos de modelos compartidos:
- `ProductCardVariant` → `models/chat-product-card.type.ts`
- `ChainInfo` → `models/chat-product-group.type.ts`

Si se buscan esos tipos en el futuro, están en `models/`, no en archivos de componentes.
