---
name: angular-gmap
description: >
  Guía para integrar Google Maps JavaScript API en Angular moderno dentro de este repo.
  Usar cuando se trabaje con mapas, Places Autocomplete, PlaceAutocompleteElement,
  geocodificación, geolocalización, loaders de Google Maps, importLibrary, widgets de
  dirección o componentes compartidos de mapa en VoiceBot.Web. Triggers: google maps,
  gmap, autocomplete, places, placeautocompleteelement, mapa, geocoder, geolocation,
  importlibrary, js-api-loading, address picker.
---

# angular-gmap

Skill para implementar y mantener integraciones de Google Maps en Angular moderno dentro de
`src/VoiceBot.Web`, con foco en la versión actual de la JavaScript API y los widgets de Places.

## Cuándo usarlo

Activar este skill cuando:

- se agregue un mapa nuevo en Angular
- se migre un mapa legado HTML/JS a componentes Angular
- falle o se degrade el autocomplete de direcciones
- aparezcan warnings de carga de Google Maps
- se necesite decidir entre `PlaceAutocompleteElement`, `Autocomplete` o búsqueda manual

## Principios del repo

1. No usar `iframe` para experiencia embebida si el mapa debe convivir con el árbol Angular.
2. Compartir la lógica del mapa; separar presentación `mobile` y `web` según el layout del repo.
3. Mantener el flujo seguro del backend existente (token, validación, save endpoint) y mover solo la UI al frontend cuando sea posible.
4. Preferir la API moderna de Google Maps y Places antes que patrones legacy.

## Recomendación base

### Loader

Usar carga moderna y explícita del script de Google Maps:

- incluir `loading=async`
- usar `callback` cuando se inserte el script manualmente
- cargar `libraries=places,geocoding` si habrá autocomplete y geocoder
- evitar patrones que disparen warnings de carga subóptima
- si ya existe un script previo, reutilizarlo en vez de insertar otro

### Places

Orden de preferencia:

1. `google.maps.places.PlaceAutocompleteElement`
2. `google.maps.places.Autocomplete` como fallback
3. búsqueda manual con `Geocoder` como último fallback

### Geocoding

`Geocoder` no debe asumirse disponible solo por existir `window.google.maps`.

Patrón recomendado:

- esperar a que cargue el script
- usar `await google.maps.importLibrary('geocoding')`
- crear `const { Geocoder } = await google.maps.importLibrary('geocoding')`
- luego instanciar `new Geocoder()`

Si aparece el error `window.google.maps.Geocoder is not a constructor`, asumir primero
que faltó importar la librería `geocoding` o que se intentó instanciar demasiado pronto.

### Angular

- encapsular Maps en un componente standalone
- si el repo tiene versión `mobile` y `web`, compartir la lógica y adaptar el shell visual
- si el host aparece condicionalmente, usar queries reactivas (`viewChild(...)`) o lifecycle correcto para no perder la inicialización del mapa
- si el boot depende del host del mapa, esperar `viewChild(...)` reactivo antes de iniciar la carga

## Checklist de implementación

1. Crear o ubicar el componente Angular del mapa.
2. Resolver el token/params desde la URL o inputs del host.
3. Consultar config mínima al backend (`apiKey`, `mapId`, etc.).
4. Cargar Google Maps JS con `loading=async`, `callback` y librerías correctas.
5. Esperar `importLibrary('maps')`, `importLibrary('places')` y `importLibrary('geocoding')` según el caso.
6. Inicializar el mapa.
7. Montar autocomplete moderno de Places.
8. Mantener fallback manual con `Geocoder`.
8. Instrumentar logs de:
   - boot
   - config ok/error
   - script ok/error
   - map initialized
   - autocomplete listo/error/fallback
   - geocoder/importLibrary error
9. Validar build Angular.
10. Si hay incidente real, revisar consola y logs del backend juntos; el warning amarillo de carga no siempre explica la falla funcional.

## Decisiones prácticas

### Cuándo usar `PlaceAutocompleteElement`

Usarlo cuando:

- el usuario necesita sugerencias visuales de dirección
- el input vive en una pantalla Angular normal
- se quiere seguir la versión moderna de Google Places

### Cuándo usar `Autocomplete`

Usarlo solo como fallback cuando:

- el widget moderno no está disponible en el runtime actual
- se necesita compatibilidad rápida sin rehacer toda la UI

### Cuándo dejar búsqueda manual

Mantenerla siempre como respaldo para:

- errores de Places
- entornos restringidos
- sesiones donde el widget moderno no cargó

## Diagnóstico rápido

### Error: `window.google.maps.Geocoder is not a constructor`

Checklist:

1. confirmar que el código usa `await google.maps.importLibrary('geocoding')`
2. confirmar que `new Geocoder()` ocurre después de ese `await`
3. confirmar que el boot espera `#loadGoogleMaps(...)` antes de `#initMap(...)`
4. confirmar que no hay un script viejo de Maps cargado sin `importLibrary`

### Warning: carga directa sin patrón óptimo

El warning de Google sobre carga directa sin el patrón recomendado suele ser de rendimiento
y no necesariamente la causa del bug. Corregirlo igual, pero no asumir que explica por sí
solo un mapa en blanco o un autocomplete roto.

### Mapa queda en “Cargando mapa…”

Revisar en este orden:

1. ¿llegó `Config OK`?
2. ¿llegó `Script OK`?
3. ¿llegó `Map initialized`?
4. ¿hay error de `geocoding` o `places` en consola?
5. ¿el host Angular existe realmente en el DOM cuando corre el boot?

## Errores comunes

- inicializar el mapa antes de que exista el host real en el DOM
- cargar el script sin `loading=async`
- crear `new window.google.maps.Geocoder()` sin `importLibrary('geocoding')`
- confiar solo en autocomplete sin fallback manual
- meter lógica de mapa directamente en páginas `web` y `mobile` duplicadas
- depender de `iframe` cuando el problema real es de integración Angular
- disparar el boot del componente antes de que `viewChild(...)` tenga el host real

## Referencias

Ver guía detallada en [references/google-maps-js-api-v3.65-weekly.md](./references/google-maps-js-api-v3.65-weekly.md).
