---
name: color-admin-ui-elements
description: >
  Referencia completa de elementos de UI de Color Admin: clases CSS, patrones HTML y componentes
  listos para copiar. Usar SIEMPRE que se necesite implementar botones, alertas, tipografía, tabs,
  modales, iconos, banderas, tablas, badges, progress bars, media objects, social buttons, o
  cualquier elemento visual del template Color Admin. El CSS ya está compilado en vendor.min.css —
  no se necesita CSS personalizado, solo las clases correctas.
applyTo: "**/*.html"
---

# Skill: Color Admin UI Elements

## Propósito

Documenta todas las clases CSS y patrones HTML disponibles en el template **Color Admin** para
Angular. El objetivo es generar UI sin escribir CSS personalizado — usando únicamente las clases
ya disponibles en `vendor.min.css`.

**Disparar cuando:**
- Se necesite un botón, alerta, badge, tab, modal, accordion, tabla, etc.
- Se pregunte qué clase usar para un estilo específico en Color Admin
- Se necesite mostrar un ícono (Bootstrap Icons, FontAwesome, Duotone/Solar, Simple Line, banderas)
- Se creen componentes con `<panel>`, media flex, social buttons o widget boxes

## Tecnologías cubiertas

- **Bootstrap 5** — sistema base de clases
- **Color Admin** — extensiones: colores extra, tamaños `fs-*px`, `w-*px`, `h-*px`, `me-*px`, etc.
- **Bootstrap Icons v1.11.3** — `bi bi-{nombre}`
- **FontAwesome 6** — `fas fa-{nombre}`, `fab fa-{nombre}`, `far fa-{nombre}`
- **Iconify Solar Duotone** — `solar:{nombre}-bold-duotone`
- **Simple Line Icons** — `icon-{nombre}`
- **Flag Icons** — `fi fi-{codigo-iso}`

## Referencias por categoría

### Elementos de Interacción

[reference: references/buttons.md]

> Botones sólidos, outline, tamaños, grupos, dropdowns, dropups, íconos en botones

[reference: references/tabs-accordions.md]

> `nav nav-tabs`, `nav nav-pills`, tabs verticales, acordeones Bootstrap 5, acordeón estilo panel

[reference: references/modal-notification.md]

> `modal fade`, `modal-message`, tamaños de modal, modales con alertas internas

### Elementos de Contenido

[reference: references/general.md]

> Alerts (`alert-{color}`), notes, badges, progress bars, pagination, labels, tables, wells

[reference: references/typography.md]

> h1–h6, clases de texto, listas, código, `fs-10px`→`fs-24px`, blockquote, dl

[reference: references/media-object.md]

> Flexbox media object, imágenes responsivas (`w-lg-250px`), media anidado

[reference: references/widget-boxes.md]

> Componente `<panel>`, props, content projection (header/outsideBody/footer), grid patterns

### Botones Sociales

[reference: references/social-buttons.md]

> `btn btn-social btn-{plataforma}`: Twitter, Facebook, Google, y 21 más. Variante icon-only.

### Íconos

[reference: references/icon-bootstrap.md]

> Bootstrap Icons `bi bi-{nombre}`: 2000+ íconos. Grid, tamaños con `fs-*px`, patrón con `h-60px bg-light`

[reference: references/icon-duotone.md]

> Iconify Solar `solar:{nombre}-bold-duotone`: 1200+ íconos duotone. Tamaños con `display-*`

[reference: references/icon-fontawesome.md]

> FontAwesome 6: `fas/fab/far`, tamaños `fa-xs`→`fa-10x`, `fa-fw`, `fa-spin`, `fa-stack`, rotación/volteo

[reference: references/icon-simple-line-icons.md]

> Simple Line Icons `icon-{nombre}`: home, user, settings, social-*, screen-*, y más

[reference: references/language-icon.md]

> Flag Icons `fi fi-{iso2}`: banderas de países. Latinoamérica + mundo. Tamaños con clases heading.

## Clases de utilidad clave (Color Admin)

### Tipografía / tamaños de fuente
```
fs-10px  fs-11px  fs-12px  fs-13px  fs-14px  fs-15px  fs-16px
fs-18px  fs-20px  fs-22px  fs-24px  fs-28px  fs-32px
```

### Anchos fijos
```
w-30px  w-40px  w-50px  w-60px  w-80px  w-100px
w-120px w-150px w-200px w-250px w-300px w-350px
```

### Anchos responsive (Color Admin)
```
w-lg-250px w-lg-200px w-lg-150px
```

### Altos fijos
```
h-10px h-20px h-30px h-40px h-50px h-60px
h-100px h-150px h-200px h-250px h-300px
```

### Márgenes / padding con px
```
me-5px   me-10px  me-15px  me-20px  me-25px
ms-5px   ms-10px  ms-15px
mt-5px   mt-10px  mt-15px  mt-20px
mb-5px   mb-10px  mb-15px
pt-10px  pb-10px  ps-10px  pe-10px
p-10px   p-15px   p-20px
```

## Colores de marca extendidos

Color Admin incluye estas variantes además de las Bootstrap estándar:
```
purple   indigo   yellow   pink   lime   green   inverse   default   gray
```

Se usan como: `btn-purple`, `alert-indigo`, `badge bg-lime`, `text-pink`, etc.

## Reglas de uso

1. **Nunca escribir CSS inline** para cosas que ya tienen clase — buscar en las references
2. **Preferir clases de Color Admin** sobre atributos `style=""`
3. **Para íconos**: elegir la librería según el contexto (Bootstrap Icons para UI general, FontAwesome para integración social, Duotone para dashboards/cards decorativos, banderas para selectores de idioma/país)
4. **El componente `<panel>`** es el contenedor principal — ver `widget-boxes.md`
