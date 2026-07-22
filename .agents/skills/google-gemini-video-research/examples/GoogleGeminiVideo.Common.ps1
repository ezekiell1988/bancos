Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GeminiCustomerConfig {
    param([Parameter(Mandatory)][int]$IdClientCustomer)

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../..')
    $secretsPath = Join-Path $repoRoot '.local-secrets/db.json'
    if (-not (Test-Path $secretsPath)) { throw 'No se encontró .local-secrets/db.json.' }
    $db = Get-Content $secretsPath -Raw | ConvertFrom-Json
    $connectionString = "Server=$($db.Server);Database=$($db.Database);User Id=$($db.User);Password=$($db.Password);TrustServerCertificate=True;"

    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = @'
SELECT apiKey, defaultModel, maxTokens, temperature
FROM dbo.tbClientCustomerGemini
WHERE idClientCustomer = @idClientCustomer;
'@
        $null = $command.Parameters.Add('@idClientCustomer', [System.Data.SqlDbType]::Int)
        $command.Parameters['@idClientCustomer'].Value = $IdClientCustomer
        $reader = $command.ExecuteReader()
        if (-not $reader.Read()) { throw "No existe configuración Gemini para idClientCustomer=$IdClientCustomer." }
        $result = [pscustomobject]@{
            ApiKey = $reader.GetString(0)
            DefaultModel = $reader.GetString(1)
            MaxTokens = $reader.GetString(2)
            Temperature = $reader.GetDecimal(3)
        }
        $reader.Close()
        return $result
    } finally {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) { $connection.Close() }
        $connection.Dispose()
    }
}

function Get-GoogleMimeType {
    param([Parameter(Mandatory)][string]$Path)
    switch ([IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        '.png' { 'image/png'; break }
        '.jpg' { 'image/jpeg'; break }
        '.jpeg' { 'image/jpeg'; break }
        '.webp' { 'image/webp'; break }
        '.mp4' { 'video/mp4'; break }
        '.mov' { 'video/mov'; break }
        default { throw "Formato no soportado: $Path" }
    }
}

function Get-GoogleInlineData {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path $Path)) { throw "Archivo no encontrado: $Path" }
    return @{ mimeType = Get-GoogleMimeType $Path; data = [Convert]::ToBase64String([IO.File]::ReadAllBytes((Resolve-Path $Path))) }
}

function Invoke-GoogleJson {
    param([Parameter(Mandatory)][string]$Uri, [Parameter(Mandatory)][string]$ApiKey, [Parameter(Mandatory)][object]$Body)
    try {
        return Invoke-RestMethod -Method Post -Uri $Uri -Headers @{ 'x-goog-api-key' = $ApiKey } -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 20)
    } catch {
        $response = $_.Exception.Response
        if ($null -ne $response) { throw "Google API HTTP $([int]$response.StatusCode): $($_.ErrorDetails.Message)" }
        throw
    }
}

function Save-GoogleVideoUri {
    param([Parameter(Mandatory)][string]$Uri, [Parameter(Mandatory)][string]$ApiKey, [Parameter(Mandatory)][string]$OutputPath)
    $folder = Split-Path -Parent $OutputPath
    if ($folder) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
    Invoke-WebRequest -Uri $Uri -Headers @{ 'x-goog-api-key' = $ApiKey } -OutFile $OutputPath
}
