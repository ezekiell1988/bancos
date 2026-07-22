# Reference: Tabla completa de propiedades AppSettings

Referencia rápida de todas las propiedades de `AppSettings` disponibles para configurar el layout de Color Admin desde cualquier componente Angular.

## Importación

```typescript
import { AppSettings } from '../service/app-settings.service';

constructor(public appSettings: AppSettings) { }
```

## Propiedades de layout general

| Propiedad              | Tipo    | Default | Efecto                                             |
|-----------------------|---------|---------|-----------------------------------------------------|
| `appTheme`             | string  | `''`    | Tema de color de la app                            |
| `appCover`             | string  | `''`    | Imagen de portada/fondo                            |
| `appDarkMode`          | boolean | false   | Activa el modo oscuro                              |
| `appGradientEnabled`   | boolean | false   | Activa gradientes                                  |
| `appBodyWhite`         | boolean | false   | Fondo blanco en el body                            |
| `appBoxedLayout`       | boolean | false   | Layout en caja (alternativa a `body.boxed-layout`) |
| `appClass`             | string  | `''`    | Clases CSS adicionales para el wrapper principal   |

## Propiedades del header

| Propiedad                     | Tipo    | Default | Efecto                                  |
|------------------------------|---------|---------|------------------------------------------|
| `appHeaderNone`               | boolean | false   | Oculta el header                        |
| `appHeaderFixed`              | boolean | true    | Header fijo en la parte superior        |
| `appHeaderInverse`            | boolean | false   | Header con colores invertidos           |
| `appHeaderMegaMenu`           | boolean | false   | Activa el mega-menu en el header        |
| `appHeaderLanguageBar`        | boolean | false   | Muestra selector de idioma en el header |
| `appTopMenu`                  | boolean | false   | Muestra barra de menú horizontal        |
| `appFooter`                   | boolean | false   | Muestra el footer global de la app      |

## Propiedades del sidebar

| Propiedad                | Tipo    | Default | Efecto                                              |
|-------------------------|---------|---------|------------------------------------------------------|
| `appSidebarNone`         | boolean | false   | Oculta el sidebar completamente                    |
| `appSidebarFixed`        | boolean | true    | Sidebar fijo (no scrollea con el contenido)        |
| `appSidebarLight`        | boolean | false   | Tema claro para el sidebar                         |
| `appSidebarTransparent`  | boolean | false   | Sidebar con fondo transparente                     |
| `appSidebarMinified`     | boolean | false   | Colapsa el sidebar a solo íconos                   |
| `appSidebarWide`         | boolean | false   | Sidebar más ancho                                  |
| `appSidebarGrid`         | boolean | false   | Layout de grid en el sidebar                       |
| `appSidebarSearch`       | boolean | false   | Campo de búsqueda en el sidebar                    |
| `appSidebarCollapsed`    | boolean | false   | Sidebar colapsado (mobile-first)                   |
| `appSidebarEnd`          | boolean | false   | Sidebar en el lado derecho (end)                   |
| `appSidebarTwo`          | boolean | false   | Layout con dos sidebars                            |
| `appSidebarEndToggled`   | boolean | false   | Sidebar derecho visible al iniciar                 |

## Propiedades del contenido

| Propiedad               | Tipo    | Default | Efecto                                               |
|------------------------|---------|---------|-------------------------------------------------------|
| `appContentClass`       | string  | `''`    | Clases CSS adicionales para `#content`              |
| `appContentFullHeight`  | boolean | false   | Contenido al 100% de la altura                       |
| `appContentFullWidth`   | boolean | false   | Contenido al 100% del ancho (sin padding lateral)    |

## Clase body directa (para boxed layout)

Algunas opciones se aplican directamente en `document.body.className`:

```typescript
// Activar
document.body.className = document.body.className + ' boxed-layout';

// Desactivar (siempre en ngOnDestroy)
document.body.className = document.body.className.replace('boxed-layout', '').trim();
```

| Clase body       | Efecto                                    |
|-----------------|-------------------------------------------|
| `boxed-layout`  | Contenedor centrado con ancho máximo     |
