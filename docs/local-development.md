# Local development setup

Clone only the repos you work on into one folder. Each builds on its own with `dotnet build`, and the frameworks it needs download and
hash-check themselves.

## Repos

Each mod repo is named after its mod.

| Repo | Holds | PRs go to |
| --- | --- | --- |
| MeitouClient | the engine and its mod hooks | `fork` |
| RivalryCore | Rivalry Core framework and its agent skill | `beta` |
| LogisticsCore | Logistics Core framework | `beta` |
| WarOrchestration | War Orchestration framework (uses Logistics Core) | `beta` |
| BastFront | Bast Front melee (uses War Orchestration and Logistics Core) | `beta` |
| AshenHand | The Ashen Hand (uses Rivalry Core) | `beta` |
| Husbandry | Husbandry | `beta` |
| SquadArena | SquadArena mod and the squad rating tool | `beta` |
| GutenStructure (TheIvoryDisaster) | shared CI, packaging, release gate, framework lock target | `beta` |

## Folders

These steps use eight folders. Set them as variables in every new PowerShell window, then paste the commands below as they are:

```powershell
$Repos     = "C:\Users\<you>\source\repos"
$Engine    = "$Repos\MeitouClient"
$Rivalry   = "$Repos\RivalryCore"
$Ashen     = "$Repos\AshenHand"
$Husbandry = "$Repos\Husbandry"
$Logistics = "$Repos\LogisticsCore"
$War       = "$Repos\WarOrchestration"
$Bast      = "$Repos\BastFront"
```

## Prerequisites

