---
name: evista-bccr-sdde
description: >
  Conocimiento completo del API SDDE del BCCR (nuevo) y su integracion en eVista.
  Usar SIEMPRE que se trabaje en tipo de cambio, BCCR, SDDE, BccrService, BccrSettings,
  facturacion automatica GTI, FacturacionOrquestadorJob, ProcesarFacturaAutomaticaStub,
  o cuando se necesite consultar indicadores economicos del BCCR via el nuevo API APIM.
  Triggers: bccr, sdde, tipo de cambio, apim bccr, indicadores economicos, bccrservice,
  bccrservings, token bccr, bearer bccr, facturacion orquestador, crc usd, tipo cambio venta,
  indicador 318, indicador 317, ObtenerTipoCambioAsync.
---

# Skill: eVista BCCR SDDE — API nuevo

Este skill cubre el uso del API SDDE del Banco Central de Costa Rica (BCCR) y su implementación en eVista.

## Referencia oficial

- **Documentación PDF**: `.agents/skills/evista-bccr-sdde/references/api.pdf`
- **Endpoints resumidos**: `.agents/skills/evista-bccr-sdde/references/API-ENDPOINTS.md`
- **Script de prueba**: `.agents/skills/evista-bccr-sdde/examples/test-bccr-sdde.ps1`
- **Portal usuario/token**: https://sdd.bccr.fi.cr/es/IndicadoresEconomicos/Inicio/ → Iniciar sesión → Mi Perfil → Generar token

---

## Protocolo y autenticación

- **Protocolo**: HTTPS RESTful JSON, TLS 1.2 o superior.
- **Autenticación**: `Authorization: Bearer <token>` en el header de cada request.
- **Content-Type**: `application/json`
- El token se genera una sola vez en el portal SDD y se almacena en `appsettings.json` sección `Bccr`.

---

## URL base

```
https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API
```

---

## Endpoints disponibles

### 1. Series de indicador económico ← **EL MÁS USADO EN eVista**

```
GET /indicadoresEconomicos/{codigo}/series
    ?fechaInicio=yyyy%2Fmm%2Fdd
    &fechaFin=yyyy%2Fmm%2Fdd
    &idioma=ES
```

- `codigo`: código numérico del indicador (ej. `318` = tipo de cambio venta USD/CRC, `317` = compra).
- `fechaInicio` / `fechaFin`: formato `yyyy/mm/dd` URL-encoded como `yyyy%2Fmm%2Fdd`.
- **Response**:
```json
{
  "estado": true,
  "mensaje": "Consulta exitosa",
  "datos": [
    {
      "codigoIndicador": "318",
      "nombreIndicador": "Tipo de cambio venta",
      "series": [
        { "fecha": "2025-05-28", "valorDatoPorPeriodo": 519.55 }
      ]
    }
  ]
}
```
- El valor buscado es `datos[0].series[0].valorDatoPorPeriodo`.
- Si la fecha es fin de semana o feriado, BCCR no publica dato → `series` puede venir vacío o con `valorDatoPorPeriodo: null`. Manejar buscando hacia atrás hasta 3 días.

### 2. Metadata de indicador

```
GET /indicadoresEconomicos/{codigo}/metadata?idioma=ES
```
Retorna nombre, periodicidad, unidad de medida, primer y último dato.

### 3. Series de cuadro

```
GET /cuadro/{codigo}/series
    ?fechaInicio=yyyy%2Fmm%2Fdd
    &fechaFin=yyyy%2Fmm%2Fdd
    &idioma=ES
```
Retorna múltiples indicadores agrupados. La estructura de `datos` tiene campo `indicadores[]` con `series[]`.

### 4. Metadata de cuadro

```
GET /cuadro/{codigo}/metadata?idioma=ES
```

### 5. Descargar lista de indicadores (Excel)

```
GET /indicadoresEconomicos/descargar?idioma=ES
```
Respuesta binaria (xlsx). Útil para conocer los códigos de indicadores disponibles.

### 6. Validar suscripción

```
POST /Usuario/ValideSuscripcion?correo={correo}&token={token}
```
Response: `{ "estado": true, "mensaje": "Suscripción válida", "datos": [] }`

### 7. Series de carpeta de preferencias del usuario

```
GET /misPreferencias/carpeta/{codigo}/series
    ?fechaInicio=yyyy%2Fmm%2Fdd
    &fechaFin=yyyy%2Fmm%2Fdd
    &idioma=ES
```

---

## Indicadores relevantes para eVista

| Código | Nombre                      | Uso en eVista                              |
|--------|-----------------------------|--------------------------------------------|
| 318    | Tipo de cambio venta USD/CRC | `ObtenerTipoCambioAsync` — facturación USD |
| 317    | Tipo de cambio compra USD/CRC | Referencia (no usado actualmente)          |

---

## Manejo de errores

El API retorna `estado: false` o códigos HTTP 4xx/5xx:

```json
{ "CodigoError": "400", "Mensaje": "Parámetros de entrada inválidos." }
```

