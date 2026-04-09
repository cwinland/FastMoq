param(
    [string]$PublishedVersion = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiPath = Join-Path $repoRoot 'api'
$helpPath = Join-Path $repoRoot 'Help'
$docfxConfigPath = Join-Path $repoRoot 'docfx.json'
$metadataScriptPath = Join-Path $PSScriptRoot 'Write-DocfxBuildMetadata.ps1'

& $metadataScriptPath -PublishedVersion $PublishedVersion

foreach ($outputPath in @($apiPath, $helpPath))
{
    if (Test-Path $outputPath)
    {
        Remove-Item $outputPath -Recurse -Force
    }
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