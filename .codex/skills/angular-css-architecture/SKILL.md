---
name: angular-css-architecture
description: >
  Diseña, refactoriza y revisa estilos CSS/SCSS de aplicaciones Angular con tokens
  globales y estilos encapsulados por componente. Usar al crear un sistema de diseño,
  centralizar colores, separar styles.css, añadir styleUrl, revisar CSS global o
  resolver problemas de responsive y overflow. Triggers: Angular CSS, Angular SCSS,
  variables CSS, design tokens, styleUrl, estilos globales, arquitectura CSS,
  separar estilos, responsive, overflow horizontal.
---

# Arquitectura CSS para Angular

Usar esta skill para mantener estilos predecibles y fáciles de modificar sin mezclar
las reglas globales con el diseño particular de una pantalla.

## Principio de separación

| Capa | Responsabilidad |
|---|---|
| `src/styles.css` | Tokens CSS, reset y reglas base verdaderamente compartidas. |
| `app.css` | Estructura visual exclusiva del shell de aplicación. |
| `feature/pages/*.css` | Composición y detalles exclusivos de una página. |
| Componentes reutilizables | Estilos asociados a cada componente mediante `styleUrl`. |

Los tokens se definen en `:root` y se consumen con `var()`. Los valores semánticos
permiten cambiar el tema desde una única fuente; no repetir colores hexadecimales en
los estilos de pantallas o componentes.

```css
:root {
  --color-text: #1d2939;
  --color-surface: #f8fafc;
  --color-brand: #175cd3;
  --color-border: #d0d5dd;
  --radius-md: .4rem;
}

.button-primary {
  background: var(--color-brand);
  border-radius: var(--radius-md);
}
```

## Cuándo usar CSS o SCSS

- Usar CSS nativo cuando se necesitan tokens, variables, selectores y media queries.
- Usar SCSS solo si el proyecto ya lo configura o si nesting/mixins aportan valor real.
- No introducir Sass únicamente para disponer de variables: las CSS Custom Properties
  son estándar, participan en la cascada y permiten temas en tiempo de ejecución.

## Procedimiento

1. Inspeccionar `src/styles.css` y los `@Component` afectados. Identificar valores
   repetidos, selectores globales de página y reglas realmente compartidas.
2. Definir tokens con nombres semánticos en `:root`: color, superficie, borde,
   estados, radios, espaciado y tipografía cuando sean compartidos.
3. Sustituir valores repetidos por `var(--token)` sin cambiar su valor visual.
4. Mover reglas de layout del shell a `app.css` y enlazarlo con `styleUrl`.
5. Mover reglas exclusivas de una pantalla a `nombre-pagina.css` y enlazarlas con
   `styleUrl` en su componente standalone.
6. Mantener global únicamente lo que distintos componentes necesitan. No usar
   selectores globales para implementar detalles de una sola feature.
7. Comprobar conflictos de especificidad y mantener la encapsulación predeterminada
   de Angular; no usar `ViewEncapsulation.None` para evitar una colisión.
8. Ejecutar el build y probar escritorio y móvil.

## Patrón Angular

```ts
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './imports-page.css',
  template: `...`,
})
export class ImportsPage {}
```

Angular aplica por defecto encapsulación emulada a los estilos de componentes. Esto
limita sus reglas a la plantilla propia y evita que una pantalla contamine otra.

## Revisión responsive

Probar como mínimo un viewport de escritorio y uno móvil. La regla general es:

```js
document.documentElement.scrollWidth === window.innerWidth
```

- Corregir el elemento que crea el overflow; no ocultar el scroll globalmente.
- En grids/flex, aplicar `min-width: 0` a hijos con texto que pueda extenderse.
- Usar `minmax(0, 1fr)` cuando las columnas deben encajar en pantallas pequeñas.
- Mantener controles táctiles legibles y evitar que barras fijas cubran contenido.

## Validación

1. Ejecutar `npm run build` desde el proyecto Angular.
2. Confirmar que cada `styleUrl` referencia un archivo existente.
3. Verificar que los colores literales solo aparezcan al definir tokens globales.
4. Revisar las rutas afectadas en desktop y móvil, incluido el ancho del documento.

## Fuentes

- [Angular: Styling components](https://angular.dev/guide/components/styling)
- [MDN: Using CSS custom properties](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Cascading_variables/Using_custom_properties)
