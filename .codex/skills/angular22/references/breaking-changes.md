# Angular 22 — Breaking Changes y Guía de Migración

## Versiones requeridas

| Dependencia | Mínimo | Recomendado |
|------------|--------|-------------|
| Node.js    | 22     | 22 LTS      |
| TypeScript | 6.0    | 6.x latest  |
| Angular    | 22.0   | 22.x latest |

---

## 1. OnPush como estrategia por defecto ⚠️ BREAKING

**Qué cambió:** El default de `changeDetection` en `@Component` pasó de
`ChangeDetectionStrategy.Default` a `ChangeDetectionStrategy.OnPush`.

**Impacto:** Componentes que mutaban objetos/arrays sin asignar nueva referencia
dejan de re-renderizar.

**Migración automática:** `ng update @angular/core@22` marca todos los componentes
existentes con `ChangeDetectionStrategy.Eager` (alias del antiguo Default).

```typescript
// ANTES (comportamiento v21 / código generado por ng update)
@Component({
  changeDetection: ChangeDetectionStrategy.Eager,  // preservado por migración
})

// NUEVO código (v22 en adelante)
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,  // default, pero explícito
})
```

**Qué hacer:** No usar `Eager` en código nuevo. Con signals, OnPush funciona sin
necesidad de llamar `markForCheck()`.

---

## 2. FetchBackend por defecto (HttpClient) ⚠️ BREAKING

**Qué cambió:** `HttpClient` usa `FetchBackend` (Fetch API) en lugar de `XhrBackend`.

**Impacto:**
- `reportProgress` para uploads ya no funciona con Fetch.
- Para upload progress, se necesita `withXhr()`.
- `reportProgress` está **deprecado**; usar `reportUploadProgress` o `reportDownloadProgress`.

```typescript
// Para mantener XHR globalmente (solo si necesario)
provideHttpClient(withXhr())

// Para upload con progreso (requiere XHR)
this.http.post('/api/upload', formData, {
  reportUploadProgress: true,
}).pipe(
  filter(e => e.type === HttpEventType.UploadProgress),
  map(e => Math.round(100 * e.loaded / (e.total ?? e.loaded))),
);
```

---

## 3. paramsInheritanceStrategy: 'always' ⚠️ BREAKING

**Qué cambió:** Los parámetros de rutas padre ahora están disponibles en rutas hijo
sin necesidad de traversal manual.

```typescript
// ANTES — necesitaba navegar el árbol de rutas
const id = this.route.parent?.parent?.snapshot.params['id'];

// AHORA (v22) — herencia automática
const id = this.route.snapshot.params['id'];
// También funciona con input() binding de router
```

**Para preservar comportamiento anterior:**
```typescript
provideRouter(routes, withRouterConfig({
  paramsInheritanceStrategy: 'emptyOnly'  // opt-out explícito
}))
```

---

## 4. strictTemplates habilitado por defecto ⚠️

**Qué cambió:** `angularCompilerOptions.strictTemplates` ahora es `true` por defecto.

**Impacto:** Errores de tipo en templates que antes pasaban silenciosamente ahora
fallan en build.

```json
// tsconfig.json — opt-out (NO recomendado para código nuevo)
{
  "angularCompilerOptions": {
    "strictTemplates": false
  }
}
```

---

## 5. Signal Forms — cambios de API (desde experimental)

```typescript
// ANTES (experimental)
form.fields.email.markAsTouched();  // solo marcaba el campo

// AHORA (estable v22)
form.fields.email.markAsTouched();  // marca TAMBIÉN descendientes por defecto
form.fields.email.markAsTouched({ skipDescendants: true });  // comportamiento anterior

// touch/touched renombrados
// ANTES: form.fields.email.touched → boolean
// AHORA: form.fields.email.touched → Signal<boolean>  (input)
//        form.fields.email.touch()  → void             (output/método)
```

---

## 6. canMatch ahora requiere currentSnapshot ⚠️

```typescript
// ANTES
export const authGuard = () => {
  const auth = inject(AuthStore);
  return auth.isAuthenticated();
};

// AHORA — firma de canMatch actualizada
export const authGuard: CanMatchFn = (route, segments, currentSnapshot) => {
  const auth = inject(AuthStore);
  return auth.isAuthenticated();
};
```

---

## 7. HTTP Transfer Cache — cookies excluidas

**Qué cambió:** Las peticiones que incluyen cookies ya no se cachean en la
transferencia SSR → CSR por defecto (seguridad).

