using Meitou.Modding;

namespace WayOfTheFist;

/// <summary>
/// Way of the Fist's entry (docs/way-of-the-fist.md): adds the fist weapons in the data phase, so the shipped WayOfTheFist.mod holds only the
/// item models' materials the import writes.
/// </summary>
public sealed class WayOfTheFistMod : IMod, IModDataPhase
{
    /// <summary>
    /// The mod's manifest id.
    /// </summary>
    public const string Id = "way.of.the.fist";

    public void EditData(ModDataContext c)
    {
        BeakGauntlets.Add(c);
        DogKnuckles.Add(c);
        FistWeapons.SellAtRonin(c, BeakGauntlets.Weapon, DogKnuckles.Weapon);
    }

    public void Initialize(ModContext c)
    {
        if (c.Data.Find(BeakGauntlets.Weapon) is null) c.LogError("The Beak Gauntlets are missing: the data phase did not add them (see the log).");
        if (c.Data.Find(DogKnuckles.Weapon) is null) c.LogError("The Dog Knuckles are missing: the data phase did not add them (see the log).");
    }
}