- .NET 11 SDK (each repo's `global.json` names the version) and Git.
- An editor: Visual Studio, VS Code with the C# Dev Kit, or Rider. A terminal alone also works.
- Kenshi installed. If it is not in `C:\Program Files (x86)\Steam\steamapps\common\Kenshi`, set the `KENSHI_PATH` environment variable
  to its folder.

## Clone

```powershell
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/RivalryCore.git $Rivalry
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/AshenHand.git $Ashen
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/Husbandry.git $Husbandry
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/LogisticsCore.git $Logistics
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/WarOrchestration.git $War
git clone --recurse-submodules https://github.com/Meitou-Modding-Ecosystem/BastFront.git $Bast
git clone https://github.com/Meitou-Modding-Ecosystem/MeitouClientSandbox.git $Engine
```

Every mod repo has its own `engine` submodule, so `$Engine` is needed only to write engine hooks.

## Build and play one mod

**Change to the mod's repo:**

```powershell
cd $Ashen
dotnet build
dotnet run --project Play
```

- The first build downloads each framework named in `meitou.lock.json` into `deps` and stops if its SHA-256 does not match.
- `dotnet run --project Play` copies the locked frameworks and the mod into `Kenshi\mods` and starts the engine. Visual Studio, VS Code
  and the installed launcher: [play.md](play.md).
- Enable the frameworks above the mod in the launcher, as for any dependency.

## Change a framework and a mod together

Point the mod repo at your framework clone:

```powershell
Set-Content "$Ashen\Meitou.user.props" "<Project><PropertyGroup><MeitouFrameworkSources>$Rivalry\RivalryCore\RivalryCore.csproj</MeitouFrameworkSources><MeitouEngineDir>$Rivalry\engine\</MeitouEngineDir></PropertyGroup></Project>"
```

- Builds in `$Ashen` now compile Rivalry Core from `$Rivalry` and use the engine in `$Rivalry\engine`, and Play deploys that Rivalry Core build.
- `Meitou.user.props` is git-ignored. If it already holds your Kenshi settings, add the two lines inside its `PropertyGroup` instead.
- `MeitouFrameworkSources` takes framework project paths separated by `;`, e.g. `$Logistics\LogisticsCore\LogisticsCore.csproj`.
  `MeitouEngineDir` names the engine the framework clone builds on.
- Delete the line to go back to the locked ZIP.

**Framework chains.** Bast Front's `meitou.lock.json` lists War Orchestration and Logistics Core. To change both frameworks and Bast Front
together, override both in Bast Front:

```powershell
Set-Content "$Bast\Meitou.user.props" "<Project><PropertyGroup><MeitouFrameworkSources>$War\WarOrchestration\WarOrchestration.csproj;$Logistics\LogisticsCore\LogisticsCore.csproj</MeitouFrameworkSources><MeitouEngineDir>$Bast\engine\</MeitouEngineDir></PropertyGroup></Project>"
```

The override passes down, so War Orchestration builds against `$Logistics` too, and the whole build uses one engine. Override only War Orchestration and it builds against the Logistics Core named in Bast Front's lock.

To ship it: merge the framework PR into `beta` of `$Rivalry`, which publishes a prerelease `rivalry-core-<version>-beta.<commit>`. Then, in
`$Ashen`, set `meitou.lock.json` to that tag and the asset, SHA-256 and engine commit from its release notes, and open the mod PR. In a
chain, ship top-down: Logistics Core, then a War Orchestration build locking it, then Bast Front locking both. After the framework's
production release, move the lock to `rivalry-core-v<version>`.

## Change the engine and a mod together

Check out your engine branch inside the mod repo's submodule:

```powershell
git -C "$Ashen\engine" fetch origin <engine branch>
git -C "$Ashen\engine" checkout FETCH_HEAD
```

- Don't commit the new engine pointer: engine pins move only to upstream tags, so the change reaches this repo with the first upstream
  nightly that has it (engine-update moves the pin).
- A mod that uses a framework also needs the framework rebuilt on that engine. Use the framework override above and check out the same
  engine branch in `$Rivalry\engine`.
- Order to land it: engine PR into `fork`; after an upstream nightly has it and engine-update moved `$Rivalry\engine`, the framework PR,
  then the mod PR moving its lock and `engine`.

## Switch the engine to play or test

To play the latest upstream, a release or an engine branch, switch your `$Engine` clone with the script in any mod repo:

```powershell
powershell -ExecutionPolicy Bypass -File "$Ashen\tools\Switch-Engine.ps1" -To Upstream -Path $Engine -Run
```

- `-To Upstream`: the newest commit of VlcekM/MeitouClient `master` (the script adds the `upstream` remote when missing).
- `-To Release`: the newest upstream release tag `vX.Y.Z`.
- `-To Fork`: the org fork's `fork` branch, pulled.
- `-Branch <name>`: a branch of the org fork, such as an engine PR's branch.
- Without `-Path` it uses the `MeitouClient` folder next to the mod repo. `-Run` builds the game in Release and starts it; `-Build` only
  builds it.
- Upstream, Release and `-Branch` leave the clone on a detached commit; the local `master` and `fork` branches are not changed. The script
  stops without changing anything when the clone has uncommitted changes.
- It never touches the mod repo's own `engine` submodule (that is `Update-Local.ps1` and the pins).

## Play every mod on the fork

To play or test the fork's hooks before upstream has them, point every mod repo at one engine, the head of the fork's `fork` branch. Run
in any mod repo:

```powershell
powershell -ExecutionPolicy Bypass -File "$Ashen\tools\Use-ForkEngine.ps1"
```

Then close Visual Studio and every terminal, reopen them, and build Play in each repo whose mod you play.

- It updates `$Engine` (cloned when missing) to the fork's head, fast-forward only, and sets the user environment variable
  `MeitouEngineDir` to it. Every mod repo's build, Play and Visual Studio then use that engine instead of the repo's `engine` submodule.
  The solution still lists the submodule's projects; the mods and Play don't use them.
- Run it again to take the newest fork. `-Status` says which engine builds use. `-Off` removes the variable: each repo builds its own
  `engine` submodule again (restart Visual Studio and rebuild).
- It stops without changing anything when `$Engine` has uncommitted changes. `-Path <folder>` uses another clone.
- Nothing is committed: the repos' engine pins keep following upstream. `Update-Local.ps1` notes when fork mode is on.

## Keep clones current

After every pull, and whenever a build fails with engine types missing (e.g. `The type or namespace name 'RelationCause' could not be
found`), close Visual Studio and run the update script in the mod's repo:

```powershell
powershell -ExecutionPolicy Bypass -File "$Ashen\tools\Update-Local.ps1" -Clean -Build
```

- It switches to `beta` and pulls it, then updates `engine` and `build/gutenstructure` to the commits `beta` pins (`git pull` alone
  leaves them on the old ones). `-Branch <name>` pulls another branch.
- It checks each submodule against its pin and prints the engine version.
- It flags `MeitouEngineDir` and `MeitouFrameworkSources` in `Meitou.user.props`: they make the build use another repo's engine or
  framework instead of this repo's pins. Delete them unless you are changing a framework and a mod together. It notes fork engine mode
  (the `MeitouEngineDir` environment variable, [above](#play-every-mod-on-the-fork)).
- `-Clean` deletes the git-ignored `bin` and `obj` folders, so no build of the old engine is reused. `-Build` runs `dotnet build`.
- It ends with `Up to date` or a list of lines marked `FIX`.

The next build downloads a framework again only when `meitou.lock.json` changed.
