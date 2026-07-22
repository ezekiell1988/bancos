# TS2593: No se encuentra el nombre 'describe' en Angular

## Anti-patron

Dejar `*.spec.ts` incluidos en el `tsconfig.json` base del proyecto (app) en vez de aislarlos al `tsconfig.spec.json`.

## Sintoma

```text
No se encuentra el nombre "describe". ¿Necesita instalar definiciones de tipo para un test runner?
ts(2593)
```

## Causa real en Angular

En Angular con Karma/Jasmine, los globales `describe`, `it`, `beforeEach` vienen de `@types/jasmine` y deben resolverse desde `tsconfig.spec.json` con:

```json
{
  "compilerOptions": {
    "types": ["jasmine"]
  }
}
```

Si el `tsconfig.json` base incluye `src/**/*.spec.ts`, el editor puede analizar specs dentro del proyecto de app (sin tipos de test) y mostrar TS2593 aunque `@types/jasmine` ya este instalado.

## Solucion recomendada

1. Mantener `@types/jasmine` en devDependencies.
2. Mantener `types: ["jasmine"]` en `tsconfig.spec.json`.
3. Si el warning persiste en un archivo puntual, agregar al inicio del spec:

```typescript
/// <reference types="jasmine" />
```

4. Reiniciar TypeScript server en VS Code si el warning persiste.

## Importante en este repo

En esta configuracion de Angular CLI, excluir `*.spec.ts` desde el `tsconfig.json` base puede sacar los tests de la compilacion del target `test`. Por eso el fix seguro aqui es mantener `tsconfig.spec.json` como fuente principal y usar la referencia explicita en el spec cuando el editor no resuelve bien los tipos.

## Nota sobre el mensaje de VS Code

El hint de instalar `@types/jest` o `@types/mocha` es generico. En proyectos Angular CLI con Karma, lo correcto normalmente es Jasmine (`@types/jasmine`).

## Validacion

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Referencias

- TypeScript tsconfig `types`: https://www.typescriptlang.org/tsconfig#types
- Angular testing setup (Karma/Jasmine): https://angular.dev/guide/testing
- Angular workspace test target con `tsconfig.spec.json`: https://angular.dev/reference/configs/workspace-config
