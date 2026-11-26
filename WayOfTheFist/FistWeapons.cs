using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Modding;

namespace WayOfTheFist;

/// <summary>
/// What the fist weapons share (docs/way-of-the-fist.md): the game's fists they are copied from, and the Ronin weapon smiths who sell them.
/// </summary>
public static class FistWeapons
{
    /// <summary>
    /// The game's fists, the record each fist weapon is copied from.
    /// </summary>
    public const string Fists = "FISTS";

    /// <summary>
    /// The VENDOR_LIST of the Ronin weapon smiths (<c>weapon vendor Ronin</c>), which sell blades to wanderers.
    /// </summary>
    public const string RoninVendor = "1026-gamedata.base";

    /// <summary>
    /// The game's fists. Throws when the game has none.
    /// </summary>
    public static GameRecord FindFists(ModDataContext c) =>
        c.Data.Find(Fists) ?? throw new InvalidOperationException("The game data has no FISTS to copy.");

    /// <summary>
    /// Lists <paramref name="weapons"/> with the Ronin weapon smiths, in one change of their list; nothing when the game has no such list.
    /// </summary>
    public static void SellAtRonin(ModDataContext c, params string[] weapons)
    {
        if (c.Data.Find(RoninVendor) is null) return;

        c.Edit(RoninVendor).References["weapons"] = [.. weapons.Select(w => new FcsReference(w, 0, 0, 0))];
    }

    /// <summary>
    /// Copies every value and reference of a loaded record onto a new one.
    /// </summary>
    public static void CopyFrom(GameRecord from, FcsRecord to)
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
