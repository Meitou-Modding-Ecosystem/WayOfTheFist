---
name: translate-meitou-mod
description: Localize a new Kenshi/MeitouClient mod or refresh translations after code, game data, README, or documentation changes. Use for nine-language game text, documentation, localization audits, and translation PRs into beta.
---

# Translate a Meitou mod

Use the repository or repositories named by the user. Honor narrower requests such as “just the docs” or “update the Beastmaster conversation.” For a full pass, cover game text and human-facing documentation. Work from the current target branch, usually `beta`, and read the repository instructions and engine pin. Start from the latest source even when an earlier conversation names a version.

| Language | Game locale | Documentation directory if starting fresh |
| --- | --- | --- |
| French | fr_FR | fr |
| German | de_DE | de |
| Spanish | es_ES | es |
| Italian | it_IT | it |
| Brazilian Portuguese | pt_BR | pt-BR |
| Russian | ru_RU | ru |
| Japanese | ja_JP | ja |
| Korean | ko_KR | ko |
| Simplified Chinese | zh_CN | zh-CN |

Keep existing locale identifiers and paths when the repository already has a convention.

## Inventory and update

1. Locate the English sources, existing translations, their runtime consumers, and packaging rules. Check prior localization PRs and the merge base of any open translation branch. Reuse an open PR for the same work.
2. For a new mod, inventory each player-visible field by stable record ID and field, message context, or documentation path and section. Trace generated records, building names and descriptions, research unlock lists, item tooltips, right-click verbs, job queue captions, skill and status text, settings, panels, notifications, and every alternate NPC line and player reply. Open the registration and rendering code to find strings built at runtime. Inventory README files at every depth and human-facing guides under docs. Exclude translated copies, vendored dependencies, generated output and private agent instructions from the English inventory.
3. For an update, identify the English revision covered by the last translation pass using a recorded source commit or the previous PR. Compare it with current source, then reconcile the full current inventory against each locale to find older gaps. A change under an unchanged ID can invalidate all nine translations. With no reliable baseline, audit all entries against English. Track additions, revisions, confirmed renames and deletions. Preserve accurate translations and user edits. Carry wording across a rename only after confirming meaning and context. Retire obsolete keys after checking readers and backward compatibility. If everything matches, report the checked revision and skip an empty PR.
4. Translate complete sentences in context. Reuse the mod's terminology, regional language choices and NPC voice. Preserve gameplay facts, units, timing, prices, UI actions and unlocks. Keep identifiers, save keys, animation names, commands, and placeholders intact. Translate a formatted template before inserting names or numbers so the target language can reorder them. Distinguish display names from lookup names, particularly for console spawning and public mod APIs.

## Deliver playable text

Inspect the pinned engine and follow the mod's actual localization path. Check when it selects the game language relative to the data and initialization phases. For code-generated FCS records, apply localized names and text where the records are created, including the correct case of each description field. For custom jobs, trace the right-click offer and the active job label separately. Test both the NPC prompt and all response branches. Keep an English fallback for unknown keys, locales and dependent-mod content.

Use the repo's existing catalog format if it works. When adding one, verify it reaches the installed mod and that normal game entry points consume it. A translation spreadsheet can support review, but does not establish runtime coverage. A working example of embedded locale catalogs and runtime selection is [Husbandry](https://github.com/Meitou-Modding-Ecosystem/Husbandry/tree/beta/Husbandry). Check current code and compatibility before borrowing it.

## Deliver readable guides

Provide complete localized pages, including tables and procedures. Link from the English README using native language names. Link translated guides to one another and back to English. Preserve fenced and inline code, indentation, paths, configuration keys, examples, identifiers and exact UI labels needed to operate tools. Repair links and heading anchors after moving pages. Keep external references and meaningful image alt text. On updates, translate changed passages and repair affected navigation without rewriting unrelated sections. See the [KenshiRaces documentation layout](https://github.com/Meitou-Modding-Ecosystem/KenshiRaces/tree/beta/translations).

## Verify and deliver

- Compare the English inventory with each of the nine locales. Check missing and blank entries, duplicate keys, exact placeholder sets with multiplicity, and unexpected English leftovers. Allow genuine proper names and commands. Spot-check long descriptions, negation, numbers, and NPC voices.
- Exercise locale selection, English fallback, generated record fields, menu registrations and formatted messages with repository tests. Check that translated resources actually ship in the release layout. Run a live smoke test when available and distinguish it from unit and packaging checks.
- For docs, compare source and target page sets, code blocks (including indented fences), inline commands, table structure, and link destinations and fragments. Read representative rendered passages for meaning. Report whether native-speaker review occurred.
- Follow repository versioning for player-visible changes. Open a feature PR into `beta` with scope, source revision, coverage and test results. Monitor required CI on the current PR head. Correct failures and recheck after each push. When authorized, use the repository's beta review exception to merge after required checks pass. Respect independent approval requirements on promotion branches. Report the PR, merge status, and any gaps.
