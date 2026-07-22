# Reference: Icon FontAwesome (v6.x)

> El CSS de FontAwesome está compilado en `vendor.min.css`. Solo inclúyelo y podrás usar todos los íconos.

## Uso básico

```html
<!-- Sólido (fas) -->
<i class="fas fa-{nombre}"></i>

<!-- Regular (far) -->
<i class="far fa-{nombre}"></i>

<!-- Brands (fab) -->
<i class="fab fa-{nombre}"></i>
```

## Tamaños

```html
<i class="fas fa-camera-retro fa-xs"></i>    <!-- Extra pequeño -->
<i class="fas fa-camera-retro fa-sm"></i>    <!-- Pequeño -->
<i class="fas fa-camera-retro fa-lg"></i>    <!-- Large (33% mayor) -->
<i class="fas fa-camera-retro fa-2x"></i>    <!-- 2x -->
<i class="fas fa-camera-retro fa-3x"></i>    <!-- 3x -->
<i class="fas fa-camera-retro fa-5x"></i>    <!-- 5x -->
<i class="fas fa-camera-retro fa-7x"></i>    <!-- 7x -->
<i class="fas fa-camera-retro fa-10x"></i>   <!-- 10x -->
```

## Ancho fijo (alineación en listas)

```html
<i class="fas fa-home fa-fw"></i> Home
<i class="fas fa-info fa-fw"></i> Info
<i class="fas fa-book fa-fw"></i> Library
<i class="fas fa-cog fa-fw"></i> Settings
```

## Íconos animados

```html
<!-- Spin continuo -->
<i class="fas fa-spinner fa-spin"></i>
<i class="fas fa-circle-notch fa-spin"></i>
<i class="fas fa-sync fa-spin"></i>
<i class="fas fa-cog fa-spin"></i>

<!-- Pulse (8 pasos) -->
<i class="fas fa-spinner fa-pulse"></i>
```

## Rotación y volteo

```html
<i class="fas fa-arrow-alt-circle-right"></i>               <!-- Normal -->
<i class="fas fa-arrow-alt-circle-right fa-rotate-90"></i>  <!-- 90° -->
<i class="fas fa-arrow-alt-circle-right fa-rotate-180"></i> <!-- 180° -->
<i class="fas fa-arrow-alt-circle-right fa-rotate-270"></i> <!-- 270° -->
<i class="fas fa-arrow-alt-circle-right fa-flip-horizontal"></i> <!-- Espejo H -->
<i class="fas fa-arrow-alt-circle-right fa-flip-vertical"></i>   <!-- Espejo V -->
```

## Íconos apilados (fa-stack)

```html
<!-- Ícono sobre cuadrado -->
<span class="fa-stack fa-4x">
  <i class="fa fa-square fa-stack-2x"></i>
  <i class="fab fa-twitter text-white fa-stack-1x"></i>
</span>

<!-- Ícono con círculo -->
<span class="fa-stack fa-2x">
  <i class="fas fa-circle fa-stack-2x text-primary"></i>
  <i class="fas fa-home fa-stack-1x text-white"></i>
</span>

<!-- Float start (texto envuelve el ícono) -->
<span class="fa-stack fa-4x float-start me-10px">
  <i class="fab fa-twitter fa-stack-1x"></i>
</span>
```

## Listas con íconos (fa-ul / fa-li)

```html
<ul class="fa-ul">
  <li><span class="fa-li"><i class="fas fa-check-square"></i></span>Item con check</li>
  <li><span class="fa-li"><i class="fas fa-check-square"></i></span>Otro item</li>
  <li class="disabled"><span class="fa-li"><i class="fas fa-spinner fa-pulse"></i></span>Cargando...</li>
</ul>
```

## Íconos más usados (selección)

| Ícono          | Código                    | Uso típico              |
|----------------|---------------------------|-------------------------|
| Home           | `fas fa-home`             | Inicio / home           |
| User           | `fas fa-user`             | Usuario                 |
| Users          | `fas fa-users`            | Grupo de usuarios       |
| Cog            | `fas fa-cog`              | Configuración           |
| Cogs           | `fas fa-cogs`             | Configuraciones         |
| List           | `fas fa-list`             | Lista                   |
| Search         | `fas fa-search`           | Buscar                  |
| Plus           | `fas fa-plus`             | Agregar                 |
| Minus          | `fas fa-minus`            | Quitar                  |
| Times / X      | `fas fa-times`            | Cerrar                  |
| Check          | `fas fa-check`            | Correcto                |
| Info Circle    | `fas fa-info-circle`      | Información             |
| Exclamation    | `fas fa-exclamation`      | Alerta                  |
| Edit / Pencil  | `fas fa-pencil-alt`       | Editar                  |
| Trash          | `fas fa-trash`            | Eliminar                |
| Save           | `fas fa-save`             | Guardar                 |
| Download       | `fas fa-download`         | Descargar               |
| Upload         | `fas fa-upload`           | Subir                   |
| Eye            | `fas fa-eye`              | Ver                     |
| Eye Slash      | `fas fa-eye-slash`        | Ocultar                 |
| Lock           | `fas fa-lock`             | Bloqueado               |
| Unlock         | `fas fa-unlock`           | Desbloqueado            |
| Calendar       | `fas fa-calendar`         | Calendario              |
| Clock          | `fas fa-clock`            | Hora                    |
| Comment        | `fas fa-comment`          | Comentario              |
| Bell           | `fas fa-bell`             | Notificación            |
| Chart Bar      | `fas fa-chart-bar`        | Gráfico de barras       |
| Chart Line     | `fas fa-chart-line`       | Gráfico de líneas       |
| Dollar Sign    | `fas fa-dollar-sign`      | Dinero                  |
| Credit Card    | `fas fa-credit-card`      | Pago / tarjeta          |
| Wallet         | `fas fa-wallet`           | Billetera               |
| Twitter        | `fab fa-twitter`          | Twitter                 |
| Facebook       | `fab fa-facebook`         | Facebook                |
| Google         | `fab fa-google`           | Google                  |
| GitHub         | `fab fa-github`           | GitHub                  |
| Apple          | `fab fa-apple`            | Apple                   |
| Android        | `fab fa-android`          | Android                 |
| Windows        | `fab fa-windows`          | Windows                 |

## Combinación con textos en botones

```html
<a class="btn btn-default">
  <i class="fa fa-comment"></i> Comment
</a>
<a class="btn btn-default">
  <i class="fa fa-cog"></i>
</a>
```
