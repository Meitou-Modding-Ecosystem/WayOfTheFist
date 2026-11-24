#!/usr/bin/env bash
# Checks that every mod in this repo, as built, only calls engine members a Meitou release still has (tools/RefCheck/Program.cs says why).
#
#   tools/refcheck.sh                   # against the newest full Meitou release
#   tools/refcheck.sh v0.21.0 v0.20.1   # against given release tags
#   CACHE=<dir> tools/refcheck.sh       # where downloaded release DLLs are kept
#
# Builds each mod (every top-level folder with meitou.json and <Folder>/<Folder>.csproj, as Play finds them) in Release, downloads the
# release's win-x64 zip once and keeps only its Meitou*.dll and 0Harmony.dll, then checks each assembly listed in the mod's meitou.json.
# Exit 0 when nothing is missing.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DOTNET="${DOTNET:-dotnet}"
CACHE="${CACHE:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/meitou-releases}"
REPO="${MEITOU_RELEASES:-https://github.com/VlcekM/MeitouClient}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

tags=("$@")
if [ ${#tags[@]} -eq 0 ]; then
  tags=("$(git ls-remote --tags "$REPO" | grep -oE 'refs/tags/v[0-9]+\.[0-9]+\.[0-9]+$' | sed 's|refs/tags/||' | sort -V | tail -1)")
fi
[ -n "${tags[0]}" ] || { echo "could not find a Meitou release tag"; exit 2; }

OUT="$(mktemp -d)"
"$DOTNET" build "$ROOT/tools/RefCheck/RefCheck.csproj" -c Release -o "$OUT/refcheck" -v q -nologo >"$OUT/refcheck.log" 2>&1 \
  || { cat "$OUT/refcheck.log"; echo "FAIL: RefCheck did not build"; exit 2; }

mods=()
for dir in "$ROOT"/*/; do
  name="$(basename "$dir")"
  [ -f "$dir/meitou.json" ] && [ -f "$dir/$name.csproj" ] && mods+=("$name")
done
[ ${#mods[@]} -gt 0 ] || { echo "no mods found"; exit 2; }
for m in "${mods[@]}"; do
  "$DOTNET" build "$ROOT/$m/$m.csproj" -c Release -o "$OUT/$m" -v q -nologo >"$OUT/$m.log" 2>&1 \
    || { tail -20 "$OUT/$m.log"; echo "FAIL: $m did not build"; exit 2; }
done

fail=0
for tag in "${tags[@]}"; do
  ver="${tag#v}"; dir="$CACHE/$ver"
  if ! ls "$dir"/Meitou*.dll >/dev/null 2>&1; then
    mkdir -p "$dir"
    curl -fsSL -o "$dir/m.zip" "$REPO/releases/download/$tag/Meitou-$ver-win-x64.zip" || { echo "FAIL: no win-x64 zip for $tag"; exit 2; }
    unzip -qjo "$dir/m.zip" '*/Meitou*.dll' '*/0Harmony.dll' -d "$dir" || { echo "FAIL: unexpected zip layout for $tag"; exit 2; }
    rm -f "$dir/m.zip"
  fi
  for m in "${mods[@]}"; do
    for dll in $(python3 -c "import json,sys;print(' '.join(json.load(open(sys.argv[1])).get('assemblies',[])))" "$ROOT/$m/meitou.json"); do
      if "$DOTNET" "$OUT/refcheck/RefCheck.dll" "$OUT/$m/$dll" "$dir" >"$OUT/result.txt" 2>&1; then
        echo "$tag $m/$dll: $(tail -1 "$OUT/result.txt")"
      else
        echo "$tag $m/$dll: calls engine members this release lacks:"; grep -E "MISSING|xception" "$OUT/result.txt" | sed 's/^/  /'
        fail=1
      fi
    done
  done
done
exit $fail
