---
name: m1on1-web2-browser-test
description: >
  Verifica cambios de frontend en MarketingOneOnOneWeb2 con un navegador real
  (Playwright headless) en vez de solo `ng build`/lectura de código — levanta
  backend + `ng serve`, hace login real por PIN (leyendo el token directo de
  `tbLoginToken` porque no hay acceso a una bandeja de email real), navega,
  hace click en botones reales y confirma con capturas de pantalla. Usar cuando
  se pida "probar en el navegador", "verificar end-to-end", validar que un botón
  o flujo de Web2 realmente funciona, o depurar un bug que "no se ve" en el código
  pero el usuario reporta en la UI (ej. un diálogo que no aparece).
  Triggers: probar en el navegador, verificar end-to-end, e2e Web2, playwright,
  login por PIN automatizado, testear frontend, screenshot de la app, click real.
---

# m1on1-web2-browser-test

Skill genérico para verificar cualquier flujo de `MarketingOneOnOneWeb2` con un
navegador real contra el backend real (BD remota `172.191.128.24`), no contra mocks.
Nació en TASK-EBC-FE-56: el usuario reportó que "los botones de Fork no producen
ningún efecto visible" — un bug que solo se detecta ejecutando la app de verdad,
porque `window.prompt()` compila y pasa `tsc`/`ng build` sin ningún error, pero no
tiene ningún efecto visible en el entorno real donde corre la app (por eso el resto
del proyecto ya usa SweetAlert2 en vez de `window.confirm`/`alert`/`prompt`).

## Cuándo usarlo

- Verificar que un botón/formulario/flujo nuevo o corregido funciona de verdad, no
  solo que compila.
- Reproducir un bug que el usuario ve en la UI pero no es obvio leyendo el código.
- Confirmar visualmente un cambio de diseño/color/layout.

## Cuándo NO usarlo

- Cambios que no tocan runtime (tests, docs, tipos sin efecto en UI) — no hay nada
  que un navegador pueda confirmar que el build ya no confirme.
- Si ya existe un servidor corriendo del usuario y alcanza con probarlo tú mismo
  pidiéndole que confirme — no dupliques esfuerzo si el usuario puede probarlo en 30s.

---

## Procedimiento

### 1. Verificar qué ya está corriendo (no asumas puertos libres)

Antes de levantar nada, revisa si el usuario ya tiene sus propios `dotnet run`/`ng serve`
corriendo — es común en este proyecto y **no se deben matar** sin preguntar:

```bash
lsof -i :8000 -sTCP:LISTEN   # backend, puerto que espera proxy.conf.json
lsof -i :4200 -sTCP:LISTEN   # frontend, puerto default de ng serve
curl -s http://localhost:8000/health   # si responde, ya hay backend real arriba — reusarlo
```

Si el puerto 8000 ya está en uso por un proceso que **no es tuyo**, no lo mates: es
probablemente el backend del usuario. Reutilízalo. Si necesitas tu propio frontend,
usa `ng serve --port <otro-puerto>` (ver `proxy.conf.json` — el proxy siempre apunta
a `localhost:8000`, así que solo cambia el puerto del frontend, nunca el del backend).

Si necesitas tu propio backend porque no hay ninguno corriendo:

```bash
cd src/MarketingOneOnOneApi && dotnet run --urls http://localhost:8000 &
cd src/MarketingOneOnOneWeb2 && npx ng serve --port 4201 &
```

`ng serve` sirve por defecto en **https** con certificado autofirmado — Playwright
necesita `ignoreHTTPSErrors: true` (ya está en la plantilla).

### 2. Preparar Playwright (una vez por sesión)

El repo no trae Playwright como dependencia de `MarketingOneOnOneWeb2` — se instala
aislado en el scratchpad, nunca en `package.json` del proyecto:

```bash
.agents/skills/m1on1-web2-browser-test/examples/setup-playwright.sh /ruta/scratchpad/pw
```

Los browsers de Playwright suelen estar ya cacheados de instalaciones previas
(`~/Library/Caches/ms-playwright` en macOS) — el `install chromium` es rápido si
ya existen.

### 3. Login por PIN sin acceso a email

