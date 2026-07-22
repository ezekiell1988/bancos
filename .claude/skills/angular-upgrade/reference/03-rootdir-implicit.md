# Antipatron 03: dejar rootDir implicito con outDir definido

## Contexto

Cuando `outDir` esta definido y `rootDir` queda implicito, TypeScript infiere la raiz comun de fuentes. En migraciones TS 6/7 aparecen advertencias para explicitar layout.

Referencia oficial:

- TSConfig `rootDir`: controla la estructura emitida en `outDir`.
- `rootDir` no cambia que archivos compilan; solo layout de salida y restricciones de emision.

## Sintoma

- Warning: "The common source directory ... rootDir should be explicitly set".
- Cambios inesperados en estructura de `dist/out-tsc` cuando cambia el set de archivos.

## Impacto

- Layout de build menos predecible.
- Riesgo de ruido en CI/CD y artefactos.

## Regla de deteccion

- `outDir` presente y `rootDir` ausente.

## Fix recomendado

1. Definir `rootDir` explícito (por ejemplo `./src` en Angular app).
2. Confirmar que todo archivo emitible cae bajo `rootDir`.

## Validacion

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```
