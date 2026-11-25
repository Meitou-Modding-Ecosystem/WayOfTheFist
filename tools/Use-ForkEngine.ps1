<#
.SYNOPSIS
    Points every mod repo's local builds and Play at one engine, the head of the org fork, or back at each repo's own pin.

.DESCRIPTION
    Fork engine mode is for playing and testing the fork's hooks before upstream has them (docs/local-development.md "Play every mod on the
    fork"). It never changes a repo: the committed engine pins keep following upstream.

      (no switch)  updates the MeitouClient clone next to this repo (cloned when missing) to the fork's "fork" branch, fast-forward only,
                   and sets the user environment variable MeitouEngineDir to it. Every mod repo's build, its Play project and Visual Studio
                   then use that engine instead of the repo's engine submodule. Run it again to take the newest fork.
      -Off         removes MeitouEngineDir: each repo builds and plays its own engine submodule again.
      -Status      says which engine builds use now, changing nothing.

    Stops without changing anything when the clone has uncommitted changes. Restart Visual Studio and open a new terminal after switching:
    running programs keep the environment they started with. Works in Windows PowerShell 5.1 and PowerShell 7. Needs git.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Use-ForkEngine.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Use-ForkEngine.ps1 -Off
#>
[CmdletBinding(DefaultParameterSetName = 'On')]
param(
    # Stop using the fork: builds use each repo's own engine submodule again.
    [Parameter(ParameterSetName = 'Off', Mandatory = $true)]
    [switch]$Off,

    # Show which engine builds use, changing nothing.
    [Parameter(ParameterSetName = 'Status', Mandatory = $true)]
    [switch]$Status,

    # The MeitouClient clone (default: MeitouClient next to this repo's folder; cloned there when missing).
    [Parameter(ParameterSetName = 'On')]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
$ForkUrl = 'https://github.com/Meitou-Modding-Ecosystem/MeitouClientSandbox.git'
$Variable = 'MeitouEngineDir'

function Write-Step([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }

function Get-DefaultClone {
    $repo = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
    return Join-Path (Split-Path $repo -Parent) 'MeitouClient'
}

function Get-Clone {
    $clone = if ($Path) { $Path } else { Get-DefaultClone }
    if (Test-Path (Join-Path $clone '.git')) { return (Resolve-Path $clone).Path }

    Write-Step "Cloning the fork into $clone"
    & git clone $ForkUrl $clone
    if ($LASTEXITCODE -ne 0) { throw "git clone failed (exit $LASTEXITCODE)" }
    return (Resolve-Path $clone).Path
}

function Write-Current {
    $value = [Environment]::GetEnvironmentVariable($Variable, 'User')
    if (-not $value) {
        Write-Host "   Fork engine mode is off: each repo builds and plays its own engine submodule." -ForegroundColor Green
        return
    }

    $commit = (& git -C $value log -1 --format='%h %cd %s' --date=short 2>$null)
    Write-Host "   Fork engine mode is on: every repo builds and plays $value" -ForegroundColor Green
    if ($commit) { Write-Host "   at $($commit.Trim())" }
}

function Enable-Fork {
    $clone = Get-Clone
    Write-Step "Updating $clone to the fork's head"
    & (Join-Path $PSScriptRoot 'Switch-Engine.ps1') -To Fork -Path $clone

    $dir = $clone.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    [Environment]::SetEnvironmentVariable($Variable, $dir, 'User')
    $env:MeitouEngineDir = $dir
}

function Disable-Fork {
    [Environment]::SetEnvironmentVariable($Variable, $null, 'User')
    Remove-Item "env:$Variable" -ErrorAction SilentlyContinue
}

if ($Status) {
    Write-Current
    exit 0
}

Write-Step $(if ($Off) { 'Turning fork engine mode off' } else { 'Turning fork engine mode on' })
if ($Off) { Disable-Fork } else { Enable-Fork }
Write-Current
Write-Host "   Restart Visual Studio and open a new terminal, then rebuild (Play rebuilds the engine it runs)." -ForegroundColor Yellow
