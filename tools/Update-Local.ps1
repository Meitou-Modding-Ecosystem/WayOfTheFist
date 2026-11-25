<#
.SYNOPSIS
    Brings a local clone of this mod repo up to date and checks that it builds against the pinned engine.

.DESCRIPTION
    Run it from anywhere inside the clone, after every pull and whenever a build fails with missing engine types
    (e.g. "The type or namespace name 'RelationCause' could not be found").

      1. Switches to the branch (default beta) and pulls it (fast-forward only).
      2. Syncs and updates every submodule (engine, build/gutenstructure) to the commits this branch pins.
      3. Checks each submodule's checked-out commit against its pin and prints the engine version.
      4. Warns about overrides that make the build use a different engine: MeitouEngineDir (in Meitou.user.props or
         MeitouMods.user.props) and MeitouFrameworkSources, and notes fork engine mode (tools\Use-ForkEngine.ps1).
      5. With -Clean, deletes the git-ignored bin and obj folders, so no stale engine build is reused.
      6. With -Build, runs dotnet build.

    Works in Windows PowerShell 5.1 and PowerShell 7. Needs only git (and dotnet for -Build).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Update-Local.ps1 -Clean -Build
#>
[CmdletBinding()]
param(
    # The branch to switch to and pull.
    [string]$Branch = 'beta',

    # Delete the git-ignored bin and obj folders (here and in the submodules) after updating.
    [switch]$Clean,

    # Run dotnet build after updating.
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$script:Problems = 0

function Write-Step([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text) { Write-Host "   OK   $Text" -ForegroundColor Green }
function Write-Note([string]$Text) { Write-Host "   NOTE $Text" -ForegroundColor DarkYellow }
function Write-Problem([string]$Text) { Write-Host "   FIX  $Text" -ForegroundColor Yellow; $script:Problems++ }

function Invoke-Git {
    # git writes progress to stderr; Windows PowerShell 5.1 turns that into errors under Stop, so collect it as text.
    $ErrorActionPreference = 'Continue'
    $output = & git @args 2>&1 | ForEach-Object { "$_" }
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed:`n$($output -join "`n")" }
    return $output
}

function Get-RepoRoot {
    $start = Split-Path -Parent $PSScriptRoot
    $root = & git -C $start rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $root) { throw "Not inside a git clone: $start" }
    return (Resolve-Path $root).Path
}

function Update-Branch([string]$Name) {
    Write-Step "Pulling $Name"
    Invoke-Git fetch origin | Out-Null

    $current = (Invoke-Git rev-parse --abbrev-ref HEAD) -join ''
    if ($current -ne $Name) {
        Write-Host "   Switching from '$current' to '$Name'"
        Invoke-Git checkout $Name | Out-Null
    }

    Invoke-Git pull --ff-only origin $Name | Out-Null
    Write-Ok "$Name is at $((Invoke-Git log --oneline -1) -join '')"
}

