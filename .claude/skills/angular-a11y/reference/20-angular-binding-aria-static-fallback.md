# Axe button-name con Angular bindings: [attr.aria-label] no visible a analizadores estáticos

## Regla

`axe/name-role-value` (button-name) — WCAG 4.1.2  
Aplica cuando un `<button>` icon-only usa `[attr.aria-label]` con binding Angular.

## Causa raíz

Los analizadores estáticos (Edge Tools, Axe CLI, Lighthouse en CI) **no ejecutan Angular**.
Ven el HTML pre-compilado donde `[attr.aria-label]="expresión"` aparece como atributo
sin valor — el botón queda sin nombre accesible desde su perspectiva.

**Esto afecta cualquier atributo de accesibilidad puesto con binding Angular:**
- `[attr.aria-label]="..."`
- `[attr.aria-labelledby]="..."`
- `[attr.title]="..."`

## Anti-patrón

```html
<!-- MAL 1: aria-label solo mediante binding — analizador estático reporta button-name -->
<button [attr.aria-label]="listening ? 'Pausar escucha' : 'Iniciar escucha'">
  <i class="fas fa-microphone" aria-hidden="true"></i>
</button>

<!-- MAL 2: falta type="button" en botones fuera de form — puede disparar submit -->
<button [attr.aria-label]="...">...</button>
```

## Solución A (preferida): `aria-label` estático + binding dinámico

Añadir un `aria-label` estático **antes** del binding. El analizador lo lee como
texto discernible. En runtime, `[attr.aria-label]` **sobreescribe** el atributo
estático, por lo que el screen reader recibe el texto dinámico correcto.

```html
<!-- BIEN: fallback estático + override dinámico -->
<button type="button"
        aria-label="Micrófono activo"
        [attr.aria-label]="isSpeaking ? 'Interrumpir bot' : 'Micrófono activo'">
  <ion-icon name="mic-outline" aria-hidden="true"></ion-icon>
</button>

<!-- Con contexto enriquecido (múltiples botones iguales en lista) -->
<button type="button"
        aria-label="Reproducir audio"
        [attr.aria-label]="playingIndex() === $index
          ? 'Pausar audio de ' + msg.role + ' ' + msg.time
          : 'Reproducir audio de ' + msg.role + ' ' + msg.time">
  <ion-icon name="play-outline" aria-hidden="true"></ion-icon>
</button>
```

**Por qué funciona:** el orden de precedencia ARIA garantiza que `aria-label`
(sea estático o dinámico) siempre gana sobre el inner text. Angular escribe el
atributo en el DOM en runtime, sobreescribiendo el valor estático.

## Solución B (alternativa): `visually-hidden` como fallback estático

Útil cuando el texto del label estático no tiene sentido fuera de contexto o
cuando el binding es sobre `aria-labelledby` (no `aria-label`).

```html
<button type="button"
        [attr.aria-label]="listening ? 'Pausar escucha' : 'Iniciar escucha'"
        [attr.aria-pressed]="listening">
  <span class="visually-hidden">Micrófono</span>
  <i class="fas fa-microphone" aria-hidden="true"></i>
</button>
```

## Cuándo usar A vs B

| Caso | Solución |
|------|----------|
| `aria-label` **estático** en button icon-only | Solo `aria-label="texto fijo"` |
| `aria-label` **dinámico** — el fallback estático es útil por sí solo | **Solución A** |
| `aria-labelledby` dinámico o fallback sin sentido en estático | **Solución B** |
| Botón con texto visible (`<span>texto</span>`) | No requiere nada adicional |

## Regla adicional: `type="button"` siempre obligatorio

Todo `<button>` fuera de un `<form>` de envío **debe** tener `type="button"`.
Sin él, el valor por defecto es `type="submit"` y puede disparar un submit
inesperado si el botón se encuentra dentro de cualquier `<form>` ancestro.

```html
<!-- MAL -->
<button (click)="playAudio()">...</button>

<!-- BIEN -->
<button type="button" (click)="playAudio()">...</button>
```

## Orden de precedencia de nombre accesible (ARIA spec)

1. `aria-labelledby` (más alta)
2. `aria-label`
3. Contenido de texto del elemento (inner text)
4. `title` (más baja)

Esto garantiza que `[attr.aria-label]` en runtime sobreescribe al `visually-hidden`.

## Referencias

- MDN Accessible name computation:
  https://developer.mozilla.org/docs/Glossary/Accessible_name
- WCAG 4.1.2 Name, Role, Value:
  https://www.w3.org/WAI/WCAG21/Understanding/name-role-value.html
- Bootstrap `visually-hidden`:
  https://getbootstrap.com/docs/5.3/helpers/visually-hidden/
