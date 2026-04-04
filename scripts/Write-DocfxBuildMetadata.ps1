param(
    [string]$PublishedVersion = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$metadataPath = Join-Path $repoRoot 'docfx.build.json'
$generatedUtc = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'")

$footerParts = @(
    "<span>Generated $generatedUtc</span>"
)

if (-not [string]::IsNullOrWhiteSpace($PublishedVersion))
{
    $footerParts += '<span class="fastmoq-footer-separator">|</span>'
    $footerParts += "<span>Last tagged version $PublishedVersion</span>"
}

$metadata = [ordered]@{
    _generatedDate = $generatedUtc
    _publishedReleaseVersion = $PublishedVersion
    _appFooter = '<div class="fastmoq-footer-meta">' + ($footerParts -join '') + '</div>'
}

$metadata | ConvertTo-Json -Depth 5 | Set-Content -Path $metadataPath -Encoding utf8