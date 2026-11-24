<#
.SYNOPSIS
    Manual fallback: tags the release branch head as <slug>-v<version>, which runs release.yml and publishes the mod zip.

.DESCRIPTION
    Normally not needed: a push to release tags and publishes by itself (release.yml). Use this only when that run failed
    before creating the tag, and only when the repo owner asks.
    Reads the slug and mod folder from .github/workflows/release.yml and the version from that folder's meitou.json on
    the remote release branch, refuses a tag that already exists, prints the git commands, then runs them.

    The same steps by hand (RivalryCore 0.6.7 as the example):
        git fetch origin release
        git tag -a rivalry-core-v0.6.7 origin/release -m "rivalry-core 0.6.7"
        git push origin rivalry-core-v0.6.7

.PARAMETER Slug
    The mod to tag, when release.yml releases more than one (e.g. rivalry-core).

.PARAMETER DryRun
    Print the commands without running them.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Tag-Release.ps1 -DryRun
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Tag-Release.ps1
#>
[CmdletBinding()]
param(
    [string]$Slug,
    [switch]$DryRun,
    [string]$Remote = 'origin'
)

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    # git writes progress to stderr; Windows PowerShell 5.1 would turn that into an error under 'Stop'.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = & git @args 2>&1 | ForEach-Object { "$_" } } finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed:`n$($output -join "`n")" }
    return $output
}

$root = (Invoke-Git rev-parse --show-toplevel | Select-Object -First 1).Trim()
$workflow = Join-Path $root '.github/workflows/release.yml'
if (-not (Test-Path $workflow)) { throw "No .github/workflows/release.yml: this repo has no release workflow yet." }

# Mods release.yml can release: "slug: x" with "mod-folder: Y" (one mod), or case lines `x) echo "folder=Y"` (several).
$text = Get-Content $workflow -Raw
$mods = @()
$folderMatch = [regex]::Match($text, '(?m)^\s*mod-folder:\s*([A-Za-z0-9_.-]+)\s*$')
$slugMatch = [regex]::Match($text, '(?m)^\s*slug:\s*([a-z0-9-]+)\s*$')
if ($folderMatch.Success -and $slugMatch.Success) {
    $mods += [pscustomobject]@{ Slug = $slugMatch.Groups[1].Value; Folder = $folderMatch.Groups[1].Value }
}
foreach ($m in [regex]::Matches($text, '(?m)^\s*([a-z0-9-]+)\)\s*echo\s+"folder=([A-Za-z0-9_.-]+)"')) {
    $mods += [pscustomobject]@{ Slug = $m.Groups[1].Value; Folder = $m.Groups[2].Value }
}
if ($mods.Count -eq 0) { throw "Couldn't find a slug and mod folder in $workflow." }

if ($Slug) {
    $mod = $mods | Where-Object { $_.Slug -eq $Slug } | Select-Object -First 1
    if (-not $mod) { throw "release.yml has no mod '$Slug'. Mods: $(($mods | ForEach-Object Slug) -join ', ')" }
} elseif ($mods.Count -eq 1) {
    $mod = $mods[0]
} else {
    throw "release.yml releases several mods; pass -Slug with one of: $(($mods | ForEach-Object Slug) -join ', ')"
}

Write-Host "Fetching $Remote/release..."
Invoke-Git fetch $Remote "+refs/heads/release:refs/remotes/$Remote/release" | Out-Null
$commit = (Invoke-Git rev-parse "$Remote/release" | Select-Object -First 1).Trim()

$manifestText = (Invoke-Git show "$Remote/release:$($mod.Folder)/meitou.json") -join "`n"
$version = ($manifestText | ConvertFrom-Json).version
if (-not $version) { throw "$($mod.Folder)/meitou.json on $Remote/release has no version." }
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$version' isn't a release version X.Y.Z." }

$tag = "$($mod.Slug)-v$version"
$remoteTag = Invoke-Git ls-remote --tags $Remote "refs/tags/$tag"
if ($remoteTag) {
    throw "$tag already exists on $Remote, so $version is already released. Bump the version in $($mod.Folder)/meitou.json (PR into beta) and promote again."
}
& git rev-parse -q --verify "refs/tags/$tag" *> $null
$localTag = $LASTEXITCODE -eq 0

$commands = @()
if ($localTag) { $commands += "git tag -d $tag" }
$commands += "git tag -a $tag $commit -m `"$($mod.Slug) $version`""
$commands += "git push $Remote $tag"

Write-Host ""
Write-Host "Release $($mod.Folder) $version from $Remote/release ($($commit.Substring(0, 7))). Commands:"
Write-Host "  git fetch $Remote release"
$commands | ForEach-Object { Write-Host "  $_" }
Write-Host ""

if ($DryRun) {
    Write-Host "Dry run: nothing was tagged."
    return
}

if ($localTag) { Invoke-Git tag -d $tag | Out-Null }
Invoke-Git tag -a $tag $commit -m "$($mod.Slug) $version" | Out-Null
Invoke-Git push $Remote $tag | Out-Null
Write-Host "Pushed $tag."

$url = (Invoke-Git remote get-url $Remote | Select-Object -First 1).Trim()
if ($url -match 'github\.com[:/](.+?)(\.git)?$') {
    Write-Host "release.yml is running: https://github.com/$($Matches[1])/actions/workflows/release.yml"
    Write-Host "Zip, once it finishes: https://github.com/$($Matches[1])/releases/tag/$tag"
}
