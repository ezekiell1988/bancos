---
name: angular-deprecated
description: >
  Patrones de migración de APIs Angular deprecadas en este repo. Usar cuando aparezcan
  advertencias de deprecated en la consola, cuando el build/test emita warnings sobre
  APIs obsoletas, o cuando se quiera modernizar código que usa módulos/funciones ya
  eliminados o marcados para eliminación. Triggers: deprecated, NgModule deprecated,
  platformBrowserDynamic, BrowserAnimationsModule, HttpClientModule, RouterTestingModule,
  BrowserDynamicTestingModule, provideHttpClient, provideAnimations, standalone migration.
applyTo: "src/**/*.{ts,html}"
---

# Angular Deprecated — Migraciones de APIs Obsoletas

Skill para reemplazar APIs Angular marcadas como deprecated o eliminadas en versiones
recientes, usando los patrones probados en este proyecto.

> Para actualizar dependencias, tsconfig o el workflow de modernización ver `angular-upgrade`.
> Para problemas de accesibilidad (Axe, ARIA) ver `angular-a11y`.

## Objetivo

- Eliminar warnings `NG0xxx deprecated` del build y la consola.
- Migrar a las APIs standalone/funcionales que reemplazan los NgModules.
- Mantener build/test en verde tras cada reemplazo.

## Como usar este skill

1. Identificar la API deprecada: nombre, versión desde la que está deprecated y
   versión en la que se elimina.
2. Buscar la reference correspondiente abajo y aplicar el patrón de reemplazo.
3. Validar: `npm run build` + `npm test -- --watch=false --browsers=ChromeHeadless`.
4. Si no existe reference, crear una nueva documentando el antipatrón y el fix.

## Referencias actuales

- [APIs legacy de testing (BrowserDynamicTestingModule)](./reference/04-angular-testing-legacy-api.md)
- [platformBrowserDynamic deprecado → platformBrowser (Angular 20+)](./reference/07-platformBrowserDynamic-deprecated.md)
- [BrowserAnimationsModule deprecado → animate.enter/leave (Angular 20.2+)](./reference/09-browserAnimationsModule-deprecated.md)
- [HttpClientModule deprecado → provideHttpClient(withInterceptorsFromDi())](./reference/10-httpclientmodule-deprecated.md)
- [RouterTestingModule deprecado → RouterModule.forRoot/provideRouter](./reference/12-routertestingmodule-deprecated.md)

## Regla de oro

Migrar una API por iteración. Validar build/test antes de continuar con la siguiente.
Las migraciones de NgModule → standalone suelen ser transitivas (cambiar uno expone
el siguiente deprecated).
