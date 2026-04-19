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

function Get-RepositoryDefaultBranch
{
    $originHead = & git -C $repoRoot symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($originHead))
    {
        return ($originHead -replace '^origin/', '')
    }

    foreach ($candidate in @('master', 'main'))
    {
        & git -C $repoRoot show-ref --verify --quiet "refs/heads/$candidate"
        if ($LASTEXITCODE -eq 0)
        {
            return $candidate
        }
    }

    $currentBranch = & git -C $repoRoot branch --show-current 2>$null
    if (-not [string]::IsNullOrWhiteSpace($currentBranch))
    {
        return $currentBranch.Trim()
    }

    return 'master'
}

function Update-GeneratedGitHubUrls
{
    param(
        [string[]]$Paths,
        [string]$DefaultBranch
    )

    $credentialedGitHubUrlPattern = 'https://[^/@\s]+@github\.com/'
    $yamlBranchPattern = '(?m)^(\s*branch:\s*).+$'
    $jsonBranchPattern = '(?m)^(\s*"branch"\s*:\s*").+?("\s*,?\s*)$'
    foreach ($path in $Paths)
    {
        if (-not (Test-Path $path))
        {
            continue
        }

        Get-ChildItem -Path $path -File -Recurse -Include *.yml, *.yaml, *.json, *.html | ForEach-Object {
            $content = Get-Content -Path $_.FullName -Raw
            $normalizedContent = $content -replace $credentialedGitHubUrlPattern, 'https://github.com/'
            $normalizedContent = [regex]::Replace($normalizedContent, $yamlBranchPattern, {
                param($match)
                $match.Groups[1].Value + $DefaultBranch
            })
            $normalizedContent = [regex]::Replace($normalizedContent, $jsonBranchPattern, {
                param($match)
                $match.Groups[1].Value + $DefaultBranch + $match.Groups[2].Value
            })
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
    $defaultBranch = Get-RepositoryDefaultBranch

    dotnet tool restore
    dotnet tool run docfx $docfxConfigPath
    Update-GeneratedGitHubUrls -Paths @($apiPath, $helpPath) -DefaultBranch $defaultBranch
}
finally
{
    Pop-Location
}