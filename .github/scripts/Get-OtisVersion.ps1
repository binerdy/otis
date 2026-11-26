[CmdletBinding()]
param(
    [string]$PropsPath
)

$ErrorActionPreference = 'Stop'
if (-not $PropsPath) {
    $PropsPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'Directory.Build.props'
}

[xml]$props = Get-Content -Raw -Path $PropsPath
$version = @($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }) |
    ForEach-Object { if ($_ -is [System.Xml.XmlElement]) { $_.InnerText } else { $_ } } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "No <Version> found in $PropsPath."
}

$version = $version.Trim()
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') {
    throw "<Version> '$version' in Directory.Build.props must be MAJOR.MINOR.PATCH."
}

$tag = "v$version"
git rev-parse --quiet --verify "refs/tags/$tag" *> $null
$released = $LASTEXITCODE -eq 0
$global:LASTEXITCODE = 0

if ($env:GITHUB_OUTPUT) {
    "version=$version" >> $env:GITHUB_OUTPUT
    "tag=$tag" >> $env:GITHUB_OUTPUT
    "released=$($released.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
}

[pscustomobject]@{ Version = $version; Tag = $tag; Released = $released }
