---
name: ionic-navigation
description: >
  Componentes de navegación Ionic: ion-tabs (con y sin router), ion-menu con
  ion-menu-button, y ion-breadcrumbs con soporte para colapso dinámico.
  Usar al implementar tabs de navegación, menú lateral deslizante o rutas de
  migas de pan en una aplicación Ionic Angular.
applyTo: "**/*.html"
---

# ionic-navigation

Componentes de navegación para aplicaciones Ionic.

## Componentes de esta sección

- [reference: references/ion-tabs.md]
- [reference: references/ion-menu.md]
- [reference: references/ion-breadcrumbs.md]

## Reglas de navegación

1. `ion-tabs` sin router: usar `ion-tab` con `tab="nombre"` como contenedores.
2. `ion-tabs` con Angular Router (modo más común en apps reales): no usar `ion-tab`, solo `ion-tab-bar` + `ion-tab-button tab="nombre"` que mapean a rutas lazy-loaded.
3. `ion-menu` requiere que el elemento raíz de la página principal tenga `id="main-content"` coincidiendo con `[contentId]` del menú.
4. `ion-breadcrumbs` es solo visual; el routing lo gestiona `href` o Angular Router.
