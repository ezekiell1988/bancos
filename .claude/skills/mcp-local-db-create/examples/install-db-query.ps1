[CmdletBinding()]
param(
  [switch]$UpdateLock,
  [switch]$SkipSmoke
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../../..")).Path
$packagePath = Join-Path $projectRoot ".mcp/db-query"

if (-not (Test-Path (Join-Path $packagePath "package.json"))) {
  throw "No se encontro .mcp/db-query/package.json desde $projectRoot. Copie el paquete completo antes de instalarlo."
}

Push-Location $projectRoot
try {
  if ($UpdateLock) {
    npm --prefix $packagePath install
  } else {
    npm --prefix $packagePath ci
  }

  if (-not $SkipSmoke) {
    node .mcp/db-query/tests/smoke.mjs
  }
} finally {
  Pop-Location
}