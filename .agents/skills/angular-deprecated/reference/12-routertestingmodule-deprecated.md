# Deprecacion: RouterTestingModule -> provideRouter o RouterModule.forRoot

## Anti-patron

Usar `RouterTestingModule` en tests de Angular.

```typescript
import { RouterTestingModule } from '@angular/router/testing';

TestBed.configureTestingModule({
  imports: [RouterTestingModule],
});
```

## Problema

`RouterTestingModule` esta deprecado. Angular recomienda:
- `provideRouter(...)`, o
- `RouterModule` / `RouterModule.forRoot(...)`

La razon es que muchos fakes que antes aportaba el modulo ya no son necesarios, porque `MockPlatformLocation` viene por defecto en TestBed.

## Sintoma

```text
"RouterTestingModule" esta en desuso.ts(6385)
@deprecated Use provideRouter or RouterModule/RouterModule.forRoot instead
```

## Solucion recomendada en este repo

Para specs con componentes basados en NgModule (como `AppComponent`), usar `RouterModule.forRoot([])` en `imports`.

```typescript
import { RouterModule } from '@angular/router';

TestBed.configureTestingModule({
  imports: [RouterModule.forRoot([])],
  declarations: [AppComponent],
});
```

## Alternativa moderna

En tests orientados a providers, usar:

```typescript
providers: [provideRouter([])]
```

Si se necesitan mocks de Location/LocationStrategy, agregar `provideLocationMocks()`.

## Validacion

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

## Referencias

- RouterTestingModule API (deprecado): https://angular.dev/api/router/testing/RouterTestingModule
- provideRouter API: https://angular.dev/api/router/provideRouter
- RouterModule API: https://angular.dev/api/router/RouterModule
- provideLocationMocks API: https://angular.dev/api/common/testing/provideLocationMocks
