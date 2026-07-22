---
name: angular-a11y
description: >
  Guía de accesibilidad (a11y) para componentes Angular en este repo. Usar cuando
  aparezcan errores de Axe, advertencias WCAG, violations de ARIA, o cuando se quiera
  auditar/corregir accesibilidad en templates. Triggers: axe, accessibility, a11y,
  aria-label, aria-labelledby, alt text, image alt, link name, button name, form label,
  wcag, screen reader, role, tabindex, discernible text.
applyTo: "src/**/*.{html,ts}"
---

# Angular A11y — Accesibilidad

Skill para corregir violaciones de accesibilidad (Axe / WCAG) en templates Angular,
usando los patrones probados en este proyecto.

> Para actualizar dependencias o tsconfig ver `angular-upgrade`.
> Para APIs Angular deprecadas ver `angular-deprecated`.

## Objetivo

- Eliminar violaciones Axe en `npm test` y auditorías de Lighthouse.
- Asegurar compatibilidad con lectores de pantalla (VoiceOver, NVDA, JAWS).
- Usar `aria-label` / `aria-labelledby` / `alt` según el rol semántico correcto.
- Mantener build/test en verde tras cada corrección.

## Como usar este skill

1. Identificar la regla Axe violada (rule-id, description, elemento afectado).
2. Buscar la reference correspondiente abajo y aplicar el patrón de corrección.
3. Validar: `npm test -- --watch=false --browsers=ChromeHeadless` (Axe corre en tests).
4. Si no existe reference, crear una nueva documentando el elemento, la regla y el fix.

## Referencias actuales

- [Axe image-alt: img sin alt — top-menu y sidebar](./reference/13-image-alt-menu-components.md)
- [Axe link-name: Links must have discernible text — aria-label en toda la app](./reference/14-link-name-discernible-text.md)
- [Axe forms: Form elements must have labels — Bootstrap toggle switch con label vacío](./reference/15-form-inputs-label.md)
- [Axe button-name: Buttons must have discernible text — aria-label en botones icon-only](./reference/18-button-name-discernible-text.md)
- [Axe aria-command-name: div[role="button"] — convertir a button nativo](./reference/19-div-role-button-to-native-button.md)
- [Axe button-name con Angular bindings: [attr.aria-label] no visible a analizadores estáticos](./reference/20-angular-binding-aria-static-fallback.md)

## Reglas de oro

- `<img>` decorativa → `alt=""` (string vacío, no omitir el atributo).
- `<img>` informativa → `alt` descriptivo en el idioma de la UI.
- `<img>` con texto dinámico → usar **interpolación** `alt="{{ expr }}"`, no property binding `[alt]="expr"` — los analizadores estáticos (Edge Tools, Axe) no resuelven property bindings.
- `<a>` sin texto visible → `aria-label` en el elemento `<a>`.
- `<button>` icon-only → `aria-label` en el `<button>`.
- `<button>` fuera de form de envío → siempre `type="button"`.
- `<button>` con `[attr.aria-label]` dinámico → añadir `aria-label` estático como fallback antes del binding (ver reference 20).
- `<input>` de formulario → siempre `<label for="">` o `aria-label` / `aria-labelledby`.
- No usar `title` como sustituto de `alt` o `aria-label` — no es confiable en lectores.
- `<ul>`/`<ol>` con `@if` o `@for` dentro → el parser estático detecta nodos `#text` inválidos como hijos directos. Usar `<div role="list">` + `<div role="listitem">` cuando los hijos son condicionales con `@if`.
- `<dl>` con pares `<dt>`/`<dd>` condicionales → **nunca usar `@if` dentro de `<dl>`**. El spec HTML (WHATWG) permite solo `<div>`, `<script>` o `<template>` como hijos directos en modo "wrapped"; `@if` Angular genera nodos de comentario y whitespace que el parser interpreta como nodos `#text` inválidos. Solución: poner **todos** los `<div class="kv-pair">` siempre en el DOM y usar `[hidden]` para ocultar los opcionales:
  ```html
  <!-- ✅ CORRECTO -->
  <dl>
    <div class="kv-pair">           <!-- siempre presente -->
      <dt>STT</dt><dd>{{ val }}</dd>
    </div>
    <div class="kv-pair" [hidden]="campo == null">  <!-- condicional via [hidden] -->
      <dt>Latencia</dt><dd>{{ campo }} ms</dd>
    </div>
  </dl>

  <!-- ❌ INCORRECTO — @if genera #text como hijo directo del dl -->
  <dl>
    @if (campo != null) {
      <div class="kv-pair">...</div>
    }
  </dl>
  ```
  CSS para `.kv-pair`: `display: flex; gap: 8px; align-items: baseline; margin-bottom: 5px;` — no usar Bootstrap `.row`/`.col-*` dentro de `<dl>` (requiere `display: contents` que a su vez viola no-inline-styles).
