Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$helpPath = Join-Path $repoRoot 'Help'
$docfxConfigPath = Join-Path $repoRoot 'docfx.json'

if (Test-Path $helpPath)
{
    Remove-Item $helpPath -Recurse -Force
}

Push-Location $repoRoot
try
{
    dotnet tool restore
    dotnet tool run docfx $docfxConfigPath
}
finally
{
    Pop-Location
}