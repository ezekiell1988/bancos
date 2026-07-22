# Axe aria-command-name: ARIA commands must have an accessible name

## Regla

`axe/aria` (aria-command-name) — WCAG 4.1.2  
Elementos con `role="button"` deben tener un nombre accesible reconocible por el analizador.

## Causa raíz

Edge Tools y Axe son **analizadores estáticos**: no evalúan expresiones Angular como
`[attr.aria-label]="..."` en tiempo de análisis. Por eso un `div[role="button"]` con
nombre puesto mediante binding Angular aparece como "sin nombre accesible".

## Anti-patrón

```html
<!-- MAL: div con role="button" y aria-label dinámico Angular — analizador estático lo ignora -->
<div class="mic-container"
     role="button"
     tabindex="0"
     [attr.aria-label]="listening ? 'Pausar escucha' : 'Iniciar escucha'"
     [attr.aria-pressed]="listening"
     (click)="toggleListening()"
     (keyup.enter)="toggleListening()"
     (keyup.space)="toggleListening()">
  ...
</div>
```

## Solución: convertir a `<button>` nativo

```html
<!-- BIEN: <button> nativo — semánticamente correcto, sin role/tabindex extra -->
<button type="button" class="mic-container"
        [attr.aria-label]="listening ? 'Pausar escucha' : 'Iniciar escucha'"
        [attr.aria-pressed]="listening"
        (click)="toggleListening()">
  <span class="visually-hidden">Micrófono</span>
  ...
</button>
```

**Ventajas del `<button>` nativo sobre `div[role="button"]`:**
- Accesible por teclado (Enter / Space) de forma inherente — no necesita `(keyup.*)`
- Focusable sin `tabindex="0"`
- Reconocido por analizadores estáticos como elemento de botón válido
- Menos atributos, menos posibilidad de error

## Reset CSS necesario

Si el `<button>` reemplaza a un contenedor con diseño personalizado, resetear los estilos
nativos para no romper el layout:

```css
.mic-container {
  border: none;
  background: transparent;
  padding: 0;
}
```

## Referencias

- MDN `<button>` — uso semántico correcto vs `div[role="button"]`:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/button
- WCAG 4.1.2 Name, Role, Value:
  https://www.w3.org/WAI/WCAG21/Understanding/name-role-value.html
- axe rule aria-command-name:
  https://dequeuniversity.com/rules/axe/4.11/aria-command-name