El login de Marketing1on1 no tiene contraseña — es PIN de 5 dígitos enviado por
email (ver skill `m1on1-login-send-pin`). Sin acceso a una bandeja real, el PIN se
lee directo de `tbLoginToken` (BD remota) **después** de disparar
`POST /api/v1/auth/request-token` desde el navegador:

```
pwsh .agents/skills/m1on1-web2-browser-test/examples/get-pin.ps1 -Email usuario@dominio.com
```

Usa la conexión de `.local-secrets/db.json` (mismo patrón que MCP `dbQuery` —
nunca hardcodear credenciales). El script de Playwright ya hace esto automáticamente:
ver [examples/login-and-test.mjs](./examples/login-and-test.mjs).

### 4. Escribir el test

Copia [examples/login-and-test.mjs](./examples/login-and-test.mjs) y reemplaza el
bloque `TU TEST AQUÍ` con los pasos específicos. El bloque de login no cambia salvo
que cambie el flujo real de auth.

```bash
BASE_URL=https://localhost:4201 EMAIL=usuario@dominio.com node /ruta/scratchpad/pw/mi-test.mjs
```

Patrones importantes ya documentados dentro de la plantilla:
- Diálogos son SweetAlert2 (`.swal2-popup`/`.swal2-confirm`), **nunca**
  `window.confirm`/`alert`/`prompt` — si un flujo dispara un diálogo nativo del
  navegador en vez de `.swal2-popup`, es casi siempre el mismo bug que TASK-EBC-FE-56
  (sin efecto visible en el entorno real de la app), no algo que el test deba tolerar.
- Para pasos que disparan una llamada async lenta de verdad (ej. generación de imagen
  vía Azure AI Foundry, 30-100s reales), usa `page.waitForResponse()` en vez de un
  `waitForTimeout` fijo — un timeout corto reporta falso negativo, uno largo desperdicia
  tiempo en los casos rápidos.
- Loguea `pageerror` y `console` tipo `error` siempre — un screenshot que "se ve bien"
  puede estar ocultando un `HttpErrorResponse` ya manejado con un toast silencioso.

### 5. Limpieza de datos de prueba

Cualquier acción real contra el backend real (crear, forkear, generar) escribe en la
**BD compartida real**, no en un sandbox. Antes de mutar/borrar datos de prueba:

- **Nunca** ejecutes un `UPDATE`/`DELETE` de limpieza sin que el usuario lo apruebe
  explícitamente — el clasificador de auto-mode de Claude Code bloqueará intentos de
  mutar recursos compartidos detectados por patrón en vez de por ID trackeado, y con
  razón: es una BD real de la que dependen otras sesiones/usuarios.
- Prefiere borrar vía la propia UI/API de la app (mismo soft-delete que usaría un
  usuario real) en vez de SQL directo, cuando sea posible.
- Si usas SQL directo para limpiar, hazlo por ID explícito de lo que tú mismo creaste
  en esta sesión (no por `LIKE 'Copia de%'` u otro patrón que pueda alcanzar datos
  reales de otra persona), y pide confirmación primero.

### 6. Apagar lo que levantaste tú

Mata solo los procesos que tú arrancaste (verifica el PID antes). No mates el proceso
que ya estaba corriendo del usuario en 8000/4200 — ver paso 1.

---

## Archivos

| Archivo | Uso |
|---|---|
| [examples/setup-playwright.sh](./examples/setup-playwright.sh) | Instala Playwright+Chromium en un directorio scratchpad aislado |
| [examples/get-pin.ps1](./examples/get-pin.ps1) | Lee el PIN activo más reciente de `tbLoginToken` por `-Email` o `-IdLogin` |
| [examples/login-and-test.mjs](./examples/login-and-test.mjs) | Plantilla de test: login real + bloque para pegar los pasos específicos |

Ver también: `m1on1-login-send-pin` (contrato del flujo de auth), MCP `dbQuery`
(patrón de conexión a BD usado por `get-pin.ps1`), `playwright-design-review`
(revisión visual de diseño — distinto objetivo, ese skill es para juzgar estética,
este es para confirmar que un flujo funciona).
