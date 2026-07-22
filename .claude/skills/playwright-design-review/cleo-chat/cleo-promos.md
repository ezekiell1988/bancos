# Playwright: Navegar hasta las Promociones en Cleo Chat

Guía de los pasos exactos para usar el skill `playwright-design-review` y llegar al paso **ViewDailyPromos** del chat Cleo.

---

## Credenciales de prueba

| Campo   | Valor      |
|---------|------------|
| Teléfono | `83681485` |
| PIN OTP  | `475957` _(solo si la sesión expiró)_ |

> La cuenta está pre-autenticada; en la mayoría de los casos el OTP **no se pide** porque la sesión persiste en Redis.

---

## Paso 0 — Cargar el skill

```
read_file(".agents/skills/playwright-design-review/SKILL.md")
```

---

## Paso 1 — Abrir la página

```js
open_browser_page("https://localhost:8000/cleo-chat")
// → devuelve un pageId, ej: f38a9a92-12f2-4f56-8b25-f5d7647b0268
```

---

## Paso 2 — Esperar a que el WebSocket conecte

El input empieza **disabled** mientras el WS `/ws/cleo` negocia.
Si sigue "Conectando..." después de ~5 s, recargar la página:

```js
await page.reload({ waitUntil: 'networkidle', timeout: 30000 });
await page.waitForSelector('input[placeholder*="Escribe"]:not([disabled])', { timeout: 20000 });
```

Una vez habilitado el input, la barra de estado muestra **"En línea"**.

---

## Paso 3 — Autenticación

### 3a — Ingresar teléfono

```js
await page.fill('input[placeholder*="Escribe"]', '83681485');
await page.keyboard.press('Enter');
await page.waitForTimeout(5000);
```

**Respuesta esperada del bot:** `"¡Bienvenido de vuelta, Ezequiel! ¿En qué te puedo ayudar hoy?"`  
_(Si el bot pide OTP, ir al paso 3b; si ya saluda, saltar al paso 4.)_

### 3b — Ingresar OTP (solo si lo pide)

```js
await page.fill('input[placeholder*="Escribe"]', '475957');
await page.keyboard.press('Enter');
await page.waitForTimeout(4000);
```

---

## Paso 4 — Seleccionar "Hacer un pedido 🍕"

```js
await page.click('button:has-text("Hacer un pedido")');
await page.waitForTimeout(5000);
```

**Respuesta esperada:** `"¿Cómo preferís recibir tu pedido?"`

---

## Paso 5 — Seleccionar tipo de entrega

```js
await page.click('button:has-text("Envío a domicilio")');
await page.waitForTimeout(5000);
```

**Respuesta esperada:** `"¿Querés enviar el pedido a Casa: Familia Baltodano Soto?"`

---

## Paso 6 — Confirmar dirección

```js
await page.click('button:has-text("Sí, esa")');
await page.waitForTimeout(6000);
```

**Respuesta esperada:** `"¡Tenemos estas promociones del día! ¿Cuál te llama la atención?"`  
Se renderizan las tarjetas de producto con el grupo **"Promociones del día"**.

---

## Paso 7 — Tomar screenshot de las promos

```js
screenshot_page(pageId)
```

Comparar con las imágenes de referencia:

| Archivo | Descripción |
|---------|-------------|
| `ia/assets/shopcart-design.png` | Diseño **sin banner** (fondo crema, imagen izquierda) |
| `ia/assets/shopcart-design-banner.png` | Diseño **con banner** (fondo de color, imagen derecha) |

---

## Paso 8 — Scroll para ver todas las tarjetas

```js
await page.evaluate(() => {
  document.querySelector('[class*="product-group"]')
    ?.scrollIntoView({ behavior: 'instant', block: 'start' });
});
screenshot_page(pageId)
```

---

## Workflow de cambios y rebuild

### Launch 5 — "Build Angular + Run .NET Dev"

El perfil de VS Code **`5. Build Angular + Run .NET Dev`** ejecuta en secuencia:

```bash
# 1) Build Angular (development)
cd src/VoiceBot.Web && npm run ng -- build --configuration development

# 2) Arrancar .NET API (Debug)  ← solo arranca si el build Angular fue exitoso
cd src/VoiceBot.Api && dotnet run --project VoiceBot.Api.csproj --configuration Debug
```

### ¿Cuándo rehacerlo?

| Tipo de cambio | Acción |
|---|---|
| Solo `.cs` (backend) | Detener el terminal de Launch 5, volver a ejecutarlo |
| Solo `.ts` / `.html` / `.css` (frontend) | Ídem — el build Angular regenera el `dist/` que sirve .NET |
| Ambos | Ídem — el mismo launch 5 hace los dos pasos |

> **Si Copilot modifica algún archivo `.cs`, pedirle al usuario que ejecute el Launch 5 antes de volver a testear.**

---

## Notas de implementación

- Las imágenes de producto vienen de `img.clickeat.online` y pueden fallar con `ERR_BLOCKED_BY_ORB` en el browser headless — es normal; aparecen los placeholders.
- El campo `displayType` del SP determina el layout:
  - `Normal` + `isBanner = true` → `.product-card-banner` (fondo cálido, imagen derecha, badge colorido)
  - `Normal` + `isBanner = false` → `.product-card-row` (thumb izquierda, info derecha)
- El componente relevante es:
  `src/VoiceBot.Web/src/app/features/cleo-chat/mobile/components/cleo-product-card/`

### ✅ Bug corregido — promos con `IsCombo=true` (2026-05-22)

**Síntoma:** productos de promos con `IsCombo=true` se renderizaban como `app-mobile-cleo-product-principal`
(imagen arriba, ancho completo) en lugar del layout horizontal banner.

**Causa:** `BuildPromoPageAsync` y `MapPromosToGroups` usaban `IsCombo` para asignar
`ProductDisplayType.SeleccionCombo`, lo que propagaba ese tipo al grupo completo.

**Fix aplicado** en `ChatViewDailyPromosStep.cs` y `ProductRenderAdapter.cs`:
- Cards: **siempre `Normal` + `IsBanner: true`** — ya no depende del valor `IsBanner` en BD (fix 2026-05-25)
- Grupo: siempre `ProductDisplayType.Normal`

### ✅ UX — Scroll automático al entrar a promos (2026-05-25)

Al transicionar a `viewDailyPromos`, el widget hace **scroll-to-top** automáticamente para que el hero (título + Cleo) quede visible. Implementado con un `effect` en `cleo-chat-widget.component.ts`.
