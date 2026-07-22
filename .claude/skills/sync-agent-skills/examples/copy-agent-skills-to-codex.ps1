[CmdletBinding()]
param(
    [string]$Source = ".agents/skills",
    [string]$Destination = ".codex/skills",
    [switch]$NoMirror
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath "..\$Path"))
}

$sourcePath = Resolve-RepoPath -Path $Source
$destinationPath = Resolve-RepoPath -Path $Destination

if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) {
    throw "Source folder not found: $sourcePath"
}

if (-not (Test-Path -LiteralPath $destinationPath)) {
    New-Item -ItemType Directory -Path $destinationPath | Out-Null
}

$robocopyArgs = @(
    $sourcePath
    $destinationPath
    "/E"
    "/COPY:DAT"
    "/DCOPY:DAT"
    "/R:2"
    "/W:1"
    "/NFL"
    "/NDL"
    "/NJH"
    "/NJS"
    "/NP"
)

if (-not $NoMirror) {
    $robocopyArgs += "/MIR"
}

Write-Host "Syncing skills from '$sourcePath' to '$destinationPath'..."

& robocopy @robocopyArgs | Out-Host
$exitCode = $LASTEXITCODE

if ($exitCode -ge 8) {
    throw "Robocopy failed with exit code $exitCode"
}

Write-Host "Done. Robocopy exit code: $exitCode"
