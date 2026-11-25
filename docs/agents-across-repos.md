# Agents working across repos

Give one agent every repo the change touches, tell it which repo owns each part, and have it land PRs in dependency order: engine, then
framework, then mod. One agent session per change, not one per repo.

## Give the agent the repos

- **Cloud session:** each repo must be attached to the session or project. The agent clones them side by side and works on its own branch
  in each.
- **Your PC (Remote Control):** the clones in the folders from [local-development.md](local-development.md). The agent can also do what
  cloud sessions can't, such as `gh repo create`.

## Where each part goes

| Change | Repo | PR into |
| --- | --- | --- |
| Engine hook (event, saved field, command) | MeitouClient | `fork` |
| Rivalry Core API or behaviour | RivalryCore | `beta` |
| Logistics Core (nodes, edges, owners, stock, transports) | LogisticsCore | `beta` |
| War Orchestration (combat scenarios on logistics, no story or characters) | WarOrchestration | `beta` |
| Bast Front melee | BastFront | `beta` |
| A mod's code, data generator, tests, docs | that mod's repo | `beta` |
| Shared CI, packaging, release gate, lock target | GutenStructure | `beta` |

## Order for a change that crosses repos

1. Engine PR into `fork`, merged. Engine pins move only to upstream tags (nightly or full release), so the framework and mod PRs that
   need it wait until upstream (VlcekM/MeitouClient) merges the change and builds a nightly; engine-update then moves the pins. Until then, build against the engine branch locally
   ([local-development.md](local-development.md#change-the-engine-and-a-mod-together)).
2. Framework PR that makes the change, once engine-update has moved its `engine` to a tag that has what it needs, merged into `beta`. CI publishes
   the beta prerelease.
3. Mod PR that moves `meitou.lock.json` to that prerelease (and `engine` to the lock's engine commit), merged into `beta`.
4. After the framework release is promoted and tagged: a mod PR that moves the lock to `<framework>-v<version>`.

Each PR links the one before it. A breaking framework API change bumps the framework's minor version and lists every dependent repo; each
dependent gets its own lock PR.

In a chain, step 2 repeats once per layer, top-down: a Logistics Core change lands, then a War Orchestration PR locks that Logistics build
and publishes its own, then the Bast Front PR locks both.

## Rules to paste into the agent's instructions

```markdown
- Repos and owners: engine hooks -> MeitouClient (PR into fork); Rivalry Core -> RivalryCore (beta);
  Logistics Core -> LogisticsCore (beta); War Orchestration -> WarOrchestration (beta); Bast Front -> BastFront (beta);
  mods -> their own repo (beta); shared CI/packaging -> GutenStructure (beta).
- Layers: logistics.core -> war.orchestration -> Bast Front. A layer uses the one above only through its public API and
  a dependencies range. War Orchestration holds no story or character content.
- Land PRs in order: engine, then each framework layer top-down, then the mod. One PR per repo per step. Self-merge each
  when CI is green; link the previous PR.
- A meitou.lock.json lists every framework in the chain (Bast Front locks War Orchestration AND Logistics Core), all on
  one engine commit, each accepted by the others' dependency ranges.
- To build against unmerged framework changes, set MeitouFrameworkSources (the sibling clones' framework .csproj paths, ';'-separated)
  and MeitouEngineDir in the repo's git-ignored Meitou.user.props. Never commit Meitou.user.props.
- A lock points only at an immutable tag: <framework>-<version>-beta.<commit> or <framework>-v<version>, with the
  asset, SHA-256 and engine commit copied from that release's notes. Never the rolling <framework>-beta tag.
- Never commit inside an engine/ submodule. engine/ follows the newest upstream Meitou tag (nightly or full release of
  VlcekM/MeitouClient) through engine-update.yml, or with a lock, the lock's engine commit through framework-update.yml. Both move
  Directory.Build.rsp and every gameVersion floor with it (GutenStructure docs/engine-update.md). Never pin the fork's fork head.
- After pulling, run tools/Update-Local.ps1 so local builds use the pinned engine.
- Don't merge a PR whose lock names a framework build that isn't published yet.
- Ask before release tags, promotions to main or release, and anything in another author's folder or repo.
- Report every PR link, in landing order.
```

## Prompt template

```text
Change: <what the player should see>
Repos: MeitouClient (<engine hook>), RivalryCore (<Rivalry Core change>), AshenHand (<mod change>)
Order: engine -> RivalryCore -> AshenHand, each PR into its integration branch, self-merge when green.
Build against unmerged changes with the local override; the final mod PR pins the published beta prerelease.
Follow the cross-repo rules. Report each PR link in order.
```
