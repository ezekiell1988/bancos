# Deprecacion: HttpClientModule -> provideHttpClient(withInterceptorsFromDi())

## Anti-patron

Usar HttpClientModule dentro de imports en AppModule.

```typescript
// ANTI-PATRON
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  imports: [
    BrowserModule,
    HttpClientModule,
  ]
})
export class AppModule {}
```

## Problema

Angular marca HttpClientModule como deprecado y recomienda usar providers funcionales:
- provideHttpClient(...)
- withInterceptorsFromDi() para mantener interceptors registrados con HTTP_INTERCEPTORS

## Sintoma

```text
"HttpClientModule" esta en desuso.ts(6385)
@deprecated - use provideHttpClient(withInterceptorsFromDi()) as providers instead
```

## Solucion

```typescript
// CORRECTO
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

@NgModule({
  imports: [
    BrowserModule,
  ],
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
  ]
})
export class AppModule {}
```

## Pasos de migracion

1. Remover HttpClientModule del arreglo imports.
2. Cambiar import de HttpClientModule por provideHttpClient + withInterceptorsFromDi.
3. Agregar provideHttpClient(withInterceptorsFromDi()) en providers.
4. Validar build y tests.

## Validacion

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Referencias

- Angular API de HttpClientModule (deprecado): https://angular.dev/api/common/http/HttpClientModule
- Angular API de provideHttpClient: https://angular.dev/api/common/http/provideHttpClient
- Angular API de withInterceptorsFromDi: https://angular.dev/api/common/http/withInterceptorsFromDi
