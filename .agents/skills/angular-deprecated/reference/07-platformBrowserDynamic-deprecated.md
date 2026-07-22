# Antipatron 07: platformBrowserDynamic deprecado — migrar a platformBrowser

## Contexto

A partir de **Angular 20.0.0** (refactor #61043), el paquete completo `@angular/platform-browser-dynamic` ha sido deprecado. Esto incluye tanto `platformBrowserDynamic()` como `platformBrowserDynamicTesting()`.

## Síntoma

- Error de TypeScript: `platformBrowserDynamic está en desuso (6385)`
- Archivo: `main.ts` usa `platformBrowserDynamic` desde `@angular/platform-browser-dynamic`
- Sugerencia del IDE/compilador:
  ```
  Use the platformBrowser function instead from @angular/platform-browser. 
  In case you are not in a CLI app and rely on JIT compilation, 
  you will also need to import @angular/compiler
  ```

## Impacto

- Deprecación de todo el paquete `@angular/platform-browser-dynamic`.
- En futuras versiones de Angular (v21+), este paquete será removido.
- JIT compilation en apps no-CLI requiere importar explícitamente `@angular/compiler`.

## Regla de detección

- `import { platformBrowserDynamic } from '@angular/platform-browser-dynamic'`
- `import { platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing'`

## Fix recomendado

### Caso 1: App CLI (bootstrapping normal)

**Antes:**
```typescript
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

platformBrowserDynamic()
  .bootstrapModule(AppModule)
  .catch(err => console.log(err));
```

**Después:**
```typescript
import { platformBrowser } from '@angular/platform-browser';
import { AppModule } from './app/app.module';

platformBrowser()
  .bootstrapModule(AppModule)
  .catch(err => console.log(err));
```

### Caso 2: App no-CLI con JIT (requiere @angular/compiler)

Si tu app no está construida con Angular CLI y depende de JIT:

```typescript
import { platformBrowser } from '@angular/platform-browser';
import { AppModule } from './app/app.module';
// Necesario para JIT en no-CLI apps
import '@angular/compiler';

platformBrowser()
  .bootstrapModule(AppModule)
  .catch(err => console.log(err));
```

### Caso 3: Testing (test.ts)

**Antes:**
```typescript
import { platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';
```

**Después:** (ya mitigado en referencia 04)
```typescript
import { platformBrowserTesting } from '@angular/platform-browser/testing';
```

## Validación

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Notas

- Angular CLI app (normal): No necesita importar `@angular/compiler` — está en `devDependencies`.
- Non-CLI app con JIT: Debe importar `@angular/compiler` explícitamente o usar AOT (Ahead-of-Time compilation).
- Esta migración es segura y no introduce breaking changes en Angular 20–21.

## Referencias

- [Angular 20.0.0 Deprecations](https://github.com/angular/angular/releases/tag/20.0.0)
- [Platform Browser Deprecation PR #61043](https://github.com/angular/angular/pull/61043)
