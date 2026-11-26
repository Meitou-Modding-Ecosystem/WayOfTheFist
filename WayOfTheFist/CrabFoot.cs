using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Modding;

namespace WayOfTheFist;

/// <summary>
/// The Crab Foot (docs/way-of-the-fist.md): a WEAPON of <c>skill category</c> 5 (Unarmed) copied from the game's <c>FISTS</c>, a strapped sleeve on
/// each forearm ending in a crab's shell over the fist, which grips the strap below the sleeve (<c>ItemModels/crab-foot.glb</c>, following the hand),
/// sold by the Ronin weapon smiths (<see cref="FistWeapons"/>).
/// </summary>
public static class CrabFoot
{
    /// <summary>
    /// The weapon's record id.
    /// </summary>
    public const string Weapon = "3-WayOfTheFist.mod";

    /// <summary>
    /// The weapon's name.
    /// </summary>
    public const string Name = "Crab Foot";

    /// <summary>
    /// The MATERIAL_SPECS_CLOTHING the item model import writes for the model (block 2 of <c>ItemModels/ids.json</c>: 950000 + 100 × block).
    /// </summary>
    public const string Material = "950200-WayOfTheFist.mod";

    /// <summary>
    /// The model's meshes the import writes: worn on a man's and a woman's hands, and the plain one on the ground.
    /// </summary>
    public const string Mesh = "wayofthefist_crab_foot_worn.mesh", FemaleMesh = "wayofthefist_crab_foot_worn_female.mesh",
        GroundMesh = "wayofthefist_crab_foot.mesh";

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
    /// The weapon's fields: <paramref name="fists"/>' with a claw's cut and crush, an item's price, weight and size, and the model's meshes; its
    /// material only when <paramref name="hasMaterial"/>.
    /// </summary>
    public static void Fill(FcsRecord weapon, GameRecord fists, bool hasMaterial)
    {
        FistWeapons.CopyFrom(fists, weapon);
        weapon.Strings["description"] = "A strapped leather sleeve under a crab's shell, its legs curled over the fist. The wearer fights with Martial "
            + "Arts, and the claws both cut and crush.";

        weapon.Ints["skill category"] = 5;
        weapon.Floats["cut damage multiplier"] = 1.2f;
        weapon.Floats["blunt damage multiplier"] = 1.1f;
        weapon.Floats["bleed mult"] = 0.5f;
        weapon.Floats["armour penetration"] = 0.2f;
        weapon.Floats["robot damage mult"] = 1;

        weapon.Ints["value"] = 4000;
        weapon.Floats["weight kg"] = 6;
        weapon.Ints["inventory footprint width"] = FootprintWidth;
        weapon.Ints["inventory footprint height"] = FootprintHeight;
        weapon.Bools["auto icon image"] = true;

        weapon.Filenames["mesh"] = Mesh;
        weapon.Filenames["mesh female"] = FemaleMesh;
        weapon.Filenames["ground mesh"] = GroundMesh;
        if (hasMaterial) weapon.References["material"] = [new FcsReference(Material, 0, 0, 0)];
    }
}
