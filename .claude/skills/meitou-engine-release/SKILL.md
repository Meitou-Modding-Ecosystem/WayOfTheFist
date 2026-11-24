---
name: meitou-engine-release
description: Use in a Meitou mod repo when a local build fails on engine types, when moving the engine to a new Meitou release, when a meitou-compat issue or cap-lift PR appears, or when preparing a mod or framework release.
---

# Engine, versions and releases in a Meitou mod repo

Rules and the CI table are in `CLAUDE.md`; policy in
[GutenStructure docs/versioning.md](https://github.com/TheIvoryDisaster/GutenStructure/blob/beta/docs/versioning.md). Every change below is
a PR into `beta` from your own branch, merged when CI is green.

## A local build fails on engine types

Symptoms: `The type or namespace name '<X>' could not be found` for an engine type, `Metadata file '...\<Mod>.dll' could not be found`.

1. Close Visual Studio, then from the repo root: `powershell -ExecutionPolicy Bypass -File tools\Update-Local.ps1 -Clean -Build`.
2. Act on each `FIX` line it prints: a submodule off its pin, or a `MeitouEngineDir` / `MeitouFrameworkSources` override in
   `Meitou.user.props` or the environment.
3. Still failing with `engine` on its pin: the code uses something the pinned engine lacks. Check `git -C engine log --oneline -1` against
   the release that added it; the fix is a pin move (below), not an engine edit.

## Move the engine to a new Meitou release

Only in a repo **without** frameworks in `meitou.lock.json`. With frameworks, the engine follows the lock: let framework-update move both,
or move the lock to a framework release built on that engine.

```powershell
git -C engine fetch --no-tags https://github.com/VlcekM/MeitouClient.git "refs/tags/vX.Y.Z:refs/tags/vX.Y.Z"
git -C engine checkout vX.Y.Z
git add engine
dotnet build
dotnet test
```

- Pin release tags only (`vX.Y.Z` of VlcekM/MeitouClient), never the fork's `fork` head or a nightly.
- Keep the floor (`Directory.Build.rsp` and each `gameVersion` floor) where it is unless the mod now needs the new release: it calls a
  member the older release lacks, or a port can't keep the old call. Then raise the rsp version and every floor to `X.Y.Z` together.
- CI's Engine references job (RefCheck) checks the built DLL against the floor release: a member missing there means raise the floor or
  avoid the member.
- Bump the mod's patch version in `meitou.json`. Say in the PR body which release the pin moved to and whether the floor moved.

## A meitou-compat issue or PR

- **Missing member in a full release** (the run fails): a port. Change the call, bump the patch version, and set the floor to the release
  that needs the port when the old and new calls can't coexist. A framework port keeps its contract, so it's a patch too.
- **Newer stamp**: `Directory.Build.rsp` is above a release the `gameVersion` admits. Set rsp to the floor.
- **Problem only in a nightly** (label `meitou-compat: nightly`): not urgent. Plan the port; don't cap ahead of a full release.
- **Code-mod API changed** (the issue asks for a play test): RefCheck can't see behaviour changes. Ask the owner to play test before
  lifting the cap by hand.
- **`Meitou: Admit <tag>` PR** (cap lift, label `needs-release`): it merges itself when CI is green. Tell the owner it needs a release.

## A framework release breaks dependents

In a framework repo (RivalryCore, LogisticsCore, ...), a change to the dependent contract bumps the **minor** version; everything else is a
patch (`.github/RELEASING.md` lists the contract where the repo has one). A minor bump's PR body gets a **Dependents** section: each
dependent repo, what it calls that changed, and what it must do. After the beta prerelease publishes, check each dependent's
framework-update PR and fix the red ones there.

## Prepare a release

1. In a PR into `beta`: bump `version` in each changed mod's `meitou.json`, and check `gameVersion` and `apiVersion` (`CLAUDE.md`,
   Versioning).
2. Only when the owner asks: open the promotion PRs `beta` -> `main` -> `release` (each needs a super-maintainer's approval).
3. The merge into `release` releases by itself: `release.yml` reads `meitou.json`, creates the tag `<slug>-v<version>` on the `release`
   head and publishes `<Mod>-<version>.zip` in the same run. If that tag already exists (no version bump) it publishes nothing. Check
   that the `Release` run on `release` (Actions, `release.yml`) succeeded and the release lists the zip.
   - Manual fallback, only if that run failed before creating the tag and the owner asks:
     `powershell -ExecutionPolicy Bypass -File tools\Tag-Release.ps1` (`-DryRun` prints the git commands; `-Slug` picks one of several
     mods). It needs a machine that can push tags: a cloud session's git proxy refuses them (HTTP 403), so give the owner the commands.
