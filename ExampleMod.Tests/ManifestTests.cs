using System.Text.Json;
using Meitou.Modding;

namespace ExampleMod.Tests;

/// <summary>
/// The manifest and the code agree: the loader refuses a mod whose entry is missing or whose id differs from what its code registers under.
/// </summary>
public sealed class ManifestTests
{
    [Fact]
    public void The_manifest_names_the_entry_and_its_id()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "meitou.json")));
        var root = manifest.RootElement;

        var entry = typeof(ExampleModEntry).Assembly.GetType(root.GetProperty("entry").GetString()!);

        Assert.NotNull(entry);
        Assert.True(typeof(IMod).IsAssignableFrom(entry));
        Assert.Equal(ExampleModEntry.Id, root.GetProperty("id").GetString());
    }
}
