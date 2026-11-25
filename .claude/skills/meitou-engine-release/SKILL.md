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

## Move the engine to a new Meitou build

The engine tracks the newest upstream tag (nightly or full release) on its own:
[engine-update](https://github.com/TheIvoryDisaster/GutenStructure/blob/beta/docs/engine-update.md) opens `Meitou: Move engine to <tag>`
in a repo **without** frameworks, moving `engine`, `Directory.Build.rsp` and every `gameVersion` floor together, and merges it when CI is
green. With frameworks, framework-update does the same once the framework has moved.

- **The PR is red**: the new Meitou broke the mod. Port it on that PR's branch (`engine-update/upstream`), bump nothing else, and let it
  merge.
- **Don't wait for the schedule**: run the `Engine update` workflow by hand (Actions, `workflow_dispatch`).
- Never pin a fork commit or hold the engine back.

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
