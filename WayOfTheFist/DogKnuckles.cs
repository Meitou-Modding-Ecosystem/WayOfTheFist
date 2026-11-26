using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Modding;

namespace WayOfTheFist;

/// <summary>
/// The Dog Knuckles (docs/way-of-the-fist.md): a WEAPON of <c>skill category</c> 5 (Unarmed) copied from the game's <c>FISTS</c>, a strapped
/// sleeve on each forearm ending in a dog's head that swallows the fist (<c>ItemModels/dog-knuckles.glb</c>), with the wearer's hands hidden while
/// it is worn (engine docs/characters.md "Worn unarmed weapons"), sold by the Ronin weapon smiths (<see cref="FistWeapons"/>).
/// </summary>
public static class DogKnuckles
{
    /// <summary>
    /// The weapon's record id.
    /// </summary>
    public const string Weapon = "2-WayOfTheFist.mod";

    /// <summary>
    /// The weapon's name.
    /// </summary>
    public const string Name = "Dog Knuckles";

    /// <summary>
    /// The MATERIAL_SPECS_CLOTHING the item model import writes for the model (block 1 of <c>ItemModels/ids.json</c>: 950000 + 100 × block).
    /// </summary>
    public const string Material = "950100-WayOfTheFist.mod";

    /// <summary>
    /// The model's meshes the import writes: worn on a man's and a woman's hands, and the plain one on the ground.
    /// </summary>
    public const string Mesh = "wayofthefist_dog_knuckles_worn.mesh", FemaleMesh = "wayofthefist_dog_knuckles_worn_female.mesh",
        GroundMesh = "wayofthefist_dog_knuckles.mesh";

    /// <summary>
    /// The inventory footprint, 4 x 1, which both weapon slots take upright (as <see cref="BeakGauntlets.FootprintWidth"/>).
    /// </summary>
    public const int FootprintWidth = 4, FootprintHeight = 1;

    /// <summary>
    /// The inventory picture in the mod's <c>icons</c> folder, by the game's saved-picture name (as <see cref="BeakGauntlets.Icon"/>).
    /// </summary>
    public const string Icon = Weapon + "." + Material + ".png";

    /// <summary>
    /// Adds the weapon. Throws when the game has no <see cref="FistWeapons.Fists"/>.
    /// </summary>
    public static void Add(ModDataContext c)
    {
        var weapon = c.Add(Weapon, FcsRecordType.Weapon, Name);
        Fill(weapon, FistWeapons.FindFists(c), hasMaterial: c.Data.Find(Material) is not null);
    }

    /// <summary>
    /// The weapon's fields: <paramref name="fists"/>' with a heavy head's blunt bite, an item's price, weight and size, the model's meshes, and
    /// <c>hide hands</c> set, as the head encloses the fist; its material only when <paramref name="hasMaterial"/>.
    /// </summary>
    public static void Fill(FcsRecord weapon, GameRecord fists, bool hasMaterial)
    {
        FistWeapons.CopyFrom(fists, weapon);
        weapon.Strings["description"] = "A strapped leather sleeve ending in a carved dog's head that swallows the fist. The wearer fights with "
            + "Martial Arts and hits like a cudgel.";

        weapon.Ints["skill category"] = 5;
        weapon.Floats["cut damage multiplier"] = 0.8f;
        weapon.Floats["blunt damage multiplier"] = 1.4f;
        weapon.Floats["bleed mult"] = 0.3f;
        weapon.Floats["armour penetration"] = 0.15f;
        weapon.Floats["robot damage mult"] = 1.1f;

        weapon.Ints["value"] = 3500;
        weapon.Floats["weight kg"] = 5;
        weapon.Ints["inventory footprint width"] = FootprintWidth;
        weapon.Ints["inventory footprint height"] = FootprintHeight;
        weapon.Bools["auto icon image"] = true;
        weapon.Bools["hide hands"] = true;

        weapon.Filenames["mesh"] = Mesh;
        weapon.Filenames["mesh female"] = FemaleMesh;
        weapon.Filenames["ground mesh"] = GroundMesh;
        if (hasMaterial) weapon.References["material"] = [new FcsReference(Material, 0, 0, 0)];
    }
}
