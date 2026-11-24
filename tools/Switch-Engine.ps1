<#
.SYNOPSIS
    Switches a local MeitouClient clone to the latest upstream, the newest upstream release, the org fork or a branch, to play or test it.

.DESCRIPTION
    Works on a separate MeitouClient clone (default: the MeitouClient folder next to this repo), never on this repo's engine submodule,
    whose pin only Update-Local.ps1 and pin PRs move.

      Upstream  the newest commit of VlcekM/MeitouClient master (the remote "upstream", added when missing).
      Release   the newest upstream release tag vX.Y.Z.
      Fork      the org fork's integration branch "fork" (remote "origin"), pulled fast-forward only.
      -Branch   any branch of the org fork, e.g. a PR's branch.

    Upstream, Release and -Branch leave the clone on a detached commit, so the local master and fork branches stay as they were.
    Stops without changing anything when the clone has uncommitted changes. With -Run it builds the game in Release and starts it.

    Works in Windows PowerShell 5.1 and PowerShell 7. Needs git (and dotnet for -Build or -Run).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Switch-Engine.ps1 -To Upstream -Run

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Switch-Engine.ps1 -Branch claude/project-thread-ni0ivp -Path C:\src\MeitouClient
#>
[CmdletBinding(DefaultParameterSetName = 'To')]
param(
    # Where to switch to.
    [Parameter(ParameterSetName = 'To')]
    [ValidateSet('Upstream', 'Release', 'Fork')]
    [string]$To = 'Upstream',

    # A branch of the org fork to switch to instead.
    [Parameter(ParameterSetName = 'Branch', Mandatory = $true)]
    [string]$Branch,

    # The MeitouClient clone (default: MeitouClient next to this repo's folder).
    [string]$Path,

    # Run dotnet build of the game after switching.
    [switch]$Build,

    # Build the game in Release and start it after switching.
    [switch]$Run
)

$ErrorActionPreference = 'Stop'
$UpstreamUrl = 'https://github.com/VlcekM/MeitouClient.git'
$Game = 'src\Meitou.Game'

function Write-Step([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }

function Invoke-Git {
    & git -C $script:Clone @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed (exit $LASTEXITCODE)" }
}

function Get-Clone {
    if ($Path) { return (Resolve-Path $Path).Path }

    $repo = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
    $sibling = Join-Path (Split-Path $repo -Parent) 'MeitouClient'
    if (-not (Test-Path (Join-Path $sibling '.git'))) { throw "No MeitouClient clone at $sibling. Pass -Path <your MeitouClient clone>." }
    return $sibling
}

function Assert-Clean {
    $changes = & git -C $script:Clone status --porcelain --untracked-files=no
    if ($changes) { throw "$script:Clone has uncommitted changes. Commit or stash them first:`n$($changes -join "`n")" }
}

function Add-Upstream {
    $remotes = & git -C $script:Clone remote
    if ($remotes -notcontains 'upstream') {
        Write-Host "   adding remote upstream = $UpstreamUrl"
        Invoke-Git remote add upstream $UpstreamUrl
    }
}

function Switch-Upstream {
    Add-Upstream
    Invoke-Git fetch upstream master
    Invoke-Git checkout --detach upstream/master
}

function Switch-Release {
    Add-Upstream
    Invoke-Git fetch upstream --tags --force
    $tags = & git -C $script:Clone tag --list 'v*' | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' }
    if (-not $tags) { throw 'No upstream release tag vX.Y.Z found.' }

    $newest = $tags | Sort-Object { [version]$_.Substring(1) } | Select-Object -Last 1
    Invoke-Git checkout --detach "refs/tags/$newest"
}

function Switch-Fork {
    Invoke-Git fetch origin fork
    $local = & git -C $script:Clone branch --list fork
    if ($local) { Invoke-Git checkout fork } else { Invoke-Git checkout -b fork --track origin/fork }
    Invoke-Git merge --ff-only origin/fork
}

function Switch-Branch([string]$Name) {
    Invoke-Git fetch origin $Name
    Invoke-Git checkout --detach FETCH_HEAD
}

function Write-Where {
    $commit = (& git -C $script:Clone log -1 --format='%h %cd %s' --date=short).Trim()
    $described = (& git -C $script:Clone describe --tags --always 2>$null)
    Write-Host "   $script:Clone is now at $commit" -ForegroundColor Green
    if ($described) { Write-Host "   (git describe: $described)" }
}

$script:Clone = Get-Clone
Assert-Clean

$target = if ($PSCmdlet.ParameterSetName -eq 'Branch') { "branch $Branch" } else { $To }
Write-Step "Switching $script:Clone to $target"
if ($PSCmdlet.ParameterSetName -eq 'Branch') { Switch-Branch $Branch }
elseif ($To -eq 'Upstream') { Switch-Upstream }
elseif ($To -eq 'Release') { Switch-Release }
else { Switch-Fork }
Write-Where

if ($Run) {
    Write-Step 'Building and starting the game (Release)'
    & dotnet run --project (Join-Path $script:Clone $Game) -c Release
    exit $LASTEXITCODE
}

if ($Build) {
    Write-Step 'Building the game (Release)'
    & dotnet build (Join-Path $script:Clone $Game) -c Release
    exit $LASTEXITCODE
}
