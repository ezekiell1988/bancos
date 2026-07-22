# Reference: Form Elements

Fuente: `color-admin/template_angularjs20/src/app/pages/form/form-elements/form-elements.html`

---

## Controles Base

### Input / Textarea / Select

```html
<!-- Input horizontal (patrón más usado — col-md-3 / col-md-9) -->
<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Email address</label>
  <div class="col-md-9">
    <input type="email" class="form-control mb-5px" placeholder="Enter email" />
    <small class="fs-12px text-gray-500-darker">We'll never share your email with anyone else.</small>
  </div>
</div>

<!-- Select simple -->
<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Example select</label>
  <div class="col-md-9">
    <select class="form-select">
      <option>1</option>
      <option>2</option>
    </select>
  </div>
</div>

<!-- Select multiple -->
<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Example multiple select</label>
  <div class="col-md-9">
    <select multiple class="form-select">
      <option>1</option>
      <option>2</option>
    </select>
  </div>
</div>

<!-- Textarea -->
<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Example textarea</label>
  <div class="col-md-9">
    <textarea class="form-control" rows="3"></textarea>
  </div>
</div>
```

### Readonly y Plaintext

```html
<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Readonly</label>
  <div class="col-sm-9">
    <input class="form-control" type="text" placeholder="Readonly input here…" readonly />
  </div>
</div>

<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Readonly Plaintext</label>
  <div class="col-sm-9">
    <input type="text" readonly class="form-control-plaintext" value="email@example.com" />
  </div>
</div>

<div class="row mb-15px">
  <label class="form-label col-form-label col-md-3">Password</label>
  <div class="col-sm-9">
    <input type="password" class="form-control" placeholder="Password" />
  </div>
</div>
```

### Range

```html
<div class="d-flex align-items-center py-2">
  <div class="w-60px text-end pe-2">Min: <b>0</b></div>
  <div class="d-flex flex-1">
    <input type="range" class="form-range" min="0" max="5" id="customRange" />
  </div>
  <div class="w-60px text-start ps-2">Max: <b>50</b></div>
</div>
```

### Floating Label

```html
<div class="row">
  <div class="col-md-6">
    <div class="form-floating mb-3 mb-md-0">
      <input type="email" class="form-control fs-15px" id="floatingInput" placeholder="name@example.com" />
      <label for="floatingInput" class="d-flex align-items-center fs-13px">Email address</label>
    </div>
  </div>
  <div class="col-md-6">
    <form class="form-floating">
      <input type="email" class="form-control fs-15px" id="floatingInputValue" placeholder="name@example.com" value="test@example.com" />
      <label for="floatingInputValue" class="d-flex align-items-center fs-13px">Input with value</label>
    </form>
  </div>
</div>
```

---

## Tamaños

```html
<div class="row">
  <div class="col-md-6">
    <!-- Inputs -->
    <div class="mb-10px">
      <input class="form-control form-control-lg" type="text" placeholder=".form-control-lg" />
    </div>
    <div class="mb-10px">
      <input class="form-control" type="text" placeholder="default input" />
    </div>
    <div class="mb-3">
      <input class="form-control form-control-sm" type="text" placeholder=".form-control-sm" />
    </div>
  </div>
  <div class="col-md-6">
    <!-- Selects -->
    <div class="mb-10px">
      <select class="form-select form-select-lg"><option>.form-select-lg</option></select>
    </div>
    <div class="mb-10px">
      <select class="form-select"><option>default select</option></select>
    </div>
    <div>
      <select class="form-select form-select-sm"><option>.form-select-sm</option></select>
    </div>
  </div>
</div>
```

---

## Validación Visual

