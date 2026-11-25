# Publishing to the Steam Workshop

You create the mod's Workshop item once, by hand, from a release ZIP. After that, [Workshop Watcher](https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/workshop-watcher.md)
runs on your Windows PC and uploads each new GitHub release to that item on a schedule. All steps run in **Windows PowerShell** on your PC,
signed in to the Steam account that will own the item.

The commands use these values. Set them once per PowerShell window:

```powershell
$Repo = "Meitou-Modding-Ecosystem/WayOfTheFist"
$Slug = "way-of-the-fist"
$Mod = "WayOfTheFist"
$Version = "0.1.0"
$Work = "$HOME\Downloads\$Mod-Workshop"
$Watcher = "$env:LOCALAPPDATA\WorkshopWatcher\WorkshopWatcher.ps1"
```

| Value | What it is | Where to find it |
| --- | --- | --- |
| `$Repo` | The GitHub repository, `<owner>/<name>`. | The repository's URL. |
| `$Slug` | The tag prefix. Never changes. | `slug` in `.github/workflows/release.yml`. |
| `$Mod` | The mod's folder name, which starts the ZIP's name. Never changes. | `mod-folder` in `.github/workflows/release.yml`. |
| `$Version` | The release's version, without a `v`. Changes with each release. | `version` in the mod's `meitou.json`, or the release's title. |

A release's tag is `$Slug-v$Version` (`way-of-the-fist-v0.1.0`) and its ZIP is `$Mod-$Version.zip` (`WayOfTheFist-0.1.0.zip`), so the ZIP
downloads from `https://github.com/$Repo/releases/download/$Slug-v$Version/$Mod-$Version.zip`. Copy `$Version` from a release that is not
marked **Pre-release**: the `way-of-the-fist-beta` and `way-of-the-fist-<version>-beta.<commit>` prereleases have other tags and ZIP names.

## 1. Release the mod

1. Merge `beta` into `main`, then open and merge a PR from `main` into `release` ([Branches](../README.md#branches)).
2. Wait for the Release workflow on `release`. It tags the merge `<slug>-v<version>` with the version in `meitou.json` (for example
   `way-of-the-fist-v0.1.0`) and creates a release with `<Mod>-<version>.zip` and `<Mod>-<version>.zip.sha256`. If that run fails before
   tagging, `powershell -ExecutionPolicy Bypass -File tools\Tag-Release.ps1` pushes the tag by hand (`-DryRun` only prints the commands).

## 2. Install Workshop Watcher and sign in to Steam

Follow [Install](https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/workshop-watcher.md#install) in the Workshop Watcher
guide. The installer downloads steamcmd to `%LOCALAPPDATA%\WorkshopWatcher\steamcmd` and signs it in to your Steam account. When it asks
`Add a mod to watch now?`, answer `n`: the item doesn't exist yet. Skip this step if Workshop Watcher is already installed.

## 3. Prepare the item's files

1. Download the release ZIP and unpack it into `$Work`:

   ```powershell
   New-Item -ItemType Directory -Force $Work
   $ProgressPreference = "SilentlyContinue"
   Invoke-WebRequest "https://github.com/$Repo/releases/download/$Slug-v$Version/$Mod-$Version.zip" -OutFile "$Work\$Mod.zip"
   Expand-Archive "$Work\$Mod.zip" -DestinationPath $Work
   ```

   `$Work\$Mod` now holds `$Mod.dll`, `meitou.json` and `$Mod.mod`.
2. Save the item's preview image as `$Work\preview.png`: PNG or JPG, under 1 MB, square (512 x 512 pixels works).

## 4. Write the VDF

steamcmd creates the item from a VDF file: a text file of `"key" "value"` pairs inside a `"workshopitem"` block. This command writes
`$Work\create.vdf` with the paths filled in. Edit `title` and `description` first:

```powershell
$Dir = $Work.Replace('\', '/')
@"
"workshopitem"
{
    "appid"           "233860"
    "publishedfileid" "0"
    "contentfolder"   "$Dir/$Mod"
    "previewfile"     "$Dir/preview.png"
    "visibility"      "2"
    "title"           "[Meitou] My Mod"
    "description"     "What the mod does. Requires MeitouClient."
    "changenote"      "$Mod $Version"
}
"@ | Set-Content "$Work\create.vdf" -Encoding ASCII
```

| Key | Value |
| --- | --- |
| `appid` | `233860`, Kenshi's Steam app ID. |
| `publishedfileid` | `0` creates a new item. Any other number updates that existing item. |
| `contentfolder` | Full path to the folder whose files become the item's content: the mod folder from the ZIP, not its parent. |
| `previewfile` | Full path to the preview image. |
| `visibility` | `0` public, `1` friends only, `2` private, `3` unlisted. Start with `2` and change it on the item's page. |
| `title` | The item's name on the Workshop. Start it with `[Meitou]`, as other Meitou code mods do. |
| `description` | One line of text. Write the full description on the item's page afterwards (it supports Steam's BBCode). |
| `changenote` | The first entry of the item's Change Notes tab. |

Rules for values:

- Write paths with forward slashes (`C:/Users/me/Downloads/...`). A backslash must be doubled (`C:\\Users\\me\\...`).
- A double quote inside a value is written `\"`.
- Keep the file ASCII.

## 5. Create the item

```powershell
& "$env:LOCALAPPDATA\WorkshopWatcher\steamcmd\steamcmd.exe" +login <steam account name> +workshop_build_item "$Work\create.vdf" +quit
```

`<steam account name>` is the name you sign in to Steam with, as given to the installer. steamcmd uses the saved login. If it asks for a
password or Steam Guard code, run `powershell -ExecutionPolicy Bypass -File $Watcher login`, then run the command again.

The run ends with `Success.`. Open Steam > your profile > Workshop Items, open the new item and copy the number after `?id=` in its URL:
that is the item ID.

## 6. Finish the item's page

On the item's page in Steam:

1. Accept the Steam Workshop legal agreement if the page shows the banner. Until you do, only you can see the item.
2. Write the full description.
3. If the mod needs another Workshop mod (for example Rivalry Core), add it under **Add/Remove Required Items**. Subscribers are then
   asked to subscribe to it too. With the dependency in `meitou.json` `dependencies`, the launcher's Arrange puts it first in the load order.
4. Change the visibility to **Public** when the page is ready.

## 7. Watch the item

```powershell
powershell -ExecutionPolicy Bypass -File $Watcher add-item -Repo $Repo -AppId 233860 -ItemId <item ID> -Asset "$Mod-*.zip"
powershell -ExecutionPolicy Bypass -File $Watcher run -DryRun
```

The dry run reports the current release as already uploaded. Remove `$Work` once it does.

## Each release after that

Release the mod as in step 1. The watcher's next scheduled run downloads the ZIP, checks its SHA-256 and uploads it to the item, with the
release title and link as the change note. It never changes the item's title, description, preview or visibility: change those on the
item's page. `powershell -ExecutionPolicy Bypass -File $Watcher status` shows the schedule, the next run and the last upload.