| Código HTTP | Significado |
|-------------|-------------|
| 200 | OK |
| 400 | Parámetros inválidos |
| 401 | Token inválido o expirado |
| 403 | Sin permiso |
| 404 | Recurso no encontrado |
| 429 | Demasiadas solicitudes |
| 500 | Error interno SDDE |

---

## Implementación en eVista.Hangfire

### Archivos involucrados

| Archivo | Descripción |
|---------|-------------|
| `eVista.Hangfire/Models/Bccr/BccrSettings.cs` | Configuración: BaseUrl, Token, Correo, IndicadorDolar |
| `eVista.Hangfire/Services/BccrService.cs` | Cliente HTTP que llama al API SDDE |
| `eVista.Hangfire/Extensions/BccrExtensions.cs` | Registro DI en `WebApplicationBuilder` |
| `eVista.Hangfire/Jobs/FacturacionOrquestadorJob.cs` | Llama `bccr.ObtenerTipoCambioAsync()` una vez por ciclo |
| `eVista.Hangfire/Jobs/ProcesarFacturaAutomaticaStub.cs` | Relay desde eVista.Api, llama `bccr.ObtenerTipoCambioAsync()` |

### BccrSettings actualizado para SDDE

```csharp
public sealed class BccrSettings
{
    public const string Section = "Bccr";
    public const string DefaultBaseUrl =
        "https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API";

    [Required] public required string Token { get; init; }
    [Required] public required string CorreoElectronico { get; init; }

    [Range(1, int.MaxValue)]
    public int IndicadorDolar { get; init; } = 318; // tipo de cambio venta USD/CRC
}
```

### BccrService actualizado para SDDE

```csharp
public sealed class BccrService(HttpClient http, IOptions<BccrSettings> opts)
{
    private readonly BccrSettings _settings = opts.Value;
    private static readonly TimeZoneInfo _tzCR =
        TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

    public async Task<decimal> ObtenerTipoCambioAsync(CancellationToken ct = default)
    {
        var hoyLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tzCR);

        // SDDE no publica en fines de semana/feriados → retroceder hasta 3 días
        for (int offset = 0; offset <= 3; offset++)
        {
            var fecha = hoyLocal.AddDays(-offset);
            var fechaStr = Uri.EscapeDataString(fecha.ToString("yyyy/MM/dd"));
            var url = $"/indicadoresEconomicos/{_settings.IndicadorDolar}/series" +
                      $"?fechaInicio={fechaStr}&fechaFin={fechaStr}&idioma=ES";

            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SddeResponse>(json);

            var valor = result?.Datos?
                .SelectMany(d => d.Series ?? [])
                .FirstOrDefault(s => s.ValorDatoPorPeriodo.HasValue)
                ?.ValorDatoPorPeriodo;

            if (valor.HasValue) return valor.Value;
        }

        throw new InvalidOperationException(
            $"[BCCR-SDDE] No se encontró tipo de cambio para indicador={_settings.IndicadorDolar} " +
            $"en los últimos 3 días hábiles.");
    }
}

// DTOs de deserialización
record SddeResponse(bool Estado, string Mensaje,
    [property: JsonPropertyName("datos")] List<SddeDato>? Datos);

record SddeDato(
    [property: JsonPropertyName("codigoIndicador")] string? CodigoIndicador,
    [property: JsonPropertyName("series")] List<SddeSerie>? Series);

record SddeSerie(
    [property: JsonPropertyName("fecha")] string? Fecha,
    [property: JsonPropertyName("valorDatoPorPeriodo")] decimal? ValorDatoPorPeriodo);
```

### BccrExtensions — registro DI

```csharp
builder.Services.AddHttpClient<BccrService>((_, client) =>
{
    client.BaseAddress = new Uri(BccrSettings.DefaultBaseUrl);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", settings.Token);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

Nota: el token debe leerse antes de registrar el HttpClient. Patrón recomendado: resolver `IOptions<BccrSettings>` tras `builder.Build()` o usar `IHttpClientFactory` con delegating handler.

### appsettings.json — sección Bccr

```json
"Bccr": {
  "Token": "<token-generado-en-mi-perfil-sdde>",
  "CorreoElectronico": "usuario@itqs.cr",
  "IndicadorDolar": 318
}
```

---

## Regla de negocio que nunca cambia

- `CurrencyCode == "CRC"` → `tipoCambio = 1.0m` (sin consultar BCCR).
- Cualquier otra moneda → `tipoCambio = await bccr.ObtenerTipoCambioAsync()`.

---

## Ejemplo curl directo

```bash
curl -X GET \
  "https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API/indicadoresEconomicos/318/series?fechaInicio=2025%2F05%2F28&fechaFin=2025%2F05%2F28&idioma=ES" \
  -H "Authorization: Bearer <tu-token>" \
  -H "Content-Type: application/json"
```

## Verificar suscripción activa (PowerShell)

```powershell
$token = "TU_TOKEN"
$correo = "usuario@itqs.cr"
$url = "https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API/Usuario/ValideSuscripcion?correo=$correo&token=$token"
Invoke-RestMethod -Method POST -Uri $url -ContentType "application/json"
```

