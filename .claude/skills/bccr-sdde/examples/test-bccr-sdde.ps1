# test-bccr-sdde.ps1
# Prueba de integración contra el API SDDE del BCCR.
# Uso: .\test-bccr-sdde.ps1 -Token "TU_TOKEN" -Correo "usuario@itqs.cr"
#
# Requiere: PowerShell 7+ o Windows PowerShell 5.1

param(
    [Parameter(Mandatory)] [string] $Token,
    [Parameter(Mandatory)] [string] $Correo,
    [int]    $IndicadorDolar = 318,
    [string] $Idioma = "ES"
)

$BaseUrl = "https://apim.bccr.fi.cr/SDDE/api/Bccr.GE.SDDE.Publico.Indicadores.API"
$Headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type"  = "application/json"
}

function Invoke-Sdde {
    param([string]$Path)
    $url = "$BaseUrl$Path"
    Write-Host "`nGET $url" -ForegroundColor Cyan
    try {
        $resp = Invoke-RestMethod -Method GET -Uri $url -Headers $Headers
        return $resp
    } catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# ─── 1. Validar suscripción ───────────────────────────────────────────────────
Write-Host "`n=== 1. Validar suscripcion ===" -ForegroundColor Yellow
$url = "$BaseUrl/Usuario/ValideSuscripcion?correo=$([Uri]::EscapeDataString($Correo))&token=$([Uri]::EscapeDataString($Token))"
Write-Host "POST $url" -ForegroundColor Cyan
try {
    $sub = Invoke-RestMethod -Method POST -Uri $url -Headers $Headers
    Write-Host "  estado : $($sub.estado)" -ForegroundColor Green
    Write-Host "  mensaje: $($sub.mensaje)"
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# ─── 2. Metadata del indicador dólar venta ───────────────────────────────────
Write-Host "`n=== 2. Metadata indicador $IndicadorDolar ===" -ForegroundColor Yellow
$meta = Invoke-Sdde "/indicadoresEconomicos/$IndicadorDolar/metadata?idioma=$Idioma"
if ($meta -and $meta.estado) {
    $d = $meta.datos[0]
    Write-Host "  Nombre         : $($d.nombre)" -ForegroundColor Green
    Write-Host "  Periodicidad   : $($d.periodicidad)"
    Write-Host "  Último dato    : $($d.ultimoDatoSerie)"
    Write-Host "  Última pub.    : $($d.ultimaPublicacion)"
} else {
    Write-Host "  Sin datos o error." -ForegroundColor Red
}

# ─── 3. Tipo de cambio hoy (retrocede hasta 3 días si es feriado/fin de sem.) ─
Write-Host "`n=== 3. Tipo de cambio (indicador $IndicadorDolar) ===" -ForegroundColor Yellow
$tzCR = [System.TimeZoneInfo]::FindSystemTimeZoneById("Central America Standard Time")
$hoy  = [System.TimeZoneInfo]::ConvertTimeFromUtc([datetime]::UtcNow, $tzCR)

$encontrado = $false
for ($offset = 0; $offset -le 3; $offset++) {
    $fecha    = $hoy.AddDays(-$offset)
    $fechaEnc = [Uri]::EscapeDataString($fecha.ToString("yyyy/MM/dd"))
    $series   = Invoke-Sdde "/indicadoresEconomicos/$IndicadorDolar/series?fechaInicio=$fechaEnc&fechaFin=$fechaEnc&idioma=$Idioma"

    if ($series -and $series.estado) {
        $valor = $series.datos |
                 ForEach-Object { $_.series } |
                 Where-Object { $_.valorDatoPorPeriodo -ne $null } |
                 Select-Object -First 1 -ExpandProperty valorDatoPorPeriodo

        if ($null -ne $valor) {
            Write-Host "  Fecha  : $($fecha.ToString('yyyy-MM-dd')) (offset -$offset días)" -ForegroundColor Green
            Write-Host "  Valor  : $valor CRC/USD" -ForegroundColor Green
            $encontrado = $true
            break
        } else {
            Write-Host "  $($fecha.ToString('yyyy-MM-dd')): sin dato publicado (feriado/fin de semana)"
        }
    }
}

if (-not $encontrado) {
    Write-Host "  No se encontró tipo de cambio en los últimos 3 días." -ForegroundColor Red
}

Write-Host "`n=== Prueba finalizada ===" -ForegroundColor Yellow