```html
<!-- Input válido -->
<div class="row mb-10px">
  <label class="form-label col-form-label col-md-3">Valid Input</label>
  <div class="col-md-9">
    <div class="input-group">
      <div class="input-group-text">@</div>
      <input type="text" class="form-control is-valid" />
      <div class="valid-feedback">Looks good!</div>
    </div>
  </div>
</div>

<!-- Input inválido -->
<div class="row mb-10px">
  <label class="form-label col-form-label col-md-3">Invalid Input</label>
  <div class="col-md-9">
    <div class="input-group">
      <div class="input-group-text">@</div>
      <input type="text" class="form-control is-invalid" />
      <div class="invalid-feedback">Please choose a unique and valid username.</div>
    </div>
  </div>
</div>

<!-- Tooltip messages (valid + invalid) -->
<div class="row form-group">
  <label class="form-label col-form-label col-md-3">Tooltip Message</label>
  <div class="col-md-9">
    <div class="row">
      <div class="col-md-6">
        <div class="input-group">
          <div class="input-group-text">@</div>
          <input type="text" class="form-control is-invalid" />
          <div class="invalid-tooltip">Please choose a unique and valid username.</div>
        </div>
      </div>
      <div class="col-md-6">
        <div class="input-group">
          <div class="input-group-text">@</div>
          <input type="text" class="form-control is-valid" />
          <div class="valid-tooltip">Looks good!</div>
        </div>
      </div>
    </div>
  </div>
</div>
```

### Validación en Angular (binding reactivo)

```html
<input type="text"
       class="form-control"
       [class.is-valid]="campo.valid && campo.touched"
       [class.is-invalid]="campo.invalid && campo.touched"
       [formControl]="campo" />
<div class="invalid-feedback">Campo requerido.</div>
<div class="valid-feedback">¡Se ve bien!</div>
```

---

## Checkboxes

```html
<!-- Vertical -->
<div class="form-check mt-2 mb-2">
  <input class="form-check-input" type="checkbox" value="" id="flexCheckDefault" />
  <label class="form-check-label" for="flexCheckDefault">Default checkbox</label>
</div>
<div class="form-check mb-2">
  <input class="form-check-input" type="checkbox" value="" id="flexCheckChecked" checked />
  <label class="form-check-label" for="flexCheckChecked">Checked checkbox</label>
</div>
<div class="form-check mb-2">
  <input class="form-check-input" type="checkbox" value="" id="flexCheckDisabled" disabled />
  <label class="form-check-label" for="flexCheckDisabled">Disabled checkbox</label>
</div>

<!-- Válido / Inválido -->
<div class="form-check mb-2">
  <input class="form-check-input is-valid" type="checkbox" value="" id="validCheckbox" />
  <label class="form-check-label" for="validCheckbox">Valid Checkbox</label>
</div>
<div class="form-check">
  <input class="form-check-input is-invalid" type="checkbox" value="" id="invalidCheckbox" />
  <label class="form-check-label" for="invalidCheckbox">Invalid Checkbox</label>
</div>

<!-- Inline -->
<div class="form-check form-check-inline">
  <input class="form-check-input" type="checkbox" id="inlineCheckbox1" value="option1" />
  <label class="form-check-label" for="inlineCheckbox1">1</label>
</div>
<div class="form-check form-check-inline">
  <input class="form-check-input" type="checkbox" id="inlineCheckbox2" value="option2" />
  <label class="form-check-label" for="inlineCheckbox2">2</label>
</div>
```

---

## Switches

```html
<div class="form-check form-switch mb-2">
  <input class="form-check-input" type="checkbox" id="flexSwitchCheckDefault" />
  <label class="form-check-label" for="flexSwitchCheckDefault">Default switch checkbox input</label>
</div>
<div class="form-check form-switch mb-2">
  <input class="form-check-input" type="checkbox" id="flexSwitchCheckChecked" checked />
  <label class="form-check-label" for="flexSwitchCheckChecked">Checked switch checkbox input</label>
</div>
<div class="form-check form-switch mb-2">
  <input class="form-check-input" type="checkbox" id="flexSwitchCheckDisabled" disabled />
  <label class="form-check-label" for="flexSwitchCheckDisabled">Disabled switch checkbox input</label>
</div>
<div class="form-check form-switch">
  <input class="form-check-input" type="checkbox" id="flexSwitchCheckCheckedDisabled" checked disabled />
  <label class="form-check-label" for="flexSwitchCheckCheckedDisabled">Disabled checked switch checkbox input</label>
</div>
```

---

## Radios

