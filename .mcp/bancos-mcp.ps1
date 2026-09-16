#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$projectRoot = "/Users/ezequielbaltodanocubillo/Documents/Bancos"
$mcpProject = Join-Path $projectRoot "src/Bancos.Mcp"

$existing = lsof -ti:8000 2>$null
if ($existing) {
    Write-Host "Liberando puerto 8000..."
    $existing | ForEach-Object { kill -9 $_ 2>$null }
    Start-Sleep -Seconds 1
}

& (Join-Path $PSScriptRoot "db-up.ps1")

Write-Host "Iniciando MCP HTTP y Hangfire..."
dotnet watch run --project $mcpProject
