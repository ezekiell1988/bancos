# BCCR SDDE — Referencia de Endpoints

**Fuente oficial**: `references/api.pdf` (copia local del estándar BCCR 2025)  
**Script de prueba**: `examples/test-bccr-sdde.ps1`  **URL base:** `https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API`  
**Autenticación:** `Authorization: Bearer <token>` en todos los requests  
**Content-Type:** `application/json`  
**Token:** generado en https://sdd.bccr.fi.cr → Iniciar sesión → Mi Perfil → Generar token

---

## Endpoints

| Nombre | Método | Path |
|--------|--------|------|
| Series de indicador económico | GET | `/indicadoresEconomicos/{codigo}/series` |
| Metadata de indicador económico | GET | `/indicadoresEconomicos/{codigo}/metadata` |
| Descargar lista de indicadores (Excel) | GET | `/indicadoresEconomicos/descargar` |
| Series de cuadro | GET | `/cuadro/{codigo}/series` |
| Metadata de cuadro | GET | `/cuadro/{codigo}/metadata` |
| Descargar cuadros (Excel) | GET | `/cuadro/descargar` |
| Series de carpeta de preferencias | GET | `/misPreferencias/carpeta/{codigo}/series` |
| Metadata de carpeta de preferencias | GET | `/misPreferencias/carpeta/{codigo}/metadata` |
| Validar suscripción | POST | `/Usuario/ValideSuscripcion` |

---

## Parámetros comunes

| Parámetro | Tipo | Ubicación | Descripción |
|-----------|------|-----------|-------------|
| `codigo` | integer | path | Código del indicador o cuadro |
| `fechaInicio` | string | query | Fecha inicio `yyyy/mm/dd` → URL-encode → `yyyy%2Fmm%2Fdd` |
| `fechaFin` | string | query | Fecha fin `yyyy/mm/dd` → URL-encode → `yyyy%2Fmm%2Fdd` |
| `idioma` | string | query | `ES` o `EN` |
| `correo` | string | query | Solo en ValideSuscripcion |
| `token` | string | query | Solo en ValideSuscripcion |

---

## Códigos de indicadores relevantes para eVista

| Código | Nombre | Uso |
|--------|--------|-----|
| 318 | Tipo de cambio venta USD/CRC | `ObtenerTipoCambioAsync` — facturación USD |
| 317 | Tipo de cambio compra USD/CRC | Referencia (no usado actualmente) |

---

## Estructura de respuesta — Series de indicador (más usada en eVista)

```json
{
  "estado": true,
  "mensaje": "Consulta exitosa",
  "datos": [
    {
      "codigoIndicador": "318",
      "nombreIndicador": "Tipo de cambio venta",
      "series": [
        { "fecha": "2026-05-28", "valorDatoPorPeriodo": 519.55 }
      ]
    }
  ]
}
```

- El valor buscado: `datos[0].series[0].valorDatoPorPeriodo`
- Si la fecha es feriado/fin de semana: `series` viene vacío o con `valorDatoPorPeriodo: null`
- **Solución**: retroceder hasta 3 días hasta encontrar un valor

---

## Estructura de respuesta — Metadata de indicador

```json
{
  "estado": true,
  "mensaje": "Consulta exitosa",
  "datos": [
    {
      "codIndicador": 318,
      "nombre": "Tipo de cambio venta",
      "periodicidad": "Diaria",
      "unidadDeMedida": "Colón Costarricense",
      "primerDato": "1983-01-01",
      "ultimoDatoSerie": "2026-05-27",
      "ultimaPublicacion": "2026-05-27"
    }
  ]
}
```

---

## Estructura de respuesta — Series de cuadro

```json
{
  "estado": true,
  "mensaje": "Consulta exitosa",
  "datos": [
    {
      "titulo": "Tipos de cambio",
      "periodicidad": "Diaria",
      "indicadores": [
        {
          "codigoIndicador": "318",
          "nombreIndicador": "Tipo de cambio venta",
          "series": [
            { "fecha": "2026-05-28", "valorDatoPorPeriodo": 519.55 }
          ]
        }
      ]
    }
  ]
}
```

---

## Estructura de respuesta — Validar suscripción

```json
{ "estado": true, "mensaje": "Suscripción válida", "datos": [] }
```

---

## Manejo de errores

```json
{ "CodigoError": "400", "Mensaje": "Parámetros de entrada inválidos." }
```

| HTTP | Significado |
|------|-------------|
| 200 | OK |
| 400 | Parámetros inválidos |
| 401 | Token inválido o expirado |
| 403 | Sin permiso |
| 404 | Recurso no encontrado |
| 429 | Demasiadas solicitudes (rate limit) |
| 500 | Error interno SDDE |
