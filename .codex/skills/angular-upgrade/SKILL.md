---
name: angular-upgrade
description: >
  Guia de actualización de dependencias Angular/TypeScript para este repo. Usar cuando
  se pida actualizar paquetes npm, aplicar GitHub Copilot Modernization, corregir
  configuraciones de tsconfig, migrar Sass, o endurecer tipado gradual sin romper
  build/test. Triggers: angular update, upgrade dependencies, modernization, github
  copilot modernization, strict mode, tsconfig, baseUrl, rootDir, sass migration,
  npm audit, noopener noreferrer, inline styles.
---

# Angular Upgrade

Skill para ejecutar actualizaciones seguras de Angular/TypeScript y documentar patrones
de migracion de configuracion comprobados en este proyecto.

> Para migraciones de APIs Angular deprecadas (HttpClientModule, BrowserAnimationsModule,
> etc.) ver el skill `angular-deprecated`.
> Para problemas de accesibilidad (Axe, ARIA, alt text) ver el skill `angular-a11y`.

## Objetivo

- Actualizar dependencias sin salir de la version Angular objetivo.
- Corregir configuraciones de tsconfig y build sin introducir regresiones.
- Mantener build/test/audit en verde en cada iteracion.
- Reusar el playbook probado de GitHub Copilot Modernization.

## Como usar este skill

1. Escanear dependencias y riesgos con GitHub Copilot Modernization.
2. Fijar la matriz de compatibilidad real (Angular vs TypeScript).
3. Aplicar cambios incrementales de config/codigo.
4. Validar continuamente:
   - `npm run build`
   - `npm test -- --watch=false --browsers=ChromeHeadless`
   - `npm audit --omit=dev`
5. Registrar cualquier antipatron o fix reusable en `reference/`.

## Referencias actuales

- [Strict habilitado pero relajado (migracion gradual)](./reference/01-strict-gradual.md)
- [baseUrl deprecado y no recomendado](./reference/02-baseurl-deprecated.md)
- [rootDir implicito con outDir](./reference/03-rootdir-implicit.md)
- [Playbook aplicado con GitHub Copilot Modernization](./reference/05-github-copilot-modernization.md)
- [Migracion Sass desbalanceada (silenceDeprecations + migrador)](./reference/06-sass-migration-unbalanced.md)
- [HTML meta elements — duplicado charset + viewport restrictivo](./reference/08-html-meta-elements.md)
- [TS2593 en tests: describe no encontrado por mezcla de tsconfig](./reference/11-ts2593-describe-not-found.md)
- [no-inline-styles: mover background-image al array styles del @Component](./reference/16-no-inline-styles.md)
- [disown-opener: target="_blank" requiere rel="noopener noreferrer"](./reference/17-disown-opener-noopener.md)

## Regla de oro

No hacer actualizaciones masivas de una sola pasada. Primero estabilizar, luego endurecer
por fases con evidencia de build/test/audit en cada paso.
