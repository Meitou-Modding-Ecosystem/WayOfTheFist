using System.Text.Json;
using Meitou.Data.Fcs;
using Meitou.Simulation.Items;

namespace WayOfTheFist.Tests;

/// <summary>
/// The Dog Knuckles are the game's fists as a worn item that hides the hands, with the model's meshes and material; the names they use are the
/// ones the item model import writes.
/// </summary>
public sealed class DogKnucklesTests
{
    [Fact]
    public void The_knuckles_are_the_fists_as_an_unarmed_item_that_hides_the_hands()
    {
        var weapon = Knuckles(hasMaterial: true);

        Assert.Equal(5, weapon.Ints["skill category"]);
        Assert.Equal(18, weapon.Ints["length"]);
        Assert.True(weapon.Bools["hide hands"]);
        Assert.Equal(1.4f, weapon.Floats["blunt damage multiplier"]);
        Assert.True(weapon.Ints["value"] > 0);
        Assert.Equal(DogKnuckles.Mesh, weapon.Filenames["mesh"]);
        Assert.Equal(DogKnuckles.FemaleMesh, weapon.Filenames["mesh female"]);
        Assert.Equal(DogKnuckles.GroundMesh, weapon.Filenames["ground mesh"]);
        Assert.Equal(DogKnuckles.Material, Assert.Single(weapon.References["material"]).TargetStringId);
    }

    [Fact]
    public void Without_the_imported_material_the_knuckles_name_none()
    {
        Assert.False(Knuckles(hasMaterial: false).References.ContainsKey("material"));
    }

    [Fact]
    public void The_names_match_what_the_import_writes_for_the_model()
    {
        string folder = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "ItemModels");
        using var ids = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "ids.json")));
        using var spec = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "dog-knuckles.json")));

        int block = ids.RootElement.GetProperty("blocks").GetProperty("dog-knuckles").GetInt32();
        string prefix = "wayofthefist_dog_knuckles";
        Assert.Equal(DogKnuckles.Material, $"{950000 + 100 * block}-WayOfTheFist.mod");
        Assert.Equal("hands", spec.RootElement.GetProperty("worn").GetString());
        Assert.Equal("forearm", spec.RootElement.GetProperty("follows").GetString());
        Assert.Equal(DogKnuckles.GroundMesh, prefix + ".mesh");
        Assert.Equal(DogKnuckles.Mesh, prefix + "_worn.mesh");
        Assert.Equal(DogKnuckles.FemaleMesh, prefix + "_worn_female.mesh");
    }

    [Fact]
    public void Each_item_model_has_its_own_id_block()
    {
        string path = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "ItemModels", "ids.json");
        using var ids = JsonDocument.Parse(File.ReadAllText(path));

        var blocks = ids.RootElement.GetProperty("blocks").EnumerateObject().Select(p => p.Value.GetInt32()).ToList();
        Assert.Equal(blocks.Count, blocks.Distinct().Count());
    }

    [Fact]
    public void The_knuckles_go_into_the_back_and_hip_weapon_slots_even_when_turned_in_the_grid()
    {
        var inventory = new Inventory();
        var onBack = Item(rotated: true);
        var onHip = Item(rotated: false);

        // The back (10 x 2) takes two rows of two, then the hip (7 x 1) one.
        Assert.True(InventoryCommands.TryEquip(inventory, onBack));
        for (int i = 0; i < 3; i++) Assert.True(InventoryCommands.TryEquip(inventory, Item(rotated: false)));
        Assert.True(InventoryCommands.TryEquip(inventory, onHip));

        Assert.Equal(("back", false), (onBack.Section, onBack.Rotated));
        Assert.Equal("hip", onHip.Section);
    }

    [Fact]
    public void The_icon_is_named_as_the_games_saved_pictures_and_shaped_as_the_footprint()
    {
        string path = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "icons", DogKnuckles.Icon);

        var ids = Meitou.Data.Gameplay.ItemIconCache.Parse(Path.GetFileNameWithoutExtension(path));
        Assert.Equal((DogKnuckles.Weapon, DogKnuckles.Material, ""), ids);
        var (width, height) = TestData.PngSize(path);
        Assert.Equal(DogKnuckles.FootprintWidth * height, DogKnuckles.FootprintHeight * width);
    }

    /// <summary>
    /// The filled weapon record, with the imported material when <paramref name="hasMaterial"/>.
    /// </summary>
    static FcsRecord Knuckles(bool hasMaterial)
    {
        var weapon = new FcsRecord { StringId = DogKnuckles.Weapon, RecordType = FcsRecordType.Weapon, Name = DogKnuckles.Name };
        DogKnuckles.Fill(weapon, TestData.Fists(), hasMaterial);
        return weapon;
    }

    /// <summary>
    /// A Dog Knuckles item as the inventory holds it, lying turned in a grid when <paramref name="rotated"/>.
    /// </summary>
    static ItemInstance Item(bool rotated) => new()
    {
        Record = DogKnuckles.Weapon,
        Type = FcsRecordType.Weapon,
        AttachSlot = 0,
        Width = DogKnuckles.FootprintWidth,
        Height = DogKnuckles.FootprintHeight,
        Rotated = rotated,
    };
}
