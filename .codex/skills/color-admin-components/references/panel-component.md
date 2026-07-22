# Guía Completa del Componente Panel

## Tabla de Contenidos
- [Introducción](#introducción)
- [Arquitectura del Componente](#arquitectura-del-componente)
- [Propiedades de Entrada](#propiedades-de-entrada)
- [Funcionalidades Principales](#funcionalidades-principales)
- [Content Projection (ng-content)](#content-projection-ng-content)
- [Ejemplos de Uso](#ejemplos-de-uso)
- [Personalización y Estilos](#personalización-y-estilos)
- [Integración con Otros Componentes](#integración-con-otros-componentes)
- [Mejores Prácticas](#mejores-prácticas)

## Introducción

El **componente Panel** es un contenedor versátil y reutilizable diseñado para encapsular contenido en la aplicación Color Admin. Proporciona una interfaz consistente con funcionalidades integradas como expansión, recarga, colapso y eliminación.

### ¿Por qué usar el componente Panel?

- ✅ **Interfaz Consistente**: Diseño uniforme en toda la aplicación
- ✅ **Funcionalidades Integradas**: Botones de acción preconstruidos
- ✅ **Altamente Personalizable**: Múltiples puntos de inyección de contenido
- ✅ **Flexible**: Soporta diferentes variantes y configuraciones
- ✅ **Responsive**: Se adapta automáticamente al contenedor
- ✅ **Reutilizable**: Fácil de implementar en cualquier parte de la app

## Arquitectura del Componente

### Estructura de Archivos

```
panel.component.ts    # Lógica del componente
panel.component.html  # Template HTML
```

### Anatomía del Panel

```
┌─────────────────────────────────────────┐
│ PANEL HEADING                           │  ← Header con título y botones
│  Title                    [- ↻ □ ×]     │
├─────────────────────────────────────────┤
│ BEFORE BODY (opcional)                  │  ← Contenido antes del body
├─────────────────────────────────────────┤
│ PANEL BODY                              │  ← Contenido principal
│                                         │
│  [Contenido principal aquí]            │
│                                         │
├─────────────────────────────────────────┤
│ OUTSIDE BODY (opcional)                 │  ← Contenido fuera del body
├─────────────────────────────────────────┤
│ PANEL FOOTER (opcional)                 │  ← Footer del panel
└─────────────────────────────────────────┘
```

## Propiedades de Entrada

### Inputs del Componente

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `title` | `string` | - | Título del panel mostrado en el header |
| `variant` | `string` | `'inverse'` | Variante de color del panel |
| `noBody` | `boolean` | `false` | Si es `true`, no renderiza el panel-body |
| `noButton` | `boolean` | `false` | Si es `true`, oculta los botones de acción |
| `headerClass` | `string` | `''` | Clases CSS adicionales para el header |
| `bodyClass` | `string` | `''` | Clases CSS adicionales para el body |
| `footerClass` | `string` | `''` | Clases CSS adicionales para el footer |
| `panelClass` | `string` | `''` | Clases CSS adicionales para el panel completo |

### Variantes de Color Disponibles

| Variante | Clase CSS | Descripción |
|----------|-----------|-------------|
| `inverse` | `panel-inverse` | Panel oscuro (default) |
| `default` | `panel-default` | Panel claro |
| `primary` | `panel-primary` | Color primario (azul) |
| `success` | `panel-success` | Color de éxito (verde) |
| `warning` | `panel-warning` | Color de advertencia (amarillo) |
| `danger` | `panel-danger` | Color de peligro (rojo) |
| `info` | `panel-info` | Color informativo (cian) |

## Funcionalidades Principales

### 1. Estados del Componente

```typescript
export class PanelComponent {
  expand: boolean = false;      // Panel en modo expandido (pantalla completa)
  reload: boolean = false;      // Panel en estado de recarga
  collapse: boolean = false;    // Panel colapsado (body oculto)
  remove: boolean = false;      // Panel eliminado (componente oculto)
  showFooter: boolean = false;  // Mostrar/ocultar footer automáticamente
}
```

### 2. Métodos Principales

#### `panelExpand()`
Alterna el modo de pantalla completa del panel.

```typescript
panelExpand() {
  this.expand = !this.expand;
}
```

**Comportamiento:**
- Expande el panel a pantalla completa
- Agrega la clase `panel-expand` al contenedor
- Al hacer clic nuevamente, restaura el tamaño original

#### `panelReload()`
Muestra un indicador de carga durante 1.5 segundos.

```typescript
panelReload() {
  this.reload = true;
  setTimeout(() => {
    this.reload = false;
  }, 1500);
}
```

**Comportamiento:**
- Muestra un spinner de carga sobre el contenido
- Agrega la clase `panel-loading`
- Se desactiva automáticamente después de 1.5s

**💡 Tip:** Puedes extender este método para recargar datos reales:

```typescript
panelReload() {
  this.reload = true;
  this.loadData().subscribe(
    data => {
      this.updateContent(data);
      this.reload = false;
    },
    error => this.reload = false
  );
}
```

#### `panelCollapse()`
Colapsa o expande el body del panel.

```typescript
panelCollapse() {
  this.collapse = !this.collapse;
}
```

**Comportamiento:**
- Oculta/muestra el contenido del panel-body
- Agrega la clase `d-none` al body cuando está colapsado
- Útil para ahorrar espacio en la pantalla

#### `panelRemove()`
Elimina el panel de la vista.

```typescript
panelRemove() {
  this.remove = !this.remove;
}
```

**Comportamiento:**
- Oculta completamente el panel del DOM
- Usa `*ngIf` para no renderizar el componente
- La acción es irreversible sin recarga de página

### 3. Detección Automática del Footer

```typescript
ngAfterViewInit() {
  setTimeout(() => {
    this.showFooter = (this.panelFooter) 
      ? this.panelFooter.nativeElement && 
        this.panelFooter.nativeElement.children.length > 0 
      : false;
  });
}
```

**Funcionamiento:**
- Detecta automáticamente si hay contenido en el footer
- Solo muestra el footer si contiene elementos hijos
- Evita renderizar un footer vacío

## Content Projection (ng-content)

El componente Panel utiliza **Content Projection** avanzado con múltiples slots para máxima flexibilidad.

### Slots Disponibles

| Selector | Ubicación | Uso |
|----------|-----------|-----|
| `[header]` | Dentro del panel-heading | Contenido personalizado en el header |
| `[beforeBody]` | Antes del panel-body | Contenido previo al cuerpo principal |
| (default) | Dentro del panel-body | Contenido principal del panel |
| `[noBody]` | Sin contenedor de body | Contenido sin envoltura de body |
| `[outsideBody]` | Después del body, antes del footer | Contenido entre body y footer |
| `[footer]` | Dentro del panel-footer | Contenido del pie del panel |
| `[afterFooter]` | Después del panel-footer | Contenido adicional al final |

### Diagrama de Proyección

```html
<panel>
  <!-- HEADER SLOT -->
  <div header>Custom header content</div>
  
  <!-- BEFORE BODY SLOT -->
  <div beforeBody>Content before body</div>
  
  <!-- DEFAULT SLOT (Body) -->
  <div>Main content here</div>
  
  <!-- NO BODY SLOT -->
  <div noBody>Content without body wrapper</div>
  
  <!-- OUTSIDE BODY SLOT -->
  <div outsideBody>Content outside body</div>
  
  <!-- FOOTER SLOT -->
  <div footer>Footer content</div>
  
  <!-- AFTER FOOTER SLOT -->
  <div afterFooter>Content after footer</div>
</panel>
```

## Ejemplos de Uso

### Ejemplo 1: Panel Básico

```html
<panel title="Mi Panel Básico">
  <p>Este es el contenido del panel.</p>
</panel>
```

### Ejemplo 2: Panel con Variante de Color

```html
<panel 
  title="Panel de Éxito" 
  variant="success">
  <p>Operación completada exitosamente.</p>
</panel>
```

### Ejemplo 3: Panel sin Botones

```html
<panel 
  title="Panel Estático" 
  [noButton]="true">
  <p>Este panel no tiene botones de acción.</p>
</panel>
```

### Ejemplo 4: Panel con Footer

```html
<panel title="Panel con Footer">
  <p>Contenido principal del panel.</p>
  
  <div footer>
    <button class="btn btn-primary">Guardar</button>
    <button class="btn btn-default">Cancelar</button>
  </div>
</panel>
```

### Ejemplo 5: Panel con Header Personalizado

```html
<panel>
  <div header>
    <h4 class="panel-title">
      <i class="fa fa-user"></i> Perfil de Usuario
    </h4>
    <span class="label label-success">Activo</span>
  </div>
  
  <div class="user-profile">
    <img src="avatar.jpg" />
    <h5>John Doe</h5>
    <p>john.doe@example.com</p>
  </div>
</panel>
```

### Ejemplo 6: Panel con Contenido Outside Body

```html
<panel title="Estadísticas">
  <div class="stats-content">
    <p>Contenido estadístico aquí...</p>
  </div>
  
  <div outsideBody>
    <div class="table-responsive">
      <table class="table table-striped">
        <!-- Tabla fuera del padding del body -->
      </table>
    </div>
  </div>
</panel>
```

### Ejemplo 7: Panel sin Body (noBody)

```html
<panel 
  title="Lista de Elementos" 
  [noBody]="true">
  <div noBody>
    <div class="list-group">
      <a href="#" class="list-group-item">Item 1</a>
      <a href="#" class="list-group-item">Item 2</a>
      <a href="#" class="list-group-item">Item 3</a>
    </div>
  </div>
</panel>
```

### Ejemplo 8: Panel con Clases Personalizadas

```html
<panel 
  title="Panel Personalizado"
  panelClass="panel-bordered"
  headerClass="bg-gradient"
  bodyClass="p-4"
  footerClass="text-right">
  
  <p>Contenido con estilos personalizados.</p>
  
  <div footer>
    <small>Última actualización: 15/01/2026</small>
  </div>
</panel>
```

### Ejemplo 9: Panel con ngx-datatable

```html
<panel 
  title="Lista de Usuarios" 
  footerClass="pb-0 pt-20px">
  
  <!-- Filtros en el body -->
  <div class="row mb-3">
    <div class="col-lg-6">
      <input 
        class="form-control" 
        placeholder="Buscar..." 
        (keyup)="updateFilter($event)" />
    </div>
  </div>
  
  <!-- Tabla fuera del padding del body -->
  <ng-container outsideBody>
    <hr class="m-0 bg-gray-600" />
    <div class="table-responsive text-nowrap">
      <ngx-datatable
        #table
        class="bootstrap"
        [columns]="columns"
        [rows]="rows"
        [columnMode]="ColumnMode.force"
        [headerHeight]="50"
        [footerHeight]="50"
        [rowHeight]="'auto'"
        [limit]="10">
      </ngx-datatable>
    </div>
  </ng-container>
</panel>
```

### Ejemplo 10: Panel con Formulario

```html
<panel 
  title="Nuevo Usuario" 
  variant="primary">
  
  <form [formGroup]="userForm" (ngSubmit)="onSubmit()">
    <div class="form-group">
      <label>Nombre</label>
      <input 
        type="text" 
        class="form-control" 
        formControlName="name" />
    </div>
    
    <div class="form-group">
      <label>Email</label>
      <input 
        type="email" 
        class="form-control" 
        formControlName="email" />
    </div>
    
    <div footer class="text-right">
      <button type="submit" class="btn btn-primary">
        Crear Usuario
      </button>
      <button type="button" class="btn btn-default">
        Cancelar
      </button>
    </div>
  </form>
</panel>
```

### Ejemplo 11: Múltiples Paneles en Grid

```html
<div class="row">
  <!-- Panel de Estadísticas -->
  <div class="col-lg-6">
    <panel title="Ventas del Mes" variant="success">
      <div class="stat-box">
        <h2>$125,000</h2>
        <p>Total de ventas</p>
      </div>
    </panel>
  </div>
  
  <!-- Panel de Gráficos -->
  <div class="col-lg-6">
    <panel title="Tendencias" variant="info">
      <canvas id="salesChart"></canvas>
    </panel>
  </div>
</div>

<div class="row">
  <!-- Panel Ancho Completo -->
  <div class="col-lg-12">
    <panel title="Últimas Transacciones">
      <ngx-datatable [rows]="transactions"></ngx-datatable>
    </panel>
  </div>
</div>
```

## Personalización y Estilos

### Clases CSS del Panel

#### Estructura Base

```scss
.panel {
  margin-bottom: 20px;
  border: 1px solid transparent;
  border-radius: 4px;
  box-shadow: 0 1px 2px rgba(0,0,0,.05);
  
  // Header
  .panel-heading {
    padding: 15px;
    border-bottom: 1px solid transparent;
    border-radius: 4px 4px 0 0;
    
    .panel-title {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
    }
    
    .panel-heading-btn {
      float: right;
      margin-top: -5px;
      
      .btn {
        margin-left: 2px;
      }
    }
  }
  
  // Body
  .panel-body {
    padding: 20px;
    position: relative;
    
    .panel-loader {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255,255,255,0.8);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10;
    }
  }
  
  // Footer
  .panel-footer {
    padding: 15px;
    background-color: #f5f5f5;
    border-top: 1px solid #ddd;
    border-radius: 0 0 4px 4px;
  }
}
```

#### Estados del Panel

```scss
// Panel Expandido
.panel-expand {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 9999;
  margin: 0;
  border-radius: 0;
  
  .panel-body {
    height: calc(100vh - 100px);
    overflow-y: auto;
  }
}

// Panel en Carga
.panel-loading {
  .panel-loader {
    display: flex !important;
  }
}

// Variantes de Color
.panel-inverse {
  .panel-heading {
    background-color: #2d353c;
    color: #fff;
    border-color: #2d353c;
  }
}

.panel-primary {
  .panel-heading {
    background-color: #007bff;
    color: #fff;
    border-color: #007bff;
  }
}

.panel-success {
  .panel-heading {
    background-color: #00acac;
    color: #fff;
    border-color: #00acac;
  }
}

.panel-warning {
  .panel-heading {
    background-color: #f59c1a;
    color: #fff;
    border-color: #f59c1a;
  }
}

.panel-danger {
  .panel-heading {
    background-color: #ff5b57;
    color: #fff;
    border-color: #ff5b57;
  }
}
```

### Personalización Avanzada

#### 1. Panel con Gradiente

```html
<panel 
  title="Panel Gradiente"
  headerClass="bg-gradient-primary">
  <p>Contenido del panel</p>
</panel>
```

```scss
.panel-heading.bg-gradient-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}
```

#### 2. Panel con Sombra Personalizada

```html
<panel 
  title="Panel Elevado"
  panelClass="panel-shadow-lg">
  <p>Panel con sombra grande</p>
</panel>
```

```scss
.panel-shadow-lg {
  box-shadow: 0 10px 40px rgba(0,0,0,0.15);
}
```

#### 3. Panel con Border Personalizado

```html
<panel 
  title="Panel Bordeado"
  panelClass="panel-bordered-primary">
  <p>Panel con borde de color</p>
</panel>
```

```scss
.panel-bordered-primary {
  border-left: 4px solid #007bff;
}
```

## Integración con Otros Componentes

### Con Angular Forms

```typescript
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-user-form',
  template: `
    <panel title="Formulario de Usuario" variant="primary">
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label>Nombre *</label>
          <input 
            type="text" 
            class="form-control" 
            formControlName="name"
            [class.is-invalid]="form.get('name').invalid && form.get('name').touched" />
          <div class="invalid-feedback">
            El nombre es requerido
          </div>
        </div>
        
        <div footer class="text-right">
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid">
            Guardar
          </button>
        </div>
      </form>
    </panel>
  `
})
export class UserFormComponent implements OnInit {
  form: FormGroup;
  
  constructor(private fb: FormBuilder) {}
  
  ngOnInit() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
  }
  
  onSubmit() {
    if (this.form.valid) {
      console.log(this.form.value);
    }
  }
}
```

### Con RxJS y Observables

```typescript
import { Component, OnInit } from '@angular/core';
import { Observable, interval } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-live-panel',
  template: `
    <panel title="Datos en Tiempo Real">
      <div class="live-data">
        <h3>{{ currentTime$ | async }}</h3>
        <p>Actualizándose cada segundo</p>
      </div>
    </panel>
  `
})
export class LivePanelComponent implements OnInit {
  currentTime$: Observable<string>;
  
  ngOnInit() {
    this.currentTime$ = interval(1000).pipe(
      map(() => new Date().toLocaleTimeString())
    );
  }
}
```

### Con Servicios HTTP

```typescript
import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PanelComponent } from './panel.component';

@Component({
  selector: 'app-data-panel',
  template: `
    <panel 
      #dataPanel
      title="Lista de Usuarios"
      variant="inverse">
      
      <div *ngIf="loading" class="text-center">
        <div class="spinner-border"></div>
      </div>
      
      <div *ngIf="!loading && users.length > 0">
        <div class="list-group">
          <div *ngFor="let user of users" class="list-group-item">
            {{ user.name }}
          </div>
        </div>
      </div>
      
      <div footer>
        <button class="btn btn-primary" (click)="reloadData()">
          <i class="fa fa-refresh"></i> Recargar
        </button>
      </div>
    </panel>
  `
})
export class DataPanelComponent implements OnInit {
  @ViewChild('dataPanel') panel: PanelComponent;
  
  users: any[] = [];
  loading = false;
  
  constructor(private http: HttpClient) {}
  
  ngOnInit() {
    this.loadData();
  }
  
  loadData() {
    this.loading = true;
    this.http.get<any[]>('/api/users').subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }
  
  reloadData() {
    this.panel.panelReload();
    setTimeout(() => this.loadData(), 500);
  }
}
```

## Mejores Prácticas

### 1. Organización de Contenido

✅ **Hacer:**
```html
<!-- Estructura clara y organizada -->
<panel title="Dashboard">
  <!-- Filtros en el body -->
  <div class="filters">
    <input type="text" placeholder="Buscar..." />
  </div>
  
  <!-- Contenido tabulado fuera del padding -->
  <div outsideBody>
    <ngx-datatable [rows]="data"></ngx-datatable>
  </div>
  
  <!-- Acciones en el footer -->
  <div footer>
    <button class="btn btn-primary">Exportar</button>
  </div>
</panel>
```

❌ **Evitar:**
```html
<!-- Estructura desordenada -->
<panel title="Dashboard">
  <input type="text" />
  <ngx-datatable [rows]="data"></ngx-datatable>
  <button class="btn btn-primary">Exportar</button>
  <!-- Sin organización clara de secciones -->
</panel>
```

### 2. Uso de Variantes

✅ **Hacer:**
```html
<!-- Usar variantes con significado semántico -->
<panel title="Error al Procesar" variant="danger">
  <p>La operación falló. Por favor, intente nuevamente.</p>
</panel>

<panel title="Operación Exitosa" variant="success">
  <p>Los datos se guardaron correctamente.</p>
</panel>
```

❌ **Evitar:**
```html
<!-- Usar variantes sin sentido semántico -->
<panel title="Error al Procesar" variant="success">
  <p>La operación falló...</p> <!-- Inconsistencia -->
</panel>
```

### 3. Manejo de Estados

✅ **Hacer:**
```typescript
// Sincronizar estado del panel con la lógica de negocio
export class MyComponent {
  @ViewChild('myPanel') panel: PanelComponent;
  
  async reloadData() {
    this.panel.reload = true;
    try {
      const data = await this.apiService.getData();
      this.updateView(data);
    } finally {
      this.panel.reload = false;
    }
  }
}
```

❌ **Evitar:**
```typescript
// Dejar el estado de recarga sin sincronizar
reloadData() {
  this.panel.panelReload(); // Se resetea automáticamente sin datos reales
}
```

### 4. Accesibilidad

✅ **Hacer:**
```html
<panel title="Configuración" [attr.aria-label]="'Panel de configuración'">
  <button 
    class="btn btn-primary"
    [attr.aria-describedby]="'save-help'">
    Guardar
  </button>
  <small id="save-help">Guarda los cambios permanentemente</small>
</panel>
```

### 5. Responsive Design

✅ **Hacer:**
```html
<!-- Paneles que se adaptan al tamaño de pantalla -->
<div class="row">
  <div class="col-lg-4 col-md-6 col-sm-12">
    <panel title="Panel 1" variant="primary">
      <p>Contenido adaptable</p>
    </panel>
  </div>
  
  <div class="col-lg-4 col-md-6 col-sm-12">
    <panel title="Panel 2" variant="success">
      <p>Contenido adaptable</p>
    </panel>
  </div>
</div>
```

### 6. Performance

✅ **Hacer:**
```typescript
// Usar ChangeDetectionStrategy.OnPush cuando sea posible
@Component({
  selector: 'app-heavy-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <panel title="Datos">
      <div *ngFor="let item of items$ | async">
        {{ item.name }}
      </div>
    </panel>
  `
})
export class HeavyPanelComponent {
  items$ = this.service.getItems();
}
```

### 7. Testing

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PanelComponent } from './panel.component';

describe('PanelComponent', () => {
  let component: PanelComponent;
  let fixture: ComponentFixture<PanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PanelComponent ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle expand state', () => {
    expect(component.expand).toBe(false);
    component.panelExpand();
    expect(component.expand).toBe(true);
    component.panelExpand();
    expect(component.expand).toBe(false);
  });

  it('should show reload state temporarily', (done) => {
    expect(component.reload).toBe(false);
    component.panelReload();
    expect(component.reload).toBe(true);
    
    setTimeout(() => {
      expect(component.reload).toBe(false);
      done();
    }, 1600);
  });

  it('should toggle collapse state', () => {
    expect(component.collapse).toBe(false);
    component.panelCollapse();
    expect(component.collapse).toBe(true);
  });

  it('should remove panel', () => {
    expect(component.remove).toBe(false);
    component.panelRemove();
    expect(component.remove).toBe(true);
  });

  it('should detect footer content', (done) => {
    const footerElement = fixture.nativeElement.querySelector('.panel-footer');
    footerElement.innerHTML = '<button>Test</button>';
    
    component.ngAfterViewInit();
    
    setTimeout(() => {
      expect(component.showFooter).toBe(true);
      done();
    }, 100);
  });
});
```

## Patrones de Uso Comunes

### 1. Panel Dashboard con Estadísticas

```html
<div class="row">
  <div class="col-lg-3 col-md-6">
    <panel variant="primary" [noButton]="true">
      <div class="stat-card text-center">
        <i class="fa fa-users fa-3x mb-3"></i>
        <h3>1,234</h3>
        <p>Usuarios Activos</p>
      </div>
    </panel>
  </div>
  
  <div class="col-lg-3 col-md-6">
    <panel variant="success" [noButton]="true">
      <div class="stat-card text-center">
        <i class="fa fa-dollar-sign fa-3x mb-3"></i>
        <h3>$56,789</h3>
        <p>Ventas del Mes</p>
      </div>
    </panel>
  </div>
</div>
```

### 2. Panel de Configuración

```html
<panel title="Configuración del Sistema" variant="inverse">
  <form [formGroup]="configForm">
    <div class="form-group">
      <label>Nombre de la Aplicación</label>
      <input type="text" class="form-control" formControlName="appName" />
    </div>
    
    <div class="form-group">
      <label>Email de Contacto</label>
      <input type="email" class="form-control" formControlName="contactEmail" />
    </div>
    
    <div class="form-check">
      <input type="checkbox" class="form-check-input" formControlName="enableNotifications" />
      <label class="form-check-label">Habilitar Notificaciones</label>
    </div>
    
    <div footer class="text-right">
      <button type="button" class="btn btn-default">Cancelar</button>
      <button type="submit" class="btn btn-primary">Guardar Cambios</button>
    </div>
  </form>
</panel>
```

### 3. Panel de Notificaciones

```html
<panel title="Notificaciones Recientes">
  <div class="notification-list">
    <div *ngFor="let notification of notifications" 
         class="notification-item"
         [class.unread]="!notification.read">
      <div class="notification-icon">
        <i [class]="notification.icon"></i>
      </div>
      <div class="notification-content">
        <h5>{{ notification.title }}</h5>
        <p>{{ notification.message }}</p>
        <small>{{ notification.timestamp | date:'short' }}</small>
      </div>
    </div>
  </div>
  
  <div footer class="text-center">
    <a href="/notifications">Ver todas las notificaciones</a>
  </div>
</panel>
```

## Troubleshooting

### Problema 1: El footer no se muestra

**Causa:** El contenido del footer está vacío o se renderiza después de `ngAfterViewInit`.

**Solución:**
```typescript
// Forzar detección del footer
ngAfterContentInit() {
  this.checkFooterContent();
}

checkFooterContent() {
  setTimeout(() => {
    if (this.panelFooter?.nativeElement) {
      this.showFooter = this.panelFooter.nativeElement.children.length > 0;
    }
  }, 100);
}
```

### Problema 2: Los botones no funcionan

**Causa:** Eventos no enlazados correctamente.

**Solución:** Verificar que los métodos estén definidos en el componente:
```typescript
// Verificar que estos métodos existan
panelExpand() { }
panelReload() { }
panelCollapse() { }
panelRemove() { }
```

### Problema 3: El panel no se expande correctamente

**Causa:** CSS conflictivo o z-index bajo.

**Solución:**
```scss
.panel-expand {
  z-index: 9999 !important;
  position: fixed !important;
}
```

## Recursos Adicionales

### Componentes Relacionados

- **ngx-datatable**: Para tablas de datos dentro del panel
- **Charts**: Para gráficos y visualizaciones
- **Forms**: Para formularios dentro del panel

### Referencias

- [Angular Content Projection](https://angular.io/guide/content-projection)
- [ViewChild Documentation](https://angular.io/api/core/ViewChild)
- [Component Lifecycle Hooks](https://angular.io/guide/lifecycle-hooks)

---

**Última actualización:** 15 de enero de 2026  
**Componente:** Panel Component v1.0  
**Compatibilidad:** Angular 19-20+
