# Reference: Language Icon (Flag Icons)

## Uso básico

```html
<!-- Bandera de país por código ISO 2 letras -->
<div class="fi fi-{codigo}" title="{nombre}" id="{codigo}"></div>
```

## Con tamaño y estilo

```html
<!-- h1 tamaño, esquinas redondeadas -->
<div class="fi fi-cr h1 rounded mb-0" title="cr" id="cr"></div>

<!-- En fila con texto del país -->
<div class="d-flex align-items-center">
  <div class="fi fi-us h1 rounded mb-0" title="us" id="us"></div>
  <div class="ps-2 fw-bold">US</div>
</div>
```

## Patrón de grilla para mostrar banderas

```html
<div class="row">
  <div class="col-lg col-md-3 col-sm-4 col-6">
    <div class="mb-3 d-flex align-items-center">
      <div class="fi fi-us h1 rounded mb-0" title="us" id="us"></div>
      <div class="ps-2 fw-bold">US</div>
    </div>
    <div class="mb-3 d-flex align-items-center">
      <div class="fi fi-mx h1 rounded mb-0" title="mx" id="mx"></div>
      <div class="ps-2 fw-bold">MX</div>
    </div>
    <!-- ... -->
  </div>
</div>
```

## Códigos de países Latinoamérica y EEUU

| País                  | Código | Clase          |
|-----------------------|--------|----------------|
| Argentina             | ar     | `fi fi-ar`     |
| Bolivia               | bo     | `fi fi-bo`     |
| Brasil                | br     | `fi fi-br`     |
| Chile                 | cl     | `fi fi-cl`     |
| Colombia              | co     | `fi fi-co`     |
| Costa Rica            | cr     | `fi fi-cr`     |
| Cuba                  | cu     | `fi fi-cu`     |
| Ecuador               | ec     | `fi fi-ec`     |
| El Salvador           | sv     | `fi fi-sv`     |
| Guatemala             | gt     | `fi fi-gt`     |
| Honduras              | hn     | `fi fi-hn`     |
| México                | mx     | `fi fi-mx`     |
| Nicaragua             | ni     | `fi fi-ni`     |
| Panamá                | pa     | `fi fi-pa`     |
| Paraguay              | py     | `fi fi-py`     |
| Perú                  | pe     | `fi fi-pe`     |
| Puerto Rico           | pr     | `fi fi-pr`     |
| República Dominicana  | do     | `fi fi-do`     |
| Uruguay               | uy     | `fi fi-uy`     |
| Venezuela             | ve     | `fi fi-ve`     |
| Estados Unidos        | us     | `fi fi-us`     |
| Canadá                | ca     | `fi fi-ca`     |
| España                | es     | `fi fi-es`     |

## Otros países frecuentes

| País           | Código | Clase          |
|----------------|--------|----------------|
| Alemania       | de     | `fi fi-de`     |
| Australia      | au     | `fi fi-au`     |
| China          | cn     | `fi fi-cn`     |
| Francia        | fr     | `fi fi-fr`     |
| India          | in     | `fi fi-in`     |
| Italia         | it     | `fi fi-it`     |
| Japón          | jp     | `fi fi-jp`     |
| Portugal       | pt     | `fi fi-pt`     |
| Reino Unido    | gb     | `fi fi-gb`     |
| Rusia          | ru     | `fi fi-ru`     |

## Tamaños con clases Bootstrap/Color Admin

```html
<div class="fi fi-us fs-12px rounded"></div>   <!-- 12px -->
<div class="fi fi-us h6 rounded"></div>        <!-- h6 size -->
<div class="fi fi-us h4 rounded"></div>        <!-- h4 size -->
<div class="fi fi-us h2 rounded"></div>        <!-- h2 size -->
<div class="fi fi-us h1 rounded"></div>        <!-- h1 size (el más grande) -->
```

## Selector de idioma usando banderas

```html
<div class="dropdown">
  <button class="btn btn-default dropdown-toggle" data-bs-toggle="dropdown">
    <span class="d-flex align-items-center gap-2">
      <div class="fi fi-cr rounded"></div>
      <span>Costa Rica</span>
    </span>
  </button>
  <div class="dropdown-menu">
    <a class="dropdown-item d-flex align-items-center gap-2" href="#">
      <div class="fi fi-cr rounded"></div> Costa Rica
    </a>
    <a class="dropdown-item d-flex align-items-center gap-2" href="#">
      <div class="fi fi-us rounded"></div> United States
    </a>
    <a class="dropdown-item d-flex align-items-center gap-2" href="#">
      <div class="fi fi-mx rounded"></div> México
    </a>
  </div>
</div>
```
