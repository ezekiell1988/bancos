# Color Admin Components - Referencias

Esta carpeta contiene guías detalladas de implementación para los componentes de Color Admin utilizados en la aplicación.

## Archivos Disponibles

### 1. panel-component.md

Guía completa del componente Panel de Color Admin:

- **Arquitectura y estructura** del componente
- **Propiedades de entrada** (inputs) con todas las opciones
- **Funcionalidades principales** (expand, reload, collapse, remove)
- **Content Projection** avanzado con múltiples slots
- **11 ejemplos prácticos** de uso
- **Personalización y estilos** CSS
- **Integración** con Forms, RxJS, y HTTP
- **Mejores prácticas** y patrones recomendados
- **Testing** con ejemplos de tests unitarios
- **Troubleshooting** de problemas comunes

**Casos de uso:**
- Contenedores de contenido con acciones
- Paneles de dashboard
- Formularios con header y footer
- Listados con tablas integradas
- Notificaciones y mensajes

### 2. ngx-datatable-guide.md

Guía completa de ngx-datatable para tablas de datos:

- **Instalación y configuración** inicial
- **Características principales** (Virtual DOM, paginación, sorting, etc.)
- **Implementación con API** backend
- **Paginación del lado del servidor** (server-side pagination)
- **Filtrado y búsqueda** avanzada
- **Ordenamiento** (client-side y server-side)
- **Ejemplos prácticos** con templates personalizados
- **Optimización** y mejores prácticas de rendimiento
- **Integración** con RxJS y state management
- **Testing** y manejo de errores

**Casos de uso:**
- Listados de datos con grandes volúmenes (>1000 registros)
- Tablas con paginación, filtrado y ordenamiento
- Selección de filas (single/multi)
- Detalles expandibles por fila
- Acciones inline (editar, eliminar)
- Exportación de datos

## Cómo Usar Estos Recursos

### Para Implementar un Panel

1. Lee [panel-component.md](./panel-component.md)
2. Identifica el ejemplo que más se acerca a tu caso de uso
3. Copia y adapta el código a tus necesidades
4. Personaliza variantes, clases y estilos
5. Implementa los métodos de negocio necesarios

### Para Implementar una Tabla

1. Lee [ngx-datatable-guide.md](./ngx-datatable-guide.md)
2. Decide si necesitas paginación client-side o server-side
3. Configura las columnas según tus datos
4. Implementa filtrado y ordenamiento si es necesario
5. Optimiza el rendimiento con virtual scrolling
6. Agrega templates personalizados para columnas especiales

### Patrón Común: Panel + Tabla

El patrón más común es combinar ambos componentes:

```html
<panel 
  title="Lista de Datos" 
  variant="inverse"
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
  
  <!-- Tabla fuera del padding -->
  <div outsideBody>
    <ngx-datatable
      [rows]="rows"
      [columns]="columns"
      [externalPaging]="true">
    </ngx-datatable>
  </div>
</panel>
```

## Notas de Implementación

### Panel Component

- Siempre usa la variante apropiada según el contexto
- Aprovecha los slots de content projection
- Implementa lazy loading para contenido pesado
- Sincroniza el estado de reload con llamadas API reales

### ngx-datatable

- Usa `externalPaging="true"` para datasets grandes
- Implementa debouncing en filtros de búsqueda
- Habilita virtual scrolling para >100 filas
- Cachea resultados de API cuando sea apropiado
- Usa ChangeDetectionStrategy.OnPush para mejor rendimiento

## Actualizaciones

Estos archivos se actualizan cuando:
- Se agregan nuevos componentes de Color Admin
- Se descubren nuevos patrones de uso
- Se identifican problemas comunes y soluciones
- Se actualizan versiones de dependencias

Para agregar más componentes, sigue el mismo formato de documentación detallada.

## Compatibilidad

- **Angular:** 19-20+
- **ngx-datatable:** 22.0.0+
- **Color Admin:** Template v5+

---

**Última actualización:** 10 de febrero de 2026
