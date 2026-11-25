using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Modding;

namespace WayOfTheFist;

/// <summary>
/// The Beak Gauntlets (docs/way-of-the-fist.md): a WEAPON of <c>skill category</c> 5 (Unarmed) copied from the game's <c>FISTS</c>, so it fights
/// with Martial Arts as fists do with its own cut, bleed and penetration, worn on both hands (<c>ItemModels/beak-gauntlet.glb</c>, engine
/// docs/characters.md "Worn unarmed weapons"), and sold by the Ronin weapon smiths.
/// </summary>
public static class BeakGauntlets
{
    /// <summary>
    /// The weapon's record id.
    /// </summary>
    public const string Weapon = "1-WayOfTheFist.mod";

    /// <summary>
    /// The weapon's name.
    /// </summary>
    public const string Name = "Beak Gauntlets";

    /// <summary>
    /// The MATERIAL_SPECS_CLOTHING the item model import writes for the model (block 0 of <c>ItemModels/ids.json</c>: 950000 + 100 × block).
    /// </summary>
    public const string Material = "950000-WayOfTheFist.mod";

    /// <summary>
    /// The model's meshes the import writes: worn on a man's and a woman's hands, and the plain one on the ground.
    /// </summary>
    public const string Mesh = "wayofthefist_beak_gauntlet_worn.mesh", FemaleMesh = "wayofthefist_beak_gauntlet_worn_female.mesh",
        GroundMesh = "wayofthefist_beak_gauntlet.mesh";

    /// <summary>
    /// The inventory footprint, 4 x 1: a weapon slot takes an item only upright (the hip slot is 7 x 1, the back 10 x 2), and the icon
    /// (<see cref="Icon"/>) is drawn at that shape.
    /// </summary>
    public const int FootprintWidth = 4, FootprintHeight = 1;

    /// <summary>
    /// The inventory picture in the mod's <c>icons</c> folder: the game's saved-picture name, <c>&lt;item&gt;.&lt;material&gt;.png</c>, which it takes
    /// instead of drawing the weapon (engine docs/game/ui-screens.md "Item icons"). Found only with <see cref="Material"/>, which the item import writes.
    /// </summary>
    public const string Icon = Weapon + "." + Material + ".png";

    /// <summary>
    /// The game's fists, the record the gauntlets are copied from.
    /// </summary>
    public const string Fists = "FISTS";

    /// <summary>
    /// The VENDOR_LIST of the Ronin weapon smiths (<c>weapon vendor Ronin</c>), which sell blades to wanderers.
    /// </summary>
    public const string RoninVendor = "1026-gamedata.base";

    /// <summary>
    /// Adds the weapon and lists it with the Ronin weapon smiths. Throws when the game has no <see cref="Fists"/>.
    /// </summary>
    public static void Add(ModDataContext c)
    {
        var fists = c.Data.Find(Fists) ?? throw new InvalidOperationException("The game data has no FISTS to copy.");
        var weapon = c.Add(Weapon, FcsRecordType.Weapon, Name);
        Fill(weapon, fists, hasMaterial: c.Data.Find(Material) is not null);

        if (c.Data.Find(RoninVendor) is not null) c.Edit(RoninVendor).References["weapons"] = [new FcsReference(Weapon, 0, 0, 0)];
    }

    /// <summary>
    /// The weapon's fields: <paramref name="fists"/>' with a beak's cut and bleed, an item's price, weight and size, and the model's meshes; its
    /// material only when <paramref name="hasMaterial"/> (a released zip and the tests have no imported model).
    /// </summary>
    public static void Fill(FcsRecord weapon, GameRecord fists, bool hasMaterial)
    {
        CopyFrom(fists, weapon);
        weapon.Strings["description"] = "Hooked beak blades bound over both fists. The wearer fights with Martial Arts and tears what it hits.";

        weapon.Ints["skill category"] = 5;
        weapon.Floats["cut damage multiplier"] = 1.4f;
        weapon.Floats["blunt damage multiplier"] = 0.6f;
        weapon.Floats["bleed mult"] = 0.6f;
        weapon.Floats["armour penetration"] = 0.1f;
        weapon.Floats["robot damage mult"] = 0.8f;

        weapon.Ints["value"] = 3000;
        weapon.Floats["weight kg"] = 4;
        weapon.Ints["inventory footprint width"] = FootprintWidth;
        weapon.Ints["inventory footprint height"] = FootprintHeight;
        weapon.Bools["auto icon image"] = true;

        weapon.Filenames["mesh"] = Mesh;
        weapon.Filenames["mesh female"] = FemaleMesh;
        weapon.Filenames["ground mesh"] = GroundMesh;
        if (hasMaterial) weapon.References["material"] = [new FcsReference(Material, 0, 0, 0)];
    }

    /// <summary>
    /// Copies every value and reference of a loaded record onto a new one.
    /// </summary>
    static void CopyFrom(GameRecord from, FcsRecord to)
    {
        foreach (var (key, value) in from.Bools) to.Bools[key] = value;
        foreach (var (key, value) in from.Floats) to.Floats[key] = value;
        foreach (var (key, value) in from.Ints) to.Ints[key] = value;
        foreach (var (key, value) in from.Vector3S) to.Vector3S[key] = value;
        foreach (var (key, value) in from.Vector4S) to.Vector4S[key] = value;
        foreach (var (key, value) in from.Strings) to.Strings[key] = value;
        foreach (var (key, value) in from.Filenames) to.Filenames[key] = value;
        foreach (string list in from.ReferenceLists) to.References[list] = [.. from.GetReferences(list).Select(Link)];
    }

    /// <summary>
    /// A loaded link as a data file's.
    /// </summary>
    static FcsReference Link(GameReference link) => new(link.TargetStringId, link.Values.Value0, link.Values.Value1, link.Values.Value2);
}
