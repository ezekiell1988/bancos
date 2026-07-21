[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = 'src/Bancos.Mcp/Bancos.Mcp.csproj'

dotnet ef database drop --force --project $project --startup-project $project --context McpCatalogDbContext
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet ef database update --project $project --startup-project $project --context McpCatalogDbContext
exit $LASTEXITCODE