#!/usr/bin/env pwsh
$status = docker inspect --format '{{.State.Status}}' bancos-sql-1 2>$null
if ($status -ne 'running') {
    Write-Host "Levantando BD..."
    docker compose --project-directory "/Users/ezequielbaltodanocubillo/Documents/Bancos" up -d
    Start-Sleep -Seconds 8
}
dotnet watch run --project "/Users/ezequielbaltodanocubillo/Documents/Bancos/src/Bancos.Mcp"
