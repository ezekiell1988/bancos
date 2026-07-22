# Playbook aplicado con GitHub Copilot Modernization

## Objetivo del playbook

Actualizar `src/VoiceBot.WebApp` todo lo posible sin romper compatibilidad con Angular 21.

## Flujo aplicado (orden real)

1. Escaneo inicial con la herramienta de modernizacion.
2. Verificacion de compatibilidad Angular/TypeScript antes de aceptar upgrades mayores.
3. Ajustes de configuracion para reducir deprecaciones de Sass.
4. Actualizacion semver segura de dependencias.
5. Cierre de vulnerabilidades con `npm audit`.
6. Validacion tecnica final (build y tests).

## Decisiones tecnicas tomadas

### 1) Mantener Angular 21 como ancla

- Se descartaron propuestas incompatibles (por ejemplo TypeScript 6/7) cuando no eran seguras para Angular 21.
- Se mantuvo `typescript@5.9.3` para estabilidad.

### 2) Mitigar deprecaciones Sass sin romper schema

En `angular.json` se uso:

- `stylePreprocessorOptions.sass.silenceDeprecations`
- valores: `import`, `global-builtin`, `color-functions`

Notas:

- `quietDeps` se descarto por incompatibilidad del schema.
- `mixed-decls` se removio por obsoleto en el contexto actual.

### 3) Endurecimiento TS gradual (sin romper build)

En `tsconfig.json` se aplico estrategia por fases:

- `strict: true`
- excepciones temporales controladas:
  - `noImplicitAny: false`
  - `strictNullChecks: false`

Adicionalmente:

- remover `baseUrl` legacy
- explicitar `rootDir: "./src"`

### 4) Seguridad: eliminar ruta vulnerable transitiva

- Se detecto vulnerabilidad moderada por dependencia transitiva de `bootstrap-social` hacia `bootstrap@3.4.1`.
- Se removio `bootstrap-social` al confirmar que no se usaba desde `node_modules`.

## Migracion de testing deprecado

Caso aplicado en `src/test.ts`:

- `BrowserDynamicTestingModule` -> `BrowserTestingModule`
- `platformBrowserDynamicTesting` -> `platformBrowserTesting`
- `zone.js/dist/zone-testing` -> `zone.js/testing`

## Validacion minima obligatoria

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm audit --omit=dev
```

## Resultado esperado

- Build en verde.
- Tests en verde.
- Audit sin vulnerabilidades.
- Warnings de deprecacion reducidos o documentados con plan.
