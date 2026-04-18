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

function Update-GeneratedGitHubUrls
{
    param(
        [string[]]$Paths
    )

    $credentialedGitHubUrlPattern = 'https://[^/@\s]+@github\.com/'
    foreach ($path in $Paths)
    {
        if (-not (Test-Path $path))
        {
            continue
        }

        Get-ChildItem -Path $path -File -Recurse -Include *.yml, *.yaml, *.json, *.html | ForEach-Object {
            $content = Get-Content -Path $_.FullName -Raw
            $normalizedContent = $content -replace $credentialedGitHubUrlPattern, 'https://github.com/'
            if ($normalizedContent -ne $content)
            {
                Set-Content -Path $_.FullName -Value $normalizedContent -Encoding utf8
            }
        }
    }
}

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
    Update-GeneratedGitHubUrls -Paths @($apiPath, $helpPath)
}
finally
{
    Pop-Location
}