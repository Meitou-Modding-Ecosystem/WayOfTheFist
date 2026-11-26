# Way of the Fist

Fist weapons: items equipped in a weapon slot that fight as fists (Martial Arts) and are worn on the hands.

## Beak Gauntlets

| | |
| --- | --- |
| Record | WEAPON `1-WayOfTheFist.mod`, added in the data phase (`BeakGauntlets`) |
| Copied from | the game's `FISTS`: `skill category` 5, `length` 18, `indoors mod` 8 |
| Damage | cut x1.4, blunt x0.6, bleed x0.6, armour penetration 0.1, robots x0.8 |
| Item | value 3000, 4 kg, 4 x 1 inventory squares (fits the hip and back slots, which take weapons only upright); icon `WayOfTheFist/icons/` |
| Sold by | the Ronin weapon smiths (`weapon vendor Ronin`, `1026-gamedata.base`), with their manufacturers (`FistWeapons`) |
| Model | `WayOfTheFist/ItemModels/beak-gauntlet.glb`, 0.6 m, one on each hand, the handle in the closed fingers and the skull on the back of the hand ([structures](structures.md#worn-on-the-hands)) |

- Equipped at the hip or back it makes the wearer fight unarmed with the gauntlets' multipliers, and shows on both forearms and hands whether
  or not they fight (engine `docs/characters.md` "Worn unarmed weapons").
- The look needs an engine with worn unarmed weapons (MeitouClientSandbox `fork`; not yet in an upstream release). On an engine without it
  the gauntlets still fight as fists but hang at the hip.
- The meshes and material come from the item model import on deploy, which reads the game's skeletons. A record without the imported material
  (`950000-WayOfTheFist.mod` missing) keeps the fists' blank look.

## Dog Knuckles

| | |
| --- | --- |
| Record | WEAPON `2-WayOfTheFist.mod`, added in the data phase (`DogKnuckles`) |
| Copied from | the game's `FISTS`, as the Beak Gauntlets |
| Damage | cut x0.8, blunt x1.4, bleed x0.3, armour penetration 0.15, robots x1.1 |
| Item | value 3500, 5 kg, 4 x 1 inventory squares; icon `WayOfTheFist/icons/` |
| Sold by | the Ronin weapon smiths, with the Beak Gauntlets |
| Model | `WayOfTheFist/ItemModels/dog-knuckles.glb`, 0.6 m, one on each forearm: the forearm bone runs along the sleeve's middle and the wrist sits 36% of the length from the cuff, so the fist is inside the dog's head; skinned to the forearm |
| Hands | `hide hands` set: the wearer's hands are hidden while it is worn (engine `docs/characters.md` "Worn unarmed weapons"; MeitouClientSandbox `fork`). On an engine without it the hands show inside the head |