**Para incluirlas explícitamente (no recomendado):**
```typescript
provideClientHydration(
  withHttpTransferCache({ includeRequestsWithAuthHeaders: true })
)
```

---

## 8. TypeScript 6 — cambios relevantes para Angular

```typescript
// TypeScript 6: using declarations (disposables)
class MyComponent {
  constructor() {
    using sub = someObservable.subscribe(); // se dispone automáticamente al salir del scope
  }
}

// Override keyword requerido en clases abstractas
abstract class BaseComponent {
  abstract getValue(): string;
}

class Concrete extends BaseComponent {
  override getValue(): string { return 'ok'; }  // override ahora requerido
}
```

---

## 9. Deprecaciones importantes en v22

| API | Reemplazada por |
|-----|----------------|
| `@Injectable({ providedIn: 'root' })` | `@Service()` |
| `reportProgress: true` (HTTP) | `reportUploadProgress` / `reportDownloadProgress` |
| `ChangeDetectionStrategy.Default` | `ChangeDetectionStrategy.Eager` (compat) / `OnPush` (nuevo) |
| `ngIf`, `ngFor`, `ngSwitch` (directivas) | `@if`, `@for`, `@switch` (control flow nativo) |
| `ngClass`, `ngStyle` | `[class.x]`, `[style.x]` directos |
| `@HostBinding`, `@HostListener` | `host: {}` en `@Component` |
| `EventEmitter` como estado | `signal()` / `model()` |
| `BehaviorSubject` para estado | `signal()` |
| `async` pipe | `toSignal()` / `httpResource()` |
| `ngOnChanges` | `effect()` sobre `input()` signals |
| `APP_INITIALIZER` para colores | `APP_INITIALIZER` + CSS custom properties (patrón correcto) |

---

## 10. Guía de migración para proyecto nuevo (ng new)

```bash
# 1. Verificar versiones
node --version   # debe ser v22+
tsc --version    # debe ser 6.x

# 2. Instalar Angular CLI 22
npm install -g @angular/cli@22

# 3. Crear proyecto (ya genera zoneless + OnPush por defecto)
ng new marketing-web --style=scss --ssr=false --routing=true

# 4. Verificar tsconfig.json generado
# angularCompilerOptions.strictTemplates: true  ← por defecto
# angularCompilerOptions.strictInputTypes: true

# 5. Instalar dependencias del proyecto
npm install
```

---

## 11. Compatibilidad con Microsoft Edge Tools (VS Code) ⚠️

**Síntoma:** `tsconfig.json` muestra el error:
```
'compilerOptions/module' must be equal to one of the allowed values ...
Value found '"preserve"'
Microsoft Edge Tools — typescript-config/is-valid
```

**Causa:** El schema de validación embebido en la extensión Edge Tools no incluye
`"preserve"` (introducido en TypeScript 5.4). El compilador y `ng build` **no fallan** —
es un falso positivo del linter de la extensión.

**Solución recomendada — `.hintrc`** en la raíz del proyecto Angular:
```json
{
  "hints": {
    "typescript-config/is-valid": "off"
  }
}
```
Después de crear el archivo, recargar VS Code con **Cmd+Shift+P → Developer: Reload Window**.

**NO cambiar** `"module"` a `"ESNext"` sin agregar explícitamente
`"moduleResolution": "bundler"`, porque `"preserve"` implica bundler resolution y
`"ESNext"` sin esa opción usa node16 por defecto, lo que puede romper imports.

---

## 12. Checklist para nuevo proyecto Angular 22

```
☐ Node 22+ instalado
☐ TypeScript 6 instalado
☐ ng new con --style=scss
☐ app.config.ts usa provideZonelessChangeDetection()
☐ Colores inyectados desde environment en APP_INITIALIZER
☐ _tokens.scss usa var(--color-*) exclusivamente
☐ Todos los componentes tienen OnPush explícito
☐ No hay NgModule en features nuevas
☐ Routes usan lazy loading (loadComponent / loadChildren)
☐ @Service en lugar de @Injectable({ providedIn: 'root' })
☐ httpResource en lugar de HttpClient + subscribe()
☐ Signal Forms en lugar de ReactiveFormsModule
☐ Control flow nativo (@if, @for, @switch)
☐ No ngClass / ngStyle / ngIf / ngFor en código nuevo
☐ viewChild / contentChild como signals
☐ No @HostBinding / @HostListener — usar host: {}
```
