#!/usr/bin/env pwsh
$existing = lsof -ti:8000 2>$null
if ($existing) {
    Write-Host "Liberando puerto 8000..."
    $existing | ForEach-Object { kill -9 $_ 2>$null }
    Start-Sleep -Seconds 1
}
$status = docker inspect --format '{{.State.Status}}' bancos-sql-1 2>$null
if ($status -ne 'running') {
    Write-Host "Levantando BD..."
    docker compose --project-directory "/Users/ezequielbaltodanocubillo/Documents/Bancos" up -d
    Start-Sleep -Seconds 8
}
dotnet watch run --project "/Users/ezequielbaltodanocubillo/Documents/Bancos/src/Bancos.Mcp"
