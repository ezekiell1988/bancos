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
$status = docker inspect --format '{{.State.Status}}' bancos-sql-1 2>$null
if ($status -ne 'running') {
    Write-Host "Levantando BD..."
    docker compose --project-directory $projectRoot up -d
    Start-Sleep -Seconds 8
}

Write-Host "Aplicando migraciones de Bancos.Mcp..."
dotnet ef database update --project $mcpProject --startup-project $mcpProject

Write-Host "Iniciando MCP y Hangfire..."
dotnet watch run --project $mcpProject
