[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$webRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $webRoot)
$certDirectory = Join-Path $webRoot 'certs'
$certificate = Join-Path $certDirectory 'localhost.pem'
$key = Join-Path $certDirectory 'localhost.key'

if (-not (Test-Path $certificate) -or -not (Test-Path $key)) {
    New-Item -ItemType Directory -Force -Path $certDirectory | Out-Null
    dotnet dev-certs https --trust --export-path $certificate --format Pem --no-password
}

$backend = Start-Process -FilePath 'dotnet' -ArgumentList 'watch', 'run', '--project', 'src/Bancos.Api', '--', '--urls', 'https://localhost:5001' -WorkingDirectory $repoRoot -PassThru -NoNewWindow

try {
    Set-Location $webRoot
    npm run start
}
finally {
    if (-not $backend.HasExited) {
        Stop-Process -Id $backend.Id -Force
    }
}
