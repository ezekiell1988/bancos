# Deprecación: BrowserAnimationsModule → animate.enter/leave (Angular 20.2+)

## Anti-patrón

Usar `BrowserAnimationsModule` en `imports` de `@NgModule`.

```typescript
// ❌ ANTI-PATRÓN (Angular 20.2+, deprecado)
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  imports: [
    BrowserAnimationsModule,  // ← Deprecado
    // ...
  ]
})
export class AppModule { }
```

## Problema

Angular 20.2.0 (PR #62795) **deprecó todo el paquete `@angular/animations`** en favor de:
- `animate.enter` y `animate.leave` (nuevas instrucciones CSS en templates)
- CSS moderno para animaciones

**Intent to remove:** v23 (completa deprecación)

**Razon:** Angular V20+ promueve el uso de CSS nativo y template instructions en lugar de cargar todo el módulo de animaciones.

## Síntomas

```
"BrowserAnimationsModule" está en desuso.ts(6385)
animations.d.ts(32, 4): La declaración se ha marcado aquí como en desuso.
@deprecated — 20.2 Use animate.enter or animate.leave instead. Intent to remove in v23
```

## Impacto

| Aspecto | Antes | Después |
|---------|-------|---------|
| Bundle size | Incluye `@angular/animations` (~10KB gzipped) | Sin carga de animaciones (excepto si usas `animate.enter/leave`) |
| API de animaciones | BrowserAnimationsModule + @angular/animations API | animate.enter/leave en templates + CSS |
| Compatibilidad | Angular 20–22 | Angular 20+ (v23+ obligatorio) |

## Solución

### Opción 1: Simplemente remover BrowserAnimationsModule (recomendado si no usas animaciones)

```typescript
// ✅ CORRECTO
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppComponent } from './app.component';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    // ← BrowserAnimationsModule removido
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

**Validación:** Si tu app no usa animaciones CSS, esto es suficiente.

---

### Opción 2: Usar animate.enter/leave en templates (si necesitas animaciones de enter/leave)

Sin necesidad de importar nada, usar directivas de template:

```html
<!-- template.html -->
<div @animate.enter="{ opacity: 0 }">
  Elemento con animación de entrada
</div>

<div @animate.leave="{ opacity: 0 }">
  Elemento con animación de salida
</div>
```

**Nota:** `animate.enter` y `animate.leave` funcionan sin `BrowserAnimationsModule` — son instrucciones CSS nativas de Angular 20+.

**Referencias:** [Angular Docs: Enter and Leave Animations](https://angular.dev/guide/animations/enter-and-leave)

---

### Opción 3: Usar CSS moderno (recomendado para casos complejos)

```css
/* styles.css */
.fade-enter {
  animation: fadeIn 0.3s ease-in;
}

.fade-leave {
  animation: fadeOut 0.3s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes fadeOut {
  from { opacity: 1; }
  to { opacity: 0; }
}
```

```html
<!-- template.html -->
<div class="fade-enter" *ngIf="isVisible">...</div>
<div class="fade-leave" *ngIf="!isVisible">...</div>
```

---

## Pasos de migración (paso a paso)

1. **Identifica si usas animaciones:**
   ```bash
   grep -r "@angular/animations" src/
   grep -r "BrowserAnimationsModule" src/
   grep -r "animate(" src/
   ```

2. **Si no hay uso → Simplemente remover:**
   ```typescript
   // En app.module.ts
   // imports: [ BrowserAnimationsModule ] ← Remover esta línea
   ```

3. **Si usas animaciones →**
   - Convertir a `animate.enter`/`animate.leave` en templates, O
   - Convertir a CSS @keyframes

4. **Validar build & tests:**
   ```bash
   npm run build
   npm test -- --watch=false --browsers=ChromeHeadless
   ```

---

## Código de ejemplo completo

**Antes (NgModule + BrowserAnimationsModule):**
```typescript
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  imports: [
    BrowserModule,
    BrowserAnimationsModule,  // ← Deprecado
    HttpClientModule,
  ],
  declarations: [ AppComponent ],
  bootstrap: [ AppComponent ]
})
export class AppModule { }
```

**Después (NgModule sin BrowserAnimationsModule):**
```typescript
// ✅ BrowserAnimationsModule removido completamente

@NgModule({
  imports: [
    BrowserModule,
    HttpClientModule,
  ],
  declarations: [ AppComponent ],
  bootstrap: [ AppComponent ]
})
export class AppModule { }
```

---

## Standalone Applications (si usas standalone)

Si usas `bootstrapApplication()`:

```typescript
// ❌ Antes
bootstrapApplication(AppComponent, {
  providers: [
    provideBrowserAnimations(),  // ← Deprecado (si existe)
  ]
});

// ✅ Después
bootstrapApplication(AppComponent);  // Sin providers de animaciones
```

---

## Timeline de deprecación

- **Angular 20.2.0:** BrowserAnimationsModule + @angular/animations deprecados
- **Angular 21–22:** Funciona aún (pero marca deprecation warning)
- **Angular 23:** **Remover completamente** (breaking change)

**Recomendación:** Aplicar este cambio ahora para estar preparado para Angular 23.

---

## Referencias

- PR: [angular/angular#62795 - Deprecate the animations package](https://github.com/angular/angular/pull/62795)
- Release: [Angular 20.2.0](https://github.com/angular/angular/releases/tag/20.2.0)
- New RFC: [Enter and Leave Animations RFC](https://github.com/angular/angular/discussions/62212)
- Docs (Angular v21): [Enter and Leave Animations Guide](https://angular.dev/guide/animations/enter-and-leave) *(nota: redirige a v17 actualmente, pero disponible en v21+)*

---

## Validación post-migración

```bash
# 1. Build sin errores
npm run build

# 2. Tests sin regresión
npm test -- --watch=false --browsers=ChromeHeadless

# 3. Revisar bundle size (opcional)
npm run build -- --stats-json
```

**Esperado:** Build y tests pasan sin cambios en regresión de funcionalidad.

---

## Notas

- **No es urgente en Angular 20–22**, pero debe hacerse antes de migrar a Angular 23.
- **Impacto mínimo** si no usas animaciones avanzadas (la mayoría de apps).
- **CSS moderno** es la opción más futura-proof (no depende de APIs de Angular).