function Update-Submodules {
    Write-Step 'Updating submodules'
    foreach ($line in (Invoke-Git config --file .gitmodules --get-regexp 'submodule\..*\.path')) {
        $path = $line.Split(' ')[1]
        if (-not (Test-Path (Join-Path $path '.git'))) { continue }

        $changes = Invoke-Git -C $path status --porcelain --untracked-files=no
        if ($changes) { throw "$path has uncommitted changes; commit, stash or discard them first:`n$($changes -join "`n")" }
    }

    Invoke-Git submodule sync --recursive | Out-Null
    Invoke-Git submodule update --init --recursive | Out-Null
}

function Test-Submodules {
    Write-Step 'Checking submodules against their pins'
    foreach ($line in (Invoke-Git submodule status --recursive)) {
        $state = $line.Substring(0, 1)
        $parts = $line.Substring(1).Trim().Split(' ')
        $sha = $parts[0].Substring(0, 7)
        $path = $parts[1]

        if ($state -eq ' ') { Write-Ok "$path at $sha (matches the pin)" }
        elseif ($state -eq '-') { Write-Problem "$path is not checked out; run this script again" }
        elseif ($state -eq '+') { Write-Problem "$path is at $sha, not the pinned commit; run this script again" }
        else { Write-Problem "$path has merge conflicts" }
    }

    $engine = Join-Path (Get-Location) 'engine'
    if (Test-Path (Join-Path $engine 'src')) {
        $version = (& git -C $engine describe --tags --always 2>$null) -join ''
        Write-Host "   Engine version: $version"
    }
}

function Test-Leftovers {
    Write-Step 'Checking for folders this branch does not have'
    $leftovers = Get-ChildItem -Directory -Force | Where-Object { $_.Name -ne '.git' -and -not (Test-Tracked $_.Name) }
    if (-not $leftovers) { Write-Ok 'None'; return }

    foreach ($folder in $leftovers) {
        Write-Note "$($folder.Name) is not in this branch (left over from an older checkout?). Delete it if you don't need it."
    }
}

function Test-Tracked([string]$Folder) {
    if (Invoke-Git ls-files $Folder | Select-Object -First 1) { return $true }

    & git check-ignore -q "$Folder/" 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Get-PropsOverride([string]$File, [string]$Property) {
    if (-not (Test-Path $File)) { return $null }

    $match = Select-String -Path $File -Pattern "<$Property>(.*?)</$Property>" | Select-Object -First 1
    if ($match) { return $match.Matches[0].Groups[1].Value }
    return $null
}

function Test-Overrides([string]$Root) {
    Write-Step 'Checking for build overrides'
    $ownEngine = (Join-Path $Root 'engine').TrimEnd('\', '/')
    $found = $false

    foreach ($name in 'Meitou.user.props', 'MeitouMods.user.props') {
        $file = Join-Path $Root $name
        $engineDir = Get-PropsOverride $file 'MeitouEngineDir'
        if ($engineDir) {
            $found = $true
            Test-EngineDir $engineDir $ownEngine "$name"
        }

        $sources = Get-PropsOverride $file 'MeitouFrameworkSources'
        if ($sources) {
            $found = $true
            Write-Problem "$name builds frameworks from local clones ($sources). Pull those clones too, or delete the line to use meitou.lock.json."
        }
    }

    if ($env:MeitouEngineDir) {
        $found = $true
        Write-Note "Fork engine mode is on: builds and Play use $env:MeitouEngineDir, not this repo's engine pin. Turn it off with tools\Use-ForkEngine.ps1 -Off."
    }

    if (-not $found) { Write-Ok "No overrides: the build uses this repo's engine and meitou.lock.json" }
}

function Test-EngineDir([string]$Value, [string]$OwnEngine, [string]$Source) {
    $resolved = $Value.TrimEnd('\', '/')
    if ((Test-Path $resolved) -and ((Resolve-Path $resolved).Path -eq $OwnEngine)) {
        Write-Ok "$Source sets MeitouEngineDir to this repo's engine"
        return
    }

    Write-Problem "$Source sets MeitouEngineDir to $Value, so builds ignore this repo's engine pin. Delete that line unless you are changing a framework and this mod together."
}

function Clear-BuildOutput([string]$Root) {
    Write-Step 'Deleting git-ignored bin and obj folders'
    $folders = Get-ChildItem -Path $Root -Directory -Recurse -Force -Include bin, obj -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/].+[\\/](bin|obj)$' }

    foreach ($folder in $folders) {
        & git -C $folder.Parent.FullName check-ignore -q "$($folder.Name)/" 2>$null
        if ($LASTEXITCODE -ne 0) { continue }

        Remove-Item -Recurse -Force $folder.FullName -ErrorAction SilentlyContinue
        Write-Host "   Deleted $($folder.FullName.Substring($Root.Length + 1))"
    }
}

function Invoke-Build {
    Write-Step 'Building'
    & dotnet build
    if ($LASTEXITCODE -ne 0) { Write-Problem 'dotnet build failed (see above)' }
}

$root = Get-RepoRoot
Push-Location $root
try {
    Update-Branch $Branch
    Update-Submodules
    Test-Submodules
    Test-Overrides $root
    Test-Leftovers
    if ($Clean) { Clear-BuildOutput $root }
    if ($Build) { Invoke-Build }
}
finally {
    Pop-Location
}

if ($script:Problems -gt 0) {
    Write-Host "`n$($script:Problems) thing(s) to fix above." -ForegroundColor Yellow
    exit 1
}

Write-Host "`nUp to date. Close and reopen the solution in Visual Studio if it was open during the update." -ForegroundColor Green
