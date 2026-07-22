# Playwright: Navegar hasta las Opciones Requeridas en Cleo Chat

Guía para llegar al paso **requiredOptions** (detalle de producto con opciones requeridas) partiendo desde el paso **BrowseMenu** con la categoría **PROMOCIONES** ya activa.

---

## Pre-requisito

Completar primero los pasos 1–7 de `cleo-menu.md` para llegar al menú completo.  
Al entrar en BrowseMenu, la primera categoría activa por defecto es **PROMOCIONES** — no es necesario cambiar de categoría.

---

## Paso 10 — Click en la primera promo del menú

Los productos de la categoría PROMOCIONES se renderizan como tarjetas `normal` (`product-card-banner` o `product-card-fullbanner`). Hacer click en cualquiera de ellas selecciona el producto y, al ser `maxSelection=1`, emite inmediatamente `addToCart` → el servidor transiciona al paso `requiredOptions`.

```js
// Click en la primera card de la categoría PROMOCIONES
// (que es la primera categoría seleccionada al entrar en browse-menu)
const firstCard = page.locator(
  '.product-card-banner, .product-card-fullbanner'
).first();
await firstCard.click();
await page.waitForTimeout(8000);
```

**Selector alternativo** (por nombre de producto):
```js
// Si se conoce el nombre del producto, usar aria-label
await page.click('[aria-label^="Agregar"]');
await page.waitForTimeout(8000);
```

**Qué ocurre tras el click:**
1. El componente `cleo-product-group` emite `selectionUpdate` → WS `selection_update`
2. Al ser `displayType='normal'` y `selectedIds.length > 0`, emite también `addToCart`
3. El servidor recibe `add_to_cart` y transiciona al step `ChatRequiredOptionsStep`
4. El bot responde con el mensaje de personalización del producto
5. El widget cambia a `isProductDetail()=true` → se muestra `.mobile-cleo-widget__product-detail`

---

## Verificar que se llegó al detalle del producto

```js
// El contenedor de detalle de producto debe existir
await page.waitForSelector('.mobile-cleo-widget__product-detail', { timeout: 12000 });

// Verificar que hay al menos un grupo de opciones requeridas (ion-card)
const groups = await page.$$('.product-detail .group-card');
console.log('grupos de opciones:', groups.length);
// → ≥ 1 (ej: "¿En qué tamaño? Selecciona 1 (obligatorio)")

// Verificar texto del bot
const lastBubble = await page.$eval(
  'app-mobile-cleo-speech-bubble',
  el => el.textContent?.trim()
);
console.log('último mensaje de Cleo:', lastBubble);
```

---

## Paso 11 — Seleccionar una opción requerida

Las opciones requeridas se renderizan como `.product-small` (variant `small`) o como la grid/cuadrícula según el `displayType` del grupo. Cada opción es un `<button class="product-small">`.

```js
// Seleccionar la primera opción disponible del primer grupo requerido
const firstOption = page.locator('.product-small').first();
await firstOption.click();
await page.waitForTimeout(2000);

// Verificar que la opción quedó seleccionada (clase is-selected o aria-pressed="true")
const isSelected = await firstOption.getAttribute('aria-pressed');
console.log('seleccionado:', isSelected); // → "true"
```

**Si el grupo tiene botón "Confirmar selección":**
```js
// Aparece cuando minSelection > 0 y displayType ≠ 'normal' o 'seleccionCombo'
await page.click('ion-button:has-text("Confirmar selección")');
await page.waitForTimeout(5000);
```

---

## Paso 12 — Tomar screenshot del detalle

```js
screenshot_page(pageId)
```

---

## Selectores clave del detalle de producto

| Elemento | Selector |
|----------|----------|
| Contenedor de detalle | `.mobile-cleo-widget__product-detail` |
| Wrapper interno | `.product-detail` |
| Tarjeta de grupo de opciones | `.product-detail .group-card` |
| Título del grupo | `.group-title` |
| Badge de hint | `.hint-badge` (ej: "Selecciona 1 (obligatorio)") |
| Opción small | `.product-small` |
| Opción small seleccionada | `.product-small.is-selected` |
| Banner lateral (card normal) | `.product-card-banner` |
| Banner full (card normal con imagen) | `.product-card-fullbanner` |
| Botón confirmar selección | `ion-button:has-text("Confirmar selección")` |
| Debug badges _(solo dev)_ | `.dev-flags` |

---

## Flujo completo resumido (Playwright)

```js
// === Desde cero hasta las opciones requeridas ===

// 1. Abrir y esperar input
await page.reload({ waitUntil: 'networkidle', timeout: 30000 });
await page.setViewportSize({ width: 390, height: 844 });
await page.waitForSelector('input[placeholder*="Escribe"]:not([disabled])', { timeout: 20000 });

// 2. Autenticación
await page.fill('input[placeholder*="Escribe"]', '83681485');
await page.keyboard.press('Enter');
await page.waitForTimeout(6000);

// 3. Hacer un pedido
await page.click('button:has-text("Hacer un pedido")');
await page.waitForTimeout(5000);

// 4. Envío a domicilio
await page.click('button:has-text("Envío a domicilio")');
await page.waitForTimeout(5000);

// 5. Confirmar dirección
await page.click('button:has-text("Sí, esa")');
await page.waitForTimeout(7000);
// → Step ViewDailyPromos activo

// 6. Ir al menú completo
await page.click('button:has-text("Ver menú completo")');
await page.waitForTimeout(8000);
// → Step BrowseMenu activo, categoría PROMOCIONES seleccionada por defecto

// 7. Hacer click en la primera promo
const firstCard = page.locator('.product-card-banner, .product-card-fullbanner').first();
await firstCard.click();
await page.waitForTimeout(8000);
// → Step requiredOptions activo

// 8. Verificar que se muestran las opciones requeridas
await page.waitForSelector('.mobile-cleo-widget__product-detail', { timeout: 12000 });

// 9. Seleccionar la primera opción del primer grupo
await page.locator('.product-small').first().click();
await page.waitForTimeout(2000);
```
