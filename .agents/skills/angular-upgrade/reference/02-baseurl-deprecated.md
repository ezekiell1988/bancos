# Antipatron 02: usar baseUrl para imports bare en Angular moderno

## Contexto

`baseUrl` fue pensado para loaders AMD y hoy no se recomienda para proyectos modernos con bundler.

Referencia oficial:

- TSConfig `baseUrl`: no recomendado en otros contextos y no requerido para `paths` desde TS 4.1.

## Sintoma

- Warning de deprecacion hacia TS 6/7.
- Riesgo de resolucion ambigua con `node_modules` (prioriza base local).

## Impacto

- Acoplamiento innecesario a un comportamiento legacy.
- Migraciones futuras mas ruidosas (TS 6 readiness check).

## Regla de deteccion

- `compilerOptions.baseUrl` definido sin una necesidad real de `paths`.
- Imports bare internos del proyecto (ejemplo `from "app/..."`) en vez de relativos o aliases explicitos en `paths`.

## Fix recomendado

1. Remover `baseUrl` si no se usa activamente.
2. Mantener imports relativos o definir aliases claros con `paths` cuando sea necesario.
3. No usar `ignoreDeprecations` como solucion permanente; solo temporal durante transicion.

## Validacion

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```