```html
<!-- Vertical -->
<div class="form-check mb-2">
  <input class="form-check-input" type="radio" name="flexRadioDefault" id="flexRadioDefault1" />
  <label class="form-check-label" for="flexRadioDefault1">Default radio</label>
</div>
<div class="form-check mb-2">
  <input class="form-check-input" type="radio" name="flexRadioDefault" id="flexRadioDefault2" checked />
  <label class="form-check-label" for="flexRadioDefault2">Default checked radio</label>
</div>
<div class="form-check mb-2">
  <input class="form-check-input" type="radio" name="flexRadioDisabled" id="flexRadioDisabled" disabled />
  <label class="form-check-label" for="flexRadioDisabled">Disabled radio</label>
</div>

<!-- Válido / Inválido -->
<div class="form-check mb-2">
  <input class="form-check-input is-valid" type="radio" name="default_radio" id="validDefaultRadio" />
  <label class="form-check-label" for="validDefaultRadio">Success State</label>
</div>
<div class="form-check">
  <input class="form-check-input is-invalid" type="radio" name="default_radio" id="invalidDefaultRadio" />
  <label class="form-check-label" for="invalidDefaultRadio">Error State</label>
</div>

<!-- Inline -->
<div class="form-check form-check-inline">
  <input class="form-check-input" type="radio" id="inlineRadio1" name="inlineRadio" />
  <label class="form-check-label" for="inlineRadio1">1</label>
</div>
<div class="form-check form-check-inline">
  <input class="form-check-input" type="radio" id="inlineRadio2" name="inlineRadio" />
  <label class="form-check-label" for="inlineRadio2">2</label>
</div>
```

---

## Input Group

```html
<!-- Prefijo texto -->
<div class="input-group mb-3">
  <div class="input-group-text">@</div>
  <input type="text" class="form-control" placeholder="Username" />
</div>

<!-- Sufijo icono -->
<div class="input-group mb-3">
  <input type="text" class="form-control" />
  <div class="input-group-text"><i class="fa fa-calendar"></i></div>
</div>

<!-- Prefijo + Sufijo texto -->
<div class="input-group mb-3">
  <div class="input-group-text">$</div>
  <input type="text" class="form-control" />
  <div class="input-group-text">.00</div>
</div>

<!-- Con checkbox -->
<div class="input-group mb-10px">
  <div class="input-group-text">
    <input type="checkbox" class="form-check-input" />
  </div>
  <input type="text" class="form-control" placeholder="Checkbox add on" />
</div>

<!-- Con radio -->
<div class="input-group mb-10px">
  <div class="input-group-text">
    <input type="radio" class="form-check-input" />
  </div>
  <input type="text" class="form-control" placeholder="Radio button add on" />
</div>

<!-- Con botón y dropdown -->
<div class="input-group">
  <button type="button" class="btn btn-primary">Action</button>
  <button type="button" class="btn btn-primary" data-bs-toggle="dropdown">
    <span class="caret"></span>
  </button>
  <div class="dropdown-menu">
    <a href="#" class="dropdown-item">Action</a>
    <a href="#" class="dropdown-item">Another action</a>
    <div class="dropdown-divider"></div>
    <a href="#" class="dropdown-item">Separated link</a>
  </div>
  <input type="text" class="form-control" />
  <button type="button" class="btn btn-indigo dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown">
    <span class="caret"></span>
  </button>
  <div class="dropdown-menu dropdown-menu-end">
    <a href="#" class="dropdown-item">Action</a>
    <a href="#" class="dropdown-item">Another action</a>
    <div class="dropdown-divider"></div>
    <a href="#" class="dropdown-item">Separated link</a>
  </div>
  <button type="button" class="btn btn-indigo">Action</button>
</div>
```

### Tamaños del Input Group

```html
<div class="input-group input-group-lg mb-10px">
  <div class="input-group-text">@</div>
  <input type="text" class="form-control" placeholder="Username" />
</div>
<div class="input-group mb-10px">
  <div class="input-group-text">@</div>
  <input type="text" class="form-control" placeholder="Username" />
</div>
<div class="input-group input-group-sm">
  <div class="input-group-text">@</div>
  <input type="text" class="form-control" placeholder="Username" />
</div>
```

---

## Layouts de Formulario

### Vertical (Default Style)

```html
<form action="/" method="POST">
  <fieldset>
    <legend class="mb-3">Legend</legend>
    <div class="mb-3">
      <label class="form-label" for="exampleInputEmail1">Email address</label>
      <input class="form-control" type="email" id="exampleInputEmail1" placeholder="Enter email" />
    </div>
    <div class="mb-3">
      <label class="form-label" for="exampleInputPassword1">Password</label>
      <input class="form-control" type="password" id="exampleInputPassword1" placeholder="Password" />
    </div>
    <div class="form-check mb-3">
      <input class="form-check-input" type="checkbox" id="nf_checkbox_css_1" />
      <label class="form-check-label" for="nf_checkbox_css_1">Check me out</label>
    </div>
    <button type="submit" class="btn btn-primary w-100px me-5px">Login</button>
    <button type="submit" class="btn btn-default w-100px">Cancel</button>
  </fieldset>
</form>
```

