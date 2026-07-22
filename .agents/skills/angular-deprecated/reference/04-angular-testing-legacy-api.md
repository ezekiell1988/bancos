# Antipatron 04: usar APIs legacy de testing en Angular moderno

## Contexto

En proyectos heredados es comun encontrar `test.ts` con imports antiguos de `platform-browser-dynamic/testing`.

Ejemplos:

- `BrowserDynamicTestingModule` (deprecado)
- `platformBrowserDynamicTesting` (ruta legacy)
- `zone.js/dist/zone-testing` (deep import legacy)

## Evidencia oficial

- `BrowserDynamicTestingModule` esta marcado como `deprecated` y recomienda usar `BrowserTestingModule` de `@angular/platform-browser/testing`.
- Angular removio deep imports `zone.js/dist/*`; la forma correcta es `zone.js/testing`.

## Impacto

- Warnings de deprecacion en IDE y CI.
- Riesgo de ruptura al subir de major Angular/Zone.js.

## Fix recomendado

Migrar `src/test.ts` a:

- `import 'zone.js/testing'`
- `BrowserTestingModule`
- `platformBrowserTesting`

## Validacion

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```
