# Google Maps JS API v3.65 (weekly) en Angular — guía práctica

## Objetivo

Documentar cómo usar Google Maps JavaScript API en Angular moderno dentro de este repo,
siguiendo la referencia actual confirmada para el canal `weekly`.

Estado verificado: 20 de junio de 2026

- `weekly`: v3.65
- `quarterly`: v3.64

## Patrón recomendado

### 1. Backend mínimo

El backend idealmente expone solo:

- validación del token/link
- `apiKey`
- `mapId`
- endpoint de guardado

La UI del mapa vive en Angular.

### 2. Componente Angular

Separar:

- lógica común del mapa
- wrapper visual mobile
- wrapper visual web

### 3. Loader de Maps

Patrón mínimo aceptable:

```ts
window.__vbGoogleMapsLoaded = () => resolve();
script.src =
  `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places,geocoding&loading=async&callback=__vbGoogleMapsLoaded`;
```

Puntos clave:

- `loading=async`
- `callback` cuando el script se inserta manualmente
- `libraries=places,geocoding` si se usará autocomplete + geocoder
- no insertar múltiples scripts duplicados
- si ya existe loader, reutilizarlo

### 4. Importación de librerías

Patrón recomendado dentro del componente:

```ts
const { Map } = await google.maps.importLibrary('maps');
const { Geocoder } = await google.maps.importLibrary('geocoding');
await google.maps.importLibrary('places');

const map = new Map(host, options);
const geocoder = new Geocoder();
```

Puntos clave:

- no asumir que `google.maps.Geocoder` ya existe
- `Geocoder` pertenece a la librería `geocoding`
- crear `Geocoder` antes de importar `geocoding` produce el error
  `window.google.maps.Geocoder is not a constructor`

## Places

### Opción moderna

`google.maps.places.PlaceAutocompleteElement`

Ventajas:

- flujo recomendado por Google
- mejor alineado con la evolución de Places
- reduce dependencia en patrones legacy

### Fallback útil

`google.maps.places.Autocomplete(input, options)`

Útil cuando el widget moderno no está disponible o se necesita compatibilidad inmediata.

### Último fallback

`Geocoder` manual sobre texto libre del usuario.

## Orden de inicialización sugerido

1. parsear inputs / token
2. pedir config al backend
3. cargar script
4. importar librerías requeridas con `importLibrary`
5. crear mapa
6. crear geocoder
7. montar autocomplete
8. habilitar geolocalización y reverse geocode

## Telemetría recomendada

Registrar al menos:

- `Boot`
- `Config OK`
- `Script OK`
- `Map initialized`
- `Places: Autocomplete listo`
- `Places: Fallback manual`
- `Places: Error`
- `Boot: Error`
- error de `geocoding` / `importLibrary`

## Señales de que algo está mal

- mapa queda en “cargando” pero no hay `Map initialized`
- host Angular condicional nunca dispara boot
- aparece `HostState` pero no logs del componente
- autocomplete manual funciona pero no hay sugerencias de Places
- sale `window.google.maps.Geocoder is not a constructor`
- aparece solo el warning amarillo de carga y se asume incorrectamente que ese warning es la raíz del fallo

## Hallazgos concretos de este repo

- En `VoiceBot.Web`, el host del mapa puede depender de render condicional; usar `viewChild(...)`
  reactivo evita que el boot corra antes de tiempo.
- El cambio de `iframe` a componente Angular compartido simplifica el flujo y evita problemas de
  permisos/CSP/embed.
- El warning de carga directa de Maps no fue la causa real del fallo funcional observado.
- La causa real revisada fue la creación de `Geocoder` sin importar `geocoding` con la API moderna.

## Integración con este repo

En `VoiceBot.Web`:

- respetar layout `mobile` vs `web`
- no mezclar CSS de Ionic con Color Admin en un solo shell visual
- compartir la lógica del mapa en un componente standalone común

## Qué evitar

- volver a mover el mapa a `iframe` por conveniencia
- depender solo del widget moderno sin fallback
- hardcodear `apiKey` en environments si ya existe un flujo seguro desde backend
- mezclar diagnóstico de consola con suposiciones; validar primero `Config OK`, `Script OK` y
  `Map initialized`
