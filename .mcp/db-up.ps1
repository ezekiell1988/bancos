#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$projectRoot = "/Users/ezequielbaltodanocubillo/Documents/Bancos"
$mcpProject = Join-Path $projectRoot "src/Bancos.Mcp.Tools"

$status = docker inspect --format '{{.State.Status}}' bancos-sql-1 2>$null
if ($status -ne 'running') {
    Write-Host "Levantando BD..."
    docker compose --project-directory $projectRoot up -d
    Start-Sleep -Seconds 8
}

Write-Host "Aplicando migraciones de Bancos.Mcp..."
dotnet ef database update --project $mcpProject --startup-project $mcpProject
