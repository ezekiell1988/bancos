# no-inline-styles: CSS inline styles should not be used

## Regla

`no-inline-styles` (Edge Tools) — MDN `<span>`: *"It can be used to group elements for
styling purposes (using the `class` or `id` attributes)".*  
Los estilos inline (`style=""`) mezclan markup con presentación, dificultan el
mantenimiento y bloquean la especificidad CSS.

## Anti-patron

```html
<!-- MAL: background-image como estilo inline -->
<span style="background-image: url(/assets/img/theme/default.jpg);" class="theme-version-cover"></span>
```

## Patron correcto en Angular — array `styles` en el decorador

Cuando los valores son únicos por elemento (ej. 22 URLs de imagen distintas) y no se
quiere crear un archivo CSS separado, se usa el array `styles` dentro del propio
`@Component`. Angular encapsula estos estilos en el componente con View Encapsulation.

```typescript
@Component({
  selector: 'theme-panel',
  templateUrl: './theme-panel.component.html',
  styles: [`
    .cover-default     { background-image: url('/assets/img/theme/default.jpg'); }
    .cover-transparent { background-image: url('/assets/img/theme/transparent.jpg'); }
    /* ... resto de clases ... */
  `],
  standalone: false
})
```

```html
<!-- BIEN: clase CSS, sin style="" -->
<span class="theme-version-cover cover-default"></span>
```

## Regla de decision

| Situacion | Solucion |
|---|---|
| Estilos reutilizables en múltiples componentes | Clase en `styles.scss` global |
| Estilos exclusivos del componente, valores estáticos | Array `styles` en `@Component` |
| Valor dinámico en Angular (depende de datos) | `[ngClass]` con clases predefinidas |
| Nunca | `style="..."` literal en el template |

## Aplicacion en este repo

`theme-panel.component.ts` — 22 clases `.cover-{name}` en el array `styles` del
decorador reemplazan los 22 atributos `style="background-image:..."` que tenía el
template.

Convencion de nombres: `.cover-{filename-sin-extension}` donde el filename viene
de la ruta original (`/assets/img/theme/default.jpg` → `.cover-default`).

## Referencias

- MDN `<span>`:
  https://developer.mozilla.org/docs/Web/HTML/Reference/Elements/span
- Angular Component Styles:
  https://angular.dev/guide/components/styling
