# Angular 22 — Errores frecuentes del Language Service

Registro de falsos positivos y patrones que confunden al Angular Language Service
(extensión VS Code `@angular/language-service@22.x`) y sus soluciones verificadas
en este repositorio.

---

## 1. "Value could not be determined statically" al importar un componente hijo

### Síntoma

El Language Service marca el array `imports` del componente padre con:

```
'imports' must be an array of components, directives, pipes, or NgModules.
Value could not be determined statically.
```

Y en cascada aparece en el template:

```
'app-roles-panel' is not a known element.
Can't bind to 'idLogin' since it isn't a known property of 'app-roles-panel'.
```

El `ng build` compila **sin errores**. El fallo es solo en el Language Server.

### Causa raíz

El Angular Language Service 22 no puede analizar estáticamente un componente cuando se
importa con un path largo que apunta **directamente** al archivo `.ts` del componente:

```typescript
// ❌ FALLA en Language Server (aunque build compila)
import { RolesPanelComponent } from '../../components/roles-panel/roles-panel';
```

El Language Server pierde la capacidad de inferir los metadatos del componente
(`selector`, `inputs`, `outputs`) cuando la ruta de importación tiene varios segmentos
profundos terminados en el nombre del archivo.

### Solución: barrel local de componentes

Crear un `index.ts` en la carpeta `components/` de la feature que re-exporte el
componente, e importar desde ese barrel:

```typescript
// components/index.ts
export { RolesPanelComponent } from './roles-panel/roles-panel';
```

```typescript
// pages/detail/user-detail.ts
// ✅ CORRECTO — Language Server lo resuelve sin problemas
import { RolesPanelComponent } from '../../components';
```

### Patrón establecido en este repo

Todas las features con sub-componentes propios usan barrel local en `components/`:

| Feature | Barrel |
|---------|--------|
| `client-settings` | `components/tabs/index.ts` (re-exporta 9 tabs) |
| `users-roles` | `components/index.ts` (re-exporta `RolesPanelComponent`) |

**Regla:** Al crear un componente en `feature/components/<nombre>/`, agregar siempre
su export al barrel `feature/components/index.ts`.

---

## 2. `linkedSignal` provoca análisis incompleto del componente

### Síntoma

El mismo error "Value could not be determined statically" puede aparecer también cuando
un componente hijo usa `linkedSignal` importado de `@angular/core`. El Language Server
no siempre infiere correctamente el tipo retornado por `linkedSignal<T>()` con
funciones factory complejas.

### Causa raíz

`linkedSignal` es estable en Angular 22 pero el Language Service puede fallar al
inferir el tipo cuando la factory accede a otros recursos reactivos (ej: `this.resource.value()`).

### Solución: usar `signal + effect` en su lugar

El patrón `signal<T>(initialValue) + effect()` es equivalente y el Language Service
lo analiza sin problemas. Es el patrón estándar de este repositorio.

```typescript
// ❌ Puede confundir al Language Server
protected readonly selectedRoleIds = linkedSignal<Set<number>>(() => {
  const assignments = this.loginRoles.value() ?? [];
  return new Set(assignments.map(a => a.idRole));
});
```

```typescript
// ✅ Patrón estándar — signal + effect (TabRedisComponent, RolesPanelComponent)
protected readonly selectedRoleIds = signal<Set<number>>(new Set());

private readonly _syncRoles = effect(() => {
  const assignments = this.loginRoles.value();
  if (assignments !== undefined) {
    this.selectedRoleIds.set(new Set(assignments.map(a => a.idRole)));
  }
});
```

> **Nota Angular 22:** `allowSignalWrites` en `effect()` está **deprecado** porque
> los effects pueden escribir en signals por defecto. No hace falta pasarlo.

### Referencia: patrón `TabRedisComponent`

```typescript
// client-settings/components/tabs/tab-redis/tab-redis.ts
protected readonly connectionString = signal('');

private readonly _sync = effect(() => {
  const cfg = this.config.value();
  if (!cfg) return;
  this.connectionString.set(cfg.connectionString);
});
```

---

## 3. Checklist antes de reportar un error del Language Server

Antes de invertir tiempo investigando, verificar en orden:

1. **¿Compila el build?** Ejecutar `npx ng build --configuration development`.
   Si el build es limpio, el error es del Language Server, no del compilador.

2. **¿El componente está en `imports` del `@Component`?**
   Confirmar que el componente hijo está listado explícitamente.

3. **¿El import es un path directo profundo?**
   Si termina en `/nombre-componente/nombre-componente`, crear un barrel y
   cambiar a `../../components`.

4. **¿El componente hijo usa `linkedSignal`?**
   Reemplazar con `signal + effect` siguiendo el patrón de la sección 2.

5. **¿La versión del Language Service coincide con el proyecto?**
   Verificar en `devDependencies` que `@angular/language-service` sea `^22.x`.
   Instalar con: `npm install --save-dev @angular/language-service@22`.

---

## 4. Versión de Language Service instalada en este repo

```json
// package.json (devDependencies)
"@angular/language-service": "^22.0.4"
```

Esta versión fue instalada explícitamente para alinear el análisis del IDE con la
versión de Angular del proyecto (`22.0.4`).
