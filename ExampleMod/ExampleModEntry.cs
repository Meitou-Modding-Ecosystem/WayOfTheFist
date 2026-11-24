using Meitou.Modding;

namespace ExampleMod;

/// <summary>
/// The mod's entry, named by "entry" in meitou.json: registers what the mod adds (engine docs/modding.md "Registration surface").
/// </summary>
public sealed class ExampleModEntry : IMod
{
    /// <summary>
    /// The mod's manifest id.
    /// </summary>
    public const string Id = "example.mod";

    public void Initialize(ModContext context)
    {
        context.Log("loaded");
    }
}
