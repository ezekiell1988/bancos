<#
.SYNOPSIS
    Devuelve el PIN de login activo mas reciente para un usuario, leyendo directo de
    tbLoginToken. Sirve para automatizar el login por PIN sin acceso a una bandeja de
    email real (el backend nunca expone el PIN por otra via).
.PARAMETER Email
    Email del usuario (tbLogin.emailLogin). Alternativa a -IdLogin.
.PARAMETER IdLogin
    idLogin directo, si ya se conoce. Alternativa a -Email.
.USAGE
    # 1. Disparar POST /api/v1/auth/request-token desde el navegador/Playwright primero,
    #    para que exista un LoginToken activo reciente.
    # 2. Leer el PIN:
    pwsh .agents/skills/m1on1-web2-browser-test/examples/get-pin.ps1 -Email usuario@dominio.com
    pwsh .agents/skills/m1on1-web2-browser-test/examples/get-pin.ps1 -IdLogin 1
.OUTPUT
    Imprime SOLO el token de 5 digitos en stdout (sin encabezados ni color) para poder
    capturarlo directo desde otro script: $pin = pwsh get-pin.ps1 -Email x@y.com
#>

[CmdletBinding()]
param(
    [Parameter(ParameterSetName = 'ByEmail')]
    [string]$Email,

    [Parameter(ParameterSetName = 'ById')]
    [int]$IdLogin
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Email -and -not $IdLogin) {
    Write-Error "Especifica -Email o -IdLogin"
    exit 1
}

$repoRoot    = Resolve-Path (Join-Path $PSScriptRoot '../../../..')
$secretsPath = Join-Path $repoRoot '.local-secrets/db.json'
$db          = Get-Content $secretsPath -Raw | ConvertFrom-Json
$cs          = "Server=$($db.Server);Database=$($db.Database);User Id=$($db.User);Password=$($db.Password);TrustServerCertificate=True;"

if ($Email) {
    $where = "l.emailLogin = @email"
} else {
    $where = "l.idLogin = @idLogin"
}

$sql = @"
SELECT TOP 1 lt.token
FROM dbo.tbLoginToken lt
JOIN dbo.tbLogin l ON l.idLogin = lt.idLogin
WHERE lt.isActive = 1 AND $where
ORDER BY lt.createAt DESC
"@

$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()
$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
if ($Email) { $cmd.Parameters.AddWithValue('@email', $Email) | Out-Null }
else        { $cmd.Parameters.AddWithValue('@idLogin', $IdLogin) | Out-Null }

$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    Write-Output $reader['token']
} else {
    Write-Error "No hay LoginToken activo. Dispara POST /api/v1/auth/request-token primero."
    exit 1
}
$conn.Close()
