param(
    [string]$PublishedVersion = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$helpPath = Join-Path $repoRoot 'Help'
$docfxConfigPath = Join-Path $repoRoot 'docfx.json'
$metadataScriptPath = Join-Path $PSScriptRoot 'Write-DocfxBuildMetadata.ps1'

& $metadataScriptPath -PublishedVersion $PublishedVersion

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