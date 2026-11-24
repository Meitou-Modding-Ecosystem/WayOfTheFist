<#
.SYNOPSIS
    Builds every project in this repo and deploys the mods to the game, without opening Visual Studio.

.DESCRIPTION
    Right-click > Run with PowerShell, or run it from a terminal. The window stays open at the end so you can read the result.

      1. Checks out the submodules (engine) if they are missing. It doesn't pull or switch branches: use Update-Local.ps1 for that.
      2. Runs dotnet build. That includes Play, which copies the mods and locked frameworks into the game's mods folder
         (docs/play.md). Close the game first: a running game locks the dll.
      3. With -Test, runs dotnet test. With -Run, starts this repo's engine afterwards (dotnet run --project Play).

    Works in Windows PowerShell 5.1 and PowerShell 7. Needs git and the .NET SDK.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Build.ps1 -Test
#>
[CmdletBinding()]
param(
    # Run the tests after building.
    [switch]$Test,

    # Start the game with this repo's engine after building.
    [switch]$Run,

    # Don't wait for Enter at the end (for terminals and scripts).
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }

function Wait-Close {
    if (-not $NoPause) { Read-Host "`nPress Enter to close" | Out-Null }
}

$failed = $null
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet is not installed or not on PATH (docs/play.md names the SDK).' }

    if (-not (Test-Path (Join-Path $root 'engine/src'))) {
        Write-Step 'Checking out submodules'
        $ErrorActionPreference = 'Continue'
        & git submodule update --init --recursive 2>&1 | ForEach-Object { "   $_" }
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0) { throw 'git submodule update failed (see above).' }
    }

    Write-Step "Building $(Split-Path -Leaf $root)"
    & dotnet build
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed (see above).' }

    if ($Test) {
        Write-Step 'Testing'
        & dotnet test --no-build
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed (see above).' }
    }

    if ($Run) {
        Write-Step 'Starting the game'
        & dotnet run --project Play --no-build
        if ($LASTEXITCODE -ne 0) { throw 'dotnet run failed (see above).' }
    }
}
catch {
    $failed = $_.Exception.Message
}
finally {
    Pop-Location
}

if ($failed) {
    Write-Host "`nFAILED: $failed" -ForegroundColor Red
    Wait-Close
    exit 1
}

Write-Host "`nBuilt and deployed." -ForegroundColor Green
Wait-Close
