using System.Text.Json;
using Meitou.Data;
using Meitou.Data.Fcs;

namespace WayOfTheFist.Tests;

/// <summary>
/// The Beak Gauntlets are the game's fists as a worn item with the model's meshes and material, built from a synthetic data file standing in for
/// the game; the names they use are the ones the item model import writes.
/// </summary>
public sealed class BeakGauntletsTests
{
    [Fact]
    public void The_gauntlets_are_the_fists_as_an_unarmed_item_with_the_models_meshes()
    {
        var weapon = new FcsRecord { StringId = BeakGauntlets.Weapon, RecordType = FcsRecordType.Weapon, Name = BeakGauntlets.Name };

        BeakGauntlets.Fill(weapon, Fists(), hasMaterial: true);

        Assert.Equal(5, weapon.Ints["skill category"]);
        Assert.Equal(18, weapon.Ints["length"]);
        Assert.Equal(8, weapon.Ints["indoors mod"]);
        Assert.Equal(1.4f, weapon.Floats["cut damage multiplier"]);
        Assert.True(weapon.Ints["value"] > 0);
        Assert.Equal(BeakGauntlets.Mesh, weapon.Filenames["mesh"]);
        Assert.Equal(BeakGauntlets.FemaleMesh, weapon.Filenames["mesh female"]);
        Assert.Equal(BeakGauntlets.GroundMesh, weapon.Filenames["ground mesh"]);
        Assert.Equal(BeakGauntlets.Material, Assert.Single(weapon.References["material"]).TargetStringId);
    }

    [Fact]
    public void Without_the_imported_material_the_gauntlets_name_none()
    {
        var weapon = new FcsRecord { StringId = BeakGauntlets.Weapon, RecordType = FcsRecordType.Weapon, Name = BeakGauntlets.Name };

        BeakGauntlets.Fill(weapon, Fists(), hasMaterial: false);

        Assert.False(weapon.References.ContainsKey("material"));
    }

    [Fact]
    public void The_names_match_what_the_import_writes_for_the_model()
    {
        string folder = Path.Combine(RepoRoot(), "WayOfTheFist", "ItemModels");
        using var ids = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "ids.json")));
        using var spec = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "beak-gauntlet.json")));

        int block = ids.RootElement.GetProperty("blocks").GetProperty("beak-gauntlet").GetInt32();
        string prefix = "wayofthefist_beak_gauntlet";
        Assert.Equal(BeakGauntlets.Material, $"{950000 + 100 * block}-WayOfTheFist.mod");
        Assert.Equal("hands", spec.RootElement.GetProperty("worn").GetString());
        Assert.Equal(BeakGauntlets.GroundMesh, prefix + ".mesh");
        Assert.Equal(BeakGauntlets.Mesh, prefix + "_worn.mesh");
        Assert.Equal(BeakGauntlets.FemaleMesh, prefix + "_worn_female.mesh");
    }

    /// <summary>
    /// The game's <c>FISTS</c>, with the base data's length, indoors bonus and multipliers.
    /// </summary>
    static GameRecord Fists()
    {
        var fists = new FcsRecord { StringId = BeakGauntlets.Fists, RecordType = FcsRecordType.Weapon, Name = "FISTS" };
        fists.Ints["skill category"] = 5;
        fists.Ints["length"] = 18;
        fists.Ints["indoors mod"] = 8;
        fists.Floats["cut damage multiplier"] = 1;
        fists.Floats["bleed mult"] = 0.05f;
        fists.Filenames["mesh"] = "";

        string path = Path.Combine(Directory.CreateTempSubdirectory("wayofthefist").FullName, "test.mod");
        var file = new FcsFile();
        file.Records.Add(fists);
        FcsWriter.WriteFile(file, path);
        var order = new LoadOrder();
        order.Entries.Add(new LoadOrderEntry("test.mod", path));
        return GameDatabase.Load(order).Find(BeakGauntlets.Fists)!;
    }

    /// <summary>
    /// The repo's root: the first folder above the test's output that holds README.md.
    /// </summary>
    static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "README.md"))) return dir.FullName;
        }

        throw new DirectoryNotFoundException("No README.md above " + AppContext.BaseDirectory);
    }
}
