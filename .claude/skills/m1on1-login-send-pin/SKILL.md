---
name: m1on1-login-send-pin
description: >
  Flujo completo de login por PIN enviado por email en Marketing1on1: endpoint request-token,
  generacion y validacion de LoginToken, envio SMTP por Hangfire, template HTML del correo,
  cooldown y estados del login web/mobile, y limpieza diaria de tokens expirados o inactivos.
  Usar SIEMPRE que se trabaje con AuthController, jobs de auth, SMTP, login-web/login-mobile,
  request-token, PIN por email o limpieza de LoginToken.
applyTo:
  - "src/MarketingOneOnOneApi/Controllers/V1/AuthController.cs"
  - "src/MarketingOneOnOneApi/Jobs/Auth/**"
  - "src/MarketingOneOnOneApi/Services/SmtpEmailService.cs"
  - "src/MarketingOneOnOneApi/Services/Interfaces/ISmtpEmailService.cs"
  - "src/MarketingOneOnOneApi/DTOs/SmtpDtos.cs"
  - "src/MarketingOneOnOneApi/Templates/Email/login-pin.html"
  - "src/MarketingOneOnOneApi/Extensions/HangfireExtensions.cs"
  - "src/MarketingOneOnOneApi/Extensions/ApplicationServicesExtensions.cs"
  - "src/MarketingOneOnOneApi/appsettings.json"
  - "src/MarketingOneOnOneApi/appsettings.example.json"
  - "src/MarketingOneOnOneWeb/src/app/pages/login/**"
  - "src/MarketingOneOnOneWeb/src/app/service/auth.service.ts"
  - "src/MarketingOneOnOneWeb/src/app/shared/models/auth.models.ts"
---

# Login Send PIN - Marketing1on1

## 1. Que hace este flujo

Implementa un login en 2 pasos:

1. `POST /api/v1/auth/request-token`
2. `POST /api/v1/auth/login`

En el primer paso se genera un PIN de 5 digitos, se guarda en `tbLoginToken` y se encola un job
de Hangfire para enviarlo por email usando SMTP configurado por customer.

En el segundo paso se valida telefono + PIN, pero el PIN solo es aceptado si:

- pertenece al usuario correcto,
- sigue `IsActive = true`,
- y no ha expirado por tiempo.

Importante: la seguridad del PIN depende de la validacion de expiracion en login, NO del job de cleanup.

---

## 2. Flujo actual real

```text
Frontend login-web / login-mobile
    |
    |- requestPin -> AuthService.requestLoginToken()
    |                  |
    |                  v
    |        AuthController.RequestToken()
    |          |- resuelve tenant actual
    |          |- busca Login por telefono + customer
    |          |- desactiva tokens activos previos del usuario
    |          |- crea nuevo LoginToken activo
    |          \- enqueue ISendLoginPinEmailJob.SendAsync()
    |
    |- usuario recibe email con PIN
    |
    \- submit -> AuthService.loginWithToken()
                       |
                       v
              AuthController.Login()
                |- valida telefono + customer
                |- valida token activo
                |- valida token no expirado por CreateAt
                \- crea sesion + cookie + JWT opcional

Hangfire recurring jobs
    |
    \- auth-login-token-cleanup (diario)
         \- elimina LoginTokens inactivos o expirados
```

---

## 3. Archivos y responsabilidades

| Archivo | Responsabilidad |
|---|---|
| `Controllers/V1/AuthController.cs` | request-token, login, expiracion real del PIN, mascar email |
| `Jobs/Auth/SendLoginPinEmailJob.cs` | carga SMTP por customer, renderiza plantilla y envia email |
| `Jobs/Auth/CleanupLoginTokensJob.cs` | recurring job diario que elimina tokens expirados o inactivos |
| `Services/SmtpEmailService.cs` | wrapper SMTP con `System.Net.Mail.SmtpClient` |
| `DTOs/SmtpDtos.cs` | DTO `SmtpEmailMessage` |
| `Templates/Email/login-pin.html` | HTML del correo de PIN |
| `Extensions/HangfireExtensions.cs` | registro DI de jobs auth y recurring job diario |
| `Extensions/ApplicationServicesExtensions.cs` | registro DI de `ISmtpEmailService` |
| `appsettings*.json` | `Auth:LoginTokenExpireMinutes` y queues de Hangfire |
| `pages/login/login.ts` | coordinador de estado `sendingPin`, `pinSent`, `pinCooldown`, `pinEmailMask` |
| `pages/login/components/login-web/*` | flujo desktop con boton enviar/reenviar PIN |
| `pages/login/components/login-mobile/*` | flujo mobile con boton enviar/reenviar PIN |
| `assets/i18n/*.json` | textos del flujo `SEND_PIN`, `SENDING_PIN`, etc. |

---

## 4. Reglas backend obligatorias

### 4.1 RequestToken

`AuthController.RequestToken()` debe mantener este comportamiento:

1. Resolver `idClientCustomer` desde `IDomainSettingsService`.
2. Buscar el usuario por telefono normalizado y pertenencia a `tbClientCustomerLogin`.
3. Si el usuario no tiene email, responder error util.
4. Desactivar tokens activos previos del mismo `IdLogin`.
5. Crear un nuevo `LoginToken` con:
   - `Token` de 5 digitos
   - `CreateAt = DateTime.UtcNow`
   - `IsActive = true`
