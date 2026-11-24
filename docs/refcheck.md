# Engine references (RefCheck)

A mod is built against one engine (this repo's `engine` submodule) and played on whatever Meitou release the player has. When a release
changes the signature of something the mod calls, the mod's source may still compile, but the DLL that was already built asks the game for
the old member and fails at run time with `MissingMethodException`.

This is not hypothetical. Meitou 0.21 gave `Inventory.Add(ItemInstance)` a second, optional parameter
(`Add(ItemInstance, bool mainShut = false)`). Source that calls `bag.Add(item)` compiles against either version, but a DLL built against
0.20 or older calls the one-parameter method, which 0.21 no longer has. In The Wasteland Remembers that broke keepsakes in play, and the
exception stopped a whole system for the rest of the session. No test caught it: tests compile against one engine.

## What it checks

`tools/RefCheck` reads a built mod DLL's metadata and lists every engine member it references (methods, constructors, fields on types in
`Meitou*.dll`), by name and parameter types, that a given release doesn't have. Nothing of the mod or the game runs.

`tools/refcheck.sh` builds every mod in the repo (each top-level folder with `meitou.json` and `<Folder>/<Folder>.csproj`), downloads a
release's win-x64 zip once (keeping only `Meitou*.dll` and `0Harmony.dll`) and checks each assembly the mod's `meitou.json` lists.

```
tools/refcheck.sh                   # the newest full Meitou release
tools/refcheck.sh v0.21.0 v0.20.1   # given releases, e.g. the oldest your meitou.json allows
CACHE=<dir> tools/refcheck.sh       # where downloaded release DLLs are kept
```

Output, one line per release and assembly:

```
v0.21.0 ExampleMod/ExampleMod.dll: 1 engine references checked, 0 missing
```

or, for a break:

```
MISSING Meitou.Simulation.Items.Inventory::Add(Meitou.Simulation.Items.ItemInstance)
```

## In CI

The `Engine references` job runs `tools/refcheck.sh` against the newest release on every PR and push, and `Release readiness` waits for
it. A mod that ships a DLL built against an older engine should also check that DLL against each release its `meitou.json` allows.

## When it finds something

Call the member in a way that works on every release you support: rebuild against the newer engine if you drop the older ones, or find the
member by reflection and fill in the parameters you don't pass with their defaults. The Wasteland Remembers' `InventoryCompat` does the
latter for `Inventory.Add`.

Reflection calls into the engine are not checked: they fail softly where you wrote them to.
