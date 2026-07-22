# disown-opener: Link rel debe incluir noopener

## Regla

`disown-opener` (Edge Tools) — seguridad en links que abren nueva pestaña.  
MDN: *"Using `target="_blank"` without `rel="noreferrer"` and `rel="noopener"` makes
the website vulnerable to `window.opener` API exploitation attacks."*

## El riesgo

Cuando un link usa `target="_blank"` sin `rel="noopener"`, la página nueva recibe
acceso a `window.opener` — una referencia a la página original. Un sitio malicioso
puede usarlo para redirigir la página del usuario a una URL de phishing.

## Patron correcto

```html
<!-- BIEN: noopener bloquea window.opener, noreferrer añade privacidad -->
<a href="https://example.com" target="_blank" rel="noopener noreferrer">
  External link
</a>
```

### Diferencia entre los dos valores

| Atributo | Efecto |
|---|---|
| `rel="noopener"` | Bloquea `window.opener` (seguridad) |
| `rel="noreferrer"` | No envía header `Referer` (privacidad) + implica `noopener` |
| `rel="noopener noreferrer"` | Ambos — recomendado por MDN |

> Nota: Navegadores modernos (Chrome, Firefox, Safari, Edge) ya aplican `noopener`
> implícitamente en `target="_blank"`, pero declararlo explícitamente mantiene
> compatibilidad con navegadores antiguos y elimina la alerta de las herramientas.

## Anti-patron

```html
<!-- MAL: target="_blank" sin rel -->
<a href="https://example.com" target="_blank">External link</a>

<!-- MAL: rel presente pero sin noopener -->
<a href="https://example.com" target="_blank" rel="nofollow">External link</a>
```

## Aplicacion en este repo (theme-panel.component.html)

6 links con `target="_blank"` recibieron `rel="noopener noreferrer"`:

- One Page Parallax frontend (línea 210)
- E-commerce frontend (línea 215)
- Blog frontend (línea 220)
- Forum frontend (línea 225)
- Corporate frontend (línea 230)
- Documentation button (línea 239)

## Referencias

- MDN `<a>` — Security and privacy:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/a#security_and_privacy
- MDN `rel=noopener`:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Attributes/rel/noopener
- MDN `rel=noreferrer`:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Attributes/rel/noreferrer
