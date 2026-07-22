# Playwright: Navegar hasta el Menú Completo en Cleo Chat

Guía para llegar al paso **BrowseMenu** (menú completo con categorías) partiendo desde el paso **ViewDailyPromos**.

---

## Pre-requisito

Completar primero los pasos 1–6 de `cleo-promos.md` para llegar a la pantalla de promociones.

---

## Paso 7 — Click en "Ver menú completo"

Desde el widget de Promos del día hay un botón que envía al usuario directamente al menú:

```js
await page.click('button:has-text("Ver menú completo")');
await page.waitForTimeout(8000);
```

**Selector alternativo** (por si el texto cambia):
```js
await page.click('[aria-label*="menú"],[aria-label*="menu"],[class*="browse"]');
```

**Respuesta esperada del bot:** el widget cambia al menú completo con:
- Un banner de Cleo apuntando hacia abajo
- Un slider de categorías con tarjetas de imagen: `PROMOCIONES`, `BIG 4`, `GRAN HUT`, `PROMOS INDIVIDUALES`, `PIZZAS`, `HUT WINGS`, `PASTAS`, `APERITIVOS`, `ENSALADAS`, `POSTRES`, `BEBIDAS`
- Los productos de la primera categoría listados debajo

---

## Verificar que el menú cargó

```js
// Confirmar que el widget de menú es visible
await page.waitForSelector('[aria-label="Menú de productos"]', { timeout: 10000 });

// Verificar categorías disponibles
const categories = await page.$$eval('button[aria-label*="Categoría"]', els => els.map(e => e.textContent?.trim()));
// O bien desde los botones del slider:
const cats = await page.$$eval('button', els =>
  els.map(e => e.textContent?.trim()).filter(t => t && t.length > 2 && t.length < 25)
);
return cats;
// → ['PROMOCIONESSeleccionado', 'BIG 4', 'GRAN HUT', 'PROMOS INDIVIDUALES', 'PIZZAS', 'HUT WINGS', ...]
```

---

## Paso 8 — Navegar entre categorías

```js
// Click en una categoría específica
await page.click('button:has-text("PIZZAS")');
await page.waitForTimeout(4000);

// Verificar que el heading de la sección cambió
const heading = await page.$eval('[class*="group-title"],[class*="product-group"] h2', el => el.textContent);
// → 'PIZZAS'
```

---

## Paso 9 — Tomar screenshot del menú

```js
screenshot_page(pageId)
```

---

## Selectores clave del menú

| Elemento | Selector |
|----------|----------|
| Widget completo | `[aria-label="Menú de productos"]` |
| Slider de categorías | `[aria-label="Categorías"]` |
| Botón categoría anterior | `[aria-label="Categoría anterior"]` |
| Card banner (primera fila) | `.product-card-banner` |
| Card small (grid) | `.product-small` |
| Card row | `.product-card-row` |
| Debug badges | `.dev-flags` _(solo en `--configuration development`)_ |

---

## Flujo completo resumido (Playwright)

```js
// === Desde cero hasta el menú ===

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
// → Step BrowseMenu activo

// 7. Screenshot
screenshot_page(pageId);
```