### Horizontal

```html
<form action="/" method="POST">
  <fieldset>
    <legend class="mb-3">Legend</legend>
    <div class="row mb-3">
      <label class="form-label col-form-label col-md-3">Email address</label>
      <div class="col-md-9">
        <input type="email" class="form-control" placeholder="Enter email" />
      </div>
    </div>
    <div class="row mb-3">
      <label class="form-label col-form-label col-md-3">Password</label>
      <div class="col-md-9">
        <input type="password" class="form-control" placeholder="Password" />
      </div>
    </div>
    <div class="row mb-3">
      <div class="col-md-9 offset-md-3">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="fh_checkbox_css_1" />
          <label class="form-check-label" for="fh_checkbox_css_1">Check me out</label>
        </div>
      </div>
    </div>
    <div class="row">
      <div class="col-md-7 offset-md-3">
        <button type="submit" class="btn btn-primary w-100px me-5px">Login</button>
        <button type="submit" class="btn btn-default w-100px">Cancel</button>
      </div>
    </div>
  </fieldset>
</form>
```

### Inline

```html
<form class="row row-cols-lg-auto g-3 align-items-center" action="/" method="POST">
  <div class="col-12">
    <input type="email" class="form-control" id="exampleInputEmail2" placeholder="Enter email" />
  </div>
  <div class="col-12">
    <input type="password" class="form-control" id="exampleInputPassword2" placeholder="Password" />
  </div>
  <div class="col-12">
    <div class="form-check">
      <input class="form-check-input" id="inline_form_checkbox" type="checkbox" />
      <label class="form-check-label" for="inline_form_checkbox">Remember me</label>
    </div>
  </div>
  <button type="submit" class="btn btn-primary w-100px me-5px">Sign in</button>
  <button type="submit" class="btn btn-default w-100px">Register</button>
</form>
```

---

## Tabla de Clases Más Usadas

| Clase                  | Uso                                              |
|------------------------|--------------------------------------------------|
| `form-control`         | Input, textarea                                  |
| `form-select`          | Select / select multiple                         |
| `form-control-plaintext` | Input readonly sin borde                      |
| `form-range`           | Input tipo range                                 |
| `form-label`           | Label estándar                                   |
| `col-form-label`       | Label en layout horizontal (alineación vertical) |
| `form-floating`        | Wrapper para labels flotantes                    |
| `form-check`           | Wrapper para checkbox / radio                    |
| `form-check-input`     | El input dentro de form-check                    |
| `form-check-label`     | El label dentro de form-check                    |
| `form-check-inline`    | Checkbox / radio en línea                        |
| `form-switch`          | Modificador para convertir checkbox a switch     |
| `form-control-lg/sm`   | Tamaño grande / pequeño para input               |
| `form-select-lg/sm`    | Tamaño grande / pequeño para select              |
| `is-valid`             | Estado válido visual en el control               |
| `is-invalid`           | Estado inválido visual en el control             |
| `valid-feedback`       | Mensaje de validación exitosa                    |
| `invalid-feedback`     | Mensaje de error de validación                   |
| `valid-tooltip`        | Tooltip de validación exitosa                    |
| `invalid-tooltip`      | Tooltip de error de validación                   |
| `input-group`          | Wrapper para Input Group                         |
| `input-group-text`     | Prefijo/sufijo dentro del Input Group            |
| `input-group-lg/sm`    | Tamaño del Input Group                           |
| `mb-15px`              | Separación estándar entre filas horizontales     |
| `mb-10px`              | Separación compacta entre elementos              |
| `mb-5px`               | Separación mínima                                |
| `me-5px`               | Separación horizontal entre botones             |
| `w-100px`              | Ancho fijo 100px (botones estándar)              |
| `fs-12px`              | Font-size 12px (texto de ayuda bajo el campo)    |
| `fs-13px`              | Font-size 13px (labels floating)                 |
| `fs-15px`              | Font-size 15px (input dentro de floating)        |
| `w-60px`               | Ancho 60px (etiquetas del range)                 |
