---
name: color-admin-form
description: >
  Guía completa de formularios en Color Admin (Bootstrap-based). Cubre clases CSS para inputs,
  selects, checkboxes, radios, switches, grupos de entrada, validación, layouts (vertical,
  horizontal, inline), wizards multi-paso y plugins de formulario (datepicker, timepicker,
  tagify, editor de texto, color picker). Usar SIEMPRE que se construya un formulario en
  un proyecto con el template Color Admin para evitar escribir CSS personalizado.
applyTo: "**/*.html"
---

# Skill: Color Admin — Formularios

## Propósito

Documenta todas las clases CSS y patrones HTML de **formularios** disponibles en el template
**Color Admin** para Angular. El objetivo es generar formularios sin escribir CSS personalizado —
usando únicamente las clases ya disponibles en `vendor.min.css`.

**Disparar cuando:**
- Se cree o modifique un formulario en una página Color Admin
- Se busque la clase correcta para input, label, select, checkbox, radio o switch
- Se implemente validación visual en campos
- Se estructure un layout de formulario (vertical, horizontal, inline)
- Se construya un wizard multi-paso
- Se integre un plugin de formulario (datepicker, timepicker, tagify, editor rico, color picker)

## Tecnologías cubiertas

- **Bootstrap 5** — base de clases de formulario (`form-control`, `form-check`, etc.)
- **Color Admin** — extensiones de espaciado (`mb-15px`, `mb-10px`, `mb-5px`, `me-5px`, `w-*px`, `fs-*px`)
- **ng-bootstrap** — `ngb-datepicker`, `ngb-timepicker`
- **Tagify** — `@yaireo/tagify`
- **ngx-editor** — editor de texto rico
- **ngx-color** — color picker

## Referencias por página

### Controles, Validación y Layouts

[reference: references/form-elements.md]

> `form-control`, `form-select`, `form-range`, floating labels, tamaños, validación (`is-valid`/`is-invalid`/`valid-feedback`/`invalid-feedback`), checkboxes, radios, switches, input groups, layouts vertical/horizontal/inline

### Wizards Multi-paso

[reference: references/form-wizards.md]

> `nav-wizards-1` (número + texto), `nav-wizards-2` (solo texto), `nav-wizards-3` (punto + título + subtexto), estados `completed`/`active`/`disabled`, wizard dinámico Angular con signals

### Plugins de Formulario

[reference: references/form-plugins.md]

> `ngb-datepicker` (inline y popup), `ngb-timepicker` (meridian, validación custom), Tagify (tags input), ngx-editor (editor rico), ngx-color (color picker con ngbDropdown)
