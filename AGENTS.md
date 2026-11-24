# Agent instructions

A [MeitouClient](https://github.com/Meitou-Modding-Ecosystem/MeitouClient) code mod repo in the Meitou-Modding-Ecosystem org. GPL-3.0-or-later.
`CLAUDE.md` is canonical; keep `AGENTS.md` identical. Everything above `## This repo` is shared by the org's mod repos: change it in
MeitouModTemplate and copy it to the others. Procedures (local build broken, new Meitou release, compat issue, release): skill
`.claude/skills/meitou-engine-release`.

## Branches and PRs

- Work on your own branch, open a PR into `beta`, merge it yourself once CI is green. Never push to `beta`, `main` or `release`.
- `beta` -> `main` -> `release` are promotion PRs that need a super-maintainer's approval. Open them, and push release tags, only when the
  repo owner asks.
- One piece of work per PR, with a description that says what changed and why. Tests in this repo need no permission: add them.

## Engine

- `engine/` is the MeitouClient submodule. Never commit inside it or edit engine code here.
- Engine changes (hooks, fixes) go to MeitouClient as a PR into `fork`, and nowhere else. They reach this repo only when upstream
  (VlcekM/MeitouClient) releases them and the pin moves.
- What `engine` is pinned to:
  - with frameworks in `meitou.lock.json`: the locked frameworks' engine commit (framework-update moves both together);
  - without: the commit of an upstream release tag `vX.Y.Z`, never a fork or development commit.
- The pin, `Directory.Build.rsp` and each `gameVersion` floor move together (Versioning below).
- `git pull` does not move submodules. After every pull, and whenever a build can't find engine types, run
  `powershell -ExecutionPolicy Bypass -File tools\Update-Local.ps1 -Clean -Build` (docs/local-development.md, "Keep clones current").
- `Meitou.user.props` (git-ignored, never committed) may set `MeitouEngineDir` and `MeitouFrameworkSources` to build against sibling
  clones. Only for changing a framework and a mod together; otherwise it makes the build ignore this repo's pins.

## Versioning

Policy: [GutenStructure docs/versioning.md](https://github.com/TheIvoryDisaster/GutenStructure/blob/beta/docs/versioning.md). The parts that
bite:

- A mod's version lives only in its `meitou.json`; the assembly reads it. Bump it in any PR that changes what players get.
- Meitou refuses a DLL that references a newer engine assembly than the running game. Releases stamp the engine `X.Y.Z.0`; the submodule
  builds `1.0.0.0`. So `Directory.Build.rsp` (`-p:AssemblyVersion=<floor>.0`) stamps the references as the oldest supported release.
- `gameVersion` = `">=<floor> <cap>"`. Floor = the rsp version = the oldest release the mod is built for. Cap only below a release with a
  **known** break (RefCheck or a test found it); never cap ahead of time.
- Framework dependency ranges: `">=<first version with what you use> <<next minor>"`, e.g. `">=0.6.2 <0.7.0"`.
- `apiVersion` in `meitou.json`: the oldest code-mod API with everything the mod uses (an older game refuses a higher one). 2 adds
  `version`, `dependencies` and `gameVersion`; 3 (Meitou 0.22.2) adds chooser goals and `World.GiveOrder`. MeitouClient `docs/modding.md`.

## CI and automation

| Workflow | What it does | What you do |
| --- | --- | --- |
| `ci.yml` | build, tests, Engine references (RefCheck), Play deploy on every PR | merge only when green |
| `meitou-compat.yml` | daily RefCheck against the newest Meitou release, nightly and each floor; one `meitou-compat` issue; opens `Meitou: Admit <tag>` cap-lift PRs | a missing member is a port (skill); a nightly-only problem is a warning |
| `framework-update.yml` | moves `meitou.lock.json` (and `engine`) to new framework releases, PR into `beta` | a red PR is a regression report: fix it in that PR |
| `release.yml` | on each merge into `release`, tags it `<slug>-v<version>` from `meitou.json` and publishes `<Mod>-<version>.zip` via GutenStructure (skips an existing tag) | bump the version to release; `tools\Tag-Release.ps1` only as a fallback when asked |

The template has every workflow; a repo may not have them all yet (`.github/workflows/`).

A `needs-release` label means the merged change only reaches players after promotion and a tag: tell the owner.

## Docs

- Keep docs short: facts and instructions, one place per fact, no progress notes. Update the doc a change affects in the same PR.
- Setup guides: no `cd` unless the directory changes; bold each directory change; with more than two directories, list them first as
  PowerShell variables and use the variables in every command; precise wording.
- Task types and dialogue codes: claim only inside the mod's block of 100
  ([GutenStructure docs/numbers.md](https://github.com/TheIvoryDisaster/GutenStructure/blob/beta/docs/numbers.md)) and list each one in the
  README's `## Numbers` table (`NumbersTests` checks it).
- Working across repos (engine, framework, mod in one change): [docs/agents-across-repos.md](docs/agents-across-repos.md).

## This repo

- Way of the Fist (`way.of.the.fist`, folder `WayOfTheFist/`, slug `way-of-the-fist`): fist weapons, records added in the data phase
  (`WayOfTheFist.mod` holds only the item models' materials). No frameworks: `engine` pins an upstream tag (a nightly one until a release has
  the data phase). Docs `docs/way-of-the-fist.md`.
- The gauntlets' look is `WayOfTheFist/ItemModels/*.glb` with `"worn"` set (docs/structures.md "Worn on the hands"); the engine draws it only
  with worn unarmed weapons (MeitouClientSandbox `fork`, not yet upstream).