6. Guardar en BD antes de encolar el job.
7. Encolar `ISendLoginPinEmailJob.SendAsync(idLogin, idClientCustomer, pin)`.
8. Retornar `EmailMasked` y `ExpiresInMinutes`.

### 4.2 Login

`AuthController.Login()` debe validar el PIN con estas 3 condiciones a la vez:

- `IdLogin` correcto
- `Token` correcto
- `IsActive == true`
- `CreateAt >= UtcNow - LoginTokenExpireMinutes`

La expiracion se toma de:

```json
"Auth": {
  "LoginTokenExpireMinutes": 5
}
```

No depender del cleanup diario para invalidar un PIN.

### 4.3 Cleanup diario

`CleanupLoginTokensJob` YA NO corre al final del envio del email.

Ahora corre como recurring job diario y elimina tokens que cumplan cualquiera de estas reglas:

- `!IsActive`
- `CreateAt < UtcNow - LoginTokenExpireMinutes`

Esto permite mantener la tabla limpia sin invalidar el PIN recien emitido.

### 4.4 Queue de Hangfire

Los jobs de auth usan la queue `auth`.

Verificar siempre:

- `Queues` incluye `auth` en `appsettings.json`
- `HangfireExtensions.AddHangfireConfiguration()` tambien contempla `auth` como fallback

---

## 5. Reglas SMTP y template

### SMTP por customer

El job `SendLoginPinEmailJob` debe cargar `ClientCustomerEmailSmtp` por `idClientCustomer` desde
`tbClientCustomerEmailSMTP`.

No resolver SMTP dentro del controller ni hardcodear credenciales.

### Template HTML

La plantilla del correo vive en:

`Templates/Email/login-pin.html`

Placeholders obligatorios:

- `{{PIN}}`
- `{{USER_NAME}}`
- `{{COMPANY_NAME}}`

El path debe construirse con:

```csharp
Path.Combine(AppContext.BaseDirectory, "Templates", "Email", "login-pin.html")
```

Y el `csproj` debe copiar `Templates/**/*` a output y publish.

---

## 6. Reglas frontend obligatorias

El frontend NO debe permitir login directo sin haber solicitado el PIN primero.

### Estado coordinador (`login.ts`)

Signals obligatorios:

- `sendingPin`
- `pinSent`
- `pinCooldown`
- `pinEmailMask`
- `pinErrorMessage`

### Comportamiento UI

1. El boton `Enviar PIN al email` llama `requestLoginToken()`.
2. Si responde ok:
   - `pinSent = true`
   - mostrar `pinEmailMask`
   - iniciar countdown de 60s
3. Mientras `pinSent` sea `false`:
   - input de token deshabilitado
   - boton de login deshabilitado
4. Si hay error del request-token:
   - mostrar `pinErrorMessage`
   - no habilitar login

### Componentes afectados

- `login-web.component.*`
- `login-mobile.component.*`
- `login.html`

---

## 7. Decisiones de diseño que NO se deben romper

### 7.1 No borrar todos los tokens al enviar el email

Esto rompe el login porque el PIN enviado deja de existir o deja de ser valido antes de usarse.

Antipatron:

```csharp
// NO HACER
_backgroundJobClient.Enqueue<ICleanupLoginTokensJob>(job => job.CleanupExpiredTokensAsync());
```

### 7.2 No depender del cleanup para seguridad

El PIN se invalida por expiracion en `AuthController.Login()`, no esperando al recurring job nocturno.

### 7.3 No guardar multiples tokens activos del mismo usuario

Antes de crear uno nuevo, desactivar los activos previos del mismo `IdLogin`.

### 7.4 No mover la logica SMTP al frontend ni al controller

La carga de config SMTP y el envio viven en `SendLoginPinEmailJob` + `SmtpEmailService`.

---

## 8. Checklist rapido cuando se toque este flujo

Si cambias `request-token`, revisar siempre:

1. que siga devolviendo `emailMasked`
2. que siga encolando el job SMTP
3. que desactive tokens activos previos
4. que no introduzca cleanup inmediato

Si cambias `login`, revisar siempre:

1. que siga filtrando por `IsActive`
2. que siga validando expiracion temporal
3. que `LoginTokenExpireMinutes` venga de configuracion

Si cambias Hangfire, revisar siempre:

1. que exista el recurring job `auth-login-token-cleanup`
2. que la queue `auth` siga activa

Si cambias frontend login, revisar siempre:

1. que `requestPin` siga separado de `submit`
2. que el token no se pueda escribir/usar antes de `pinSent`
3. que web y mobile mantengan el mismo comportamiento

---

## 9. Comandos de validacion recomendados

Backend:

```bash
dotnet build src/MarketingOneOnOneApi/MarketingOneOnOneApi.csproj
```

Frontend:

```bash
cd src/MarketingOneOnOneWeb
npm run build:dev
```

Pruebas manuales minimas:

1. solicitar PIN con usuario valido y email configurado
2. verificar que llega correo
3. login con PIN dentro de vigencia -> OK
4. login con PIN vencido -> reject
5. reenviar PIN -> el PIN anterior queda inactivo

---

## 10. Leccion aprendida principal

El plan inicial hablaba de cleanup al terminar el envio del email. Eso parecia limpio, pero en la
practica invalidaba el PIN antes de que el usuario pudiera usarlo.

La implementacion correcta en este repo es:

- expiracion real en login,
- cleanup diario por Hangfire,
- y un unico token activo por usuario.