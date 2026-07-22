# Antipatron 06: Sass migration desbalanceada en tsconfig / angular.json

## Contexto

Durante la migracion de Angular/Sass legacy, es comun encontrar conflictos entre lo que el migrador Sass propone (`--migrate-deps`, `--migrate-all`) y el schema de `angular.json`.

## Sintoma

- Warnings de Sass deprecadas en build (`import`, `global-builtin`, `color-functions`).
- Intentos fallidos de migration script porque el schema no permite ciertos flags antiguos.
- Riesgo de mantener degradacion de sintaxis Sass sin validacion clara.

## Impacto

- Build ruidoso por warnings que no son errores.
- Migrador Sass genera un reporte pero no puede aplicarse directamente sin ajuste manual.
- Postergacion de limpieza Sass a futuras migraciones mayores.

## Regla de deteccion

- `warnings` o `errors` de Sass en `npm run build`.
- Schema de `angular.json` rechaza flags del migrador.
- Ausencia de `stylePreprocessorOptions.sass.silenceDeprecations` en `angular.json`.

## Fix recomendado

**Fase 1 — Suprimir warnings de forma controlada:**

```json
// angular.json
"build": {
  "options": {
    "stylePreprocessorOptions": {
      "sass": {
        "silenceDeprecations": [
          "import",
          "global-builtin",
          "color-functions"
        ]
      }
    }
  }
}
```

**Fase 2 — Auditar que el migrador Sass funciona:**

```bash
# Dry run del migrador (NO aplica cambios)
npx sass-migrator --dry-run --migrate-all ./src
```

Si el migrador genera un reporte pero `npx sass-migrator --migrate-all` falla:

```bash
# Intento extendido con entrypoint Bootstrap (por ejemplo)
npx sass-migrator --dry-run --migrate-all --migrate-deps ./node_modules/bootstrap/_functions.scss
```

**Fase 3 — Plan incremental de limpieza:**

Una vez que el migrador funciona (exit 0), guardar su propuesta pero aplicar cambios solo a `_custom.scss` o archivos de la app, no a toda la carpeta. Validar build/test en cada paso.

## Validacion

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Notas

- No usar `ignoreDeprecations` (ignora TODO); usar `silenceDeprecations` para suppresion explicita por tipo.
- El schema de `angular.json` suele ser la fuente de verdad; el migrador Sass es una herramienta informativa.
