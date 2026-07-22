# Antipatron 01: "strict" activado pero sin hardening real

## Contexto observado

En proyectos Angular legacy, se activa `"strict": true` en `tsconfig.json`, pero a la vez se dejan desactivadas reglas clave como:

- `"noImplicitAny": false`
- `"strictNullChecks": false`

Esto suele aparecer durante migraciones para no romper compilacion.

## Por que es un antipatron

- Da una falsa sensacion de seguridad de tipos.
- Oculta deuda tecnica de `any` y null handling.
- Aplaza errores de runtime que TypeScript podria detectar.

## Cuando SI se permite

Como estado transitorio y controlado en una migracion gradual.

## Estrategia recomendada

1. Fase 0: Estabilizar build/test con `strict: true` + excepciones temporales.
2. Fase 1: Tipar `@Input`, handlers y campos sin tipo para habilitar `noImplicitAny: true`.
3. Fase 2: Corregir nullability para habilitar `strictNullChecks: true`.
4. Fase 3: Revisar reglas Angular compiler (`strictTemplates`, etc.) por modulo.

## Criterio de salida

Se considera resuelto cuando:

- `noImplicitAny = true`
- `strictNullChecks = true`
- build y tests en verde

## Validacion minima

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```
