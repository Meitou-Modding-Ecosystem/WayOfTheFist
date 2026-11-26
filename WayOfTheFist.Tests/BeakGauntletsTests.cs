using System.Text.Json;
using Meitou.Data;
using Meitou.Data.Fcs;
using Meitou.Simulation.Items;

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

        BeakGauntlets.Fill(weapon, TestData.Fists(), hasMaterial: true);

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

        BeakGauntlets.Fill(weapon, TestData.Fists(), hasMaterial: false);

        Assert.False(weapon.References.ContainsKey("material"));
    }

    [Fact]
    public void The_names_match_what_the_import_writes_for_the_model()
    {
        string folder = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "ItemModels");
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

    [Fact]
    public void The_gauntlets_go_into_the_back_and_hip_weapon_slots_even_when_turned_in_the_grid()
    {
        var inventory = new Inventory();
        var onBack = Gauntlets(rotated: true);
        var second = Gauntlets(rotated: false);
        var third = Gauntlets(rotated: false);
        var fourth = Gauntlets(rotated: false);
        var onHip = Gauntlets(rotated: false);

        // The back (10 x 2) takes two rows of two, then the hip (7 x 1) one.
        Assert.True(InventoryCommands.TryEquip(inventory, onBack));
        Assert.True(InventoryCommands.TryEquip(inventory, second));
        Assert.True(InventoryCommands.TryEquip(inventory, third));
        Assert.True(InventoryCommands.TryEquip(inventory, fourth));
        Assert.True(InventoryCommands.TryEquip(inventory, onHip));

        Assert.Equal(("back", false), (onBack.Section, onBack.Rotated));
        Assert.Equal("hip", onHip.Section);
    }

    [Fact]
    public void The_footprint_fits_both_weapon_slots_upright()
    {
        var weapon = new FcsRecord { StringId = BeakGauntlets.Weapon, RecordType = FcsRecordType.Weapon, Name = BeakGauntlets.Name };
        BeakGauntlets.Fill(weapon, TestData.Fists(), hasMaterial: true);

        int width = weapon.Ints["inventory footprint width"];
        int height = weapon.Ints["inventory footprint height"];
        foreach (string slot in new[] { "hip", "back" })
        {
            var size = InventoryCommands.SectionSize(slot);
            bool fits = width <= size.Width && height <= size.Height;
            Assert.True(fits, $"{width} x {height} does not fit the {slot} slot ({size.Width} x {size.Height})");
        }
    }

    [Fact]
    public void The_icon_is_named_as_the_games_saved_pictures_and_shaped_as_the_footprint()
    {
        string path = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "icons", BeakGauntlets.Icon);

        var ids = Meitou.Data.Gameplay.ItemIconCache.Parse(Path.GetFileNameWithoutExtension(path));
        Assert.Equal((BeakGauntlets.Weapon, BeakGauntlets.Material, ""), ids);
        var (width, height) = TestData.PngSize(path);
        Assert.Equal(BeakGauntlets.FootprintWidth * height, BeakGauntlets.FootprintHeight * width);
    }

    /// <summary>
    /// A Beak Gauntlets item as the inventory holds it, lying turned in a grid when <paramref name="rotated"/>.
    /// </summary>
    static ItemInstance Gauntlets(bool rotated) => new()
    {
        Record = BeakGauntlets.Weapon,
        Type = FcsRecordType.Weapon,
        AttachSlot = 0,
        Width = BeakGauntlets.FootprintWidth,
        Height = BeakGauntlets.FootprintHeight,
        Rotated = rotated,
    };
}
