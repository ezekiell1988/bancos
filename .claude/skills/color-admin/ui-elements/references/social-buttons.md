# Reference: Social Buttons

## Estructura base de un botón social

```html
<a href="javascript:;" class="btn btn-sm btn-social btn-{plataforma}">
  <span class="fab fa-{icono}"></span> Sign in with {Plataforma}
</a>
```

## Catálogo de botones sociales

| Plataforma    | Clase                         | Ícono FA              | Color hex   |
|---------------|-------------------------------|----------------------|-------------|
| App.net       | `btn-social btn-adn`          | `fa-adn`             | #D87A68     |
| Bitbucket     | `btn-social btn-bitbucket`    | `fa-bitbucket`       | #205081     |
| Dropbox       | `btn-social btn-dropbox`      | `fa-dropbox`         | #1087DD     |
| Facebook      | `btn-social btn-facebook`     | `fa-facebook`        | #3B5998     |
| Flickr        | `btn-social btn-flickr`       | `fa-flickr`          | #2BA9E1     |
| Foursquare    | `btn-social btn-foursquare`   | `fa-foursquare`      | #f94877     |
| GitHub        | `btn-social btn-github`       | `fa-github`          | #444444     |
| Google        | `btn-social btn-google`       | `fa-google`          | #DD4B39     |
| Instagram     | `btn-social btn-instagram`    | `fa-instagram`       | #3F729B     |
| LinkedIn      | `btn-social btn-linkedin`     | `fa-linkedin`        | #007BB6     |
| Microsoft     | `btn-social btn-microsoft`    | `fa-windows`         | #2672EC     |
| Odnoklassniki | `btn-social btn-odnoklassniki`| `fa-odnoklassniki`   | #F4731C     |
| OpenID        | `btn-social btn-openid`       | `fa-openid`          | #F7931E     |
| Pinterest     | `btn-social btn-pinterest`    | `fa-pinterest`       | #CB2027     |
| Reddit        | `btn-social btn-reddit`       | `fa-reddit`          | #EFF7FF     |
| SoundCloud    | `btn-social btn-soundcloud`   | `fa-soundcloud`      | #FF5500     |
| Tumblr        | `btn-social btn-tumblr`       | `fa-tumblr`          | #CB2027     |
| Twitter       | `btn-social btn-twitter`      | `fa-twitter`         | #55ACEE     |
| Vimeo         | `btn-social btn-vimeo`        | `fa-vimeo-square`    | #1AB7EA     |
| VK            | `btn-social btn-vk`           | `fa-vk`              | #587EA3     |
| Yahoo         | `btn-social btn-yahoo`        | `fa-yahoo`           | #720E9E     |
| Weibo         | `btn-social btn-weibo`        | `fa-weibo`           | #DD302A     |
| Spotify       | `btn-social btn-spotify`      | `fa-spotify`         | #1DB954     |
| Steam         | `btn-social btn-steam`        | `fa-steam`           | #1B2838     |
| YouTube       | `btn-social btn-youtube`      | `fa-youtube`         | #FF0000     |

## Ejemplos de uso

```html
<!-- Botón de login con Facebook -->
<a href="#" class="btn btn-social btn-facebook">
  <span class="fab fa-facebook"></span> Sign in with Facebook
</a>

<!-- Botón de login con Google -->
<a href="#" class="btn btn-social btn-google">
  <span class="fab fa-google"></span> Sign in with Google
</a>

<!-- Botón de login con GitHub -->
<a href="#" class="btn btn-social btn-github">
  <span class="fab fa-github"></span> Sign in with GitHub
</a>

<!-- Botón tamaño pequeño con ancho fijo -->
<a href="javascript:;" class="btn btn-sm w-250px btn-social btn-twitter">
  <span class="fab fa-twitter"></span> Sign in with Twitter
</a>
```

## Variantes de tamaño

```html
<a class="btn btn-xs btn-social btn-facebook">...</a>   <!-- Extra pequeño -->
<a class="btn btn-sm btn-social btn-facebook">...</a>   <!-- Pequeño -->
<a class="btn btn-social btn-facebook">...</a>          <!-- Por defecto -->
<a class="btn btn-lg btn-social btn-facebook">...</a>   <!-- Grande -->
```

## Botón solo con ícono (sin texto)

```html
<a href="#" class="btn btn-social-icon btn-facebook">
  <span class="fab fa-facebook"></span>
</a>
<a href="#" class="btn btn-social-icon btn-google">
  <span class="fab fa-google"></span>
</a>
```
