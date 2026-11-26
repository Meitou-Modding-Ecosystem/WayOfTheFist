using System.Text.Json;
using Meitou.Data.Fcs;
using Meitou.Simulation.Items;

namespace WayOfTheFist.Tests;

/// <summary>
/// The Crab Foot is the game's fists as a worn item following the hand, with the model's meshes and material; the names they use are the
/// ones the item model import writes.
/// </summary>
public sealed class CrabFootTests
{
    [Fact]
    public void The_crab_foot_is_the_fists_as_an_unarmed_item_with_the_models_meshes()
    {
        var weapon = Claw(hasMaterial: true);

        Assert.Equal(5, weapon.Ints["skill category"]);
        Assert.Equal(18, weapon.Ints["length"]);
        Assert.False(weapon.Bools.ContainsKey("hide hands"));
        Assert.Equal(1.2f, weapon.Floats["cut damage multiplier"]);
        Assert.True(weapon.Ints["value"] > 0);
        Assert.Equal(CrabFoot.Mesh, weapon.Filenames["mesh"]);
        Assert.Equal(CrabFoot.FemaleMesh, weapon.Filenames["mesh female"]);
        Assert.Equal(CrabFoot.GroundMesh, weapon.Filenames["ground mesh"]);
        Assert.Equal(CrabFoot.Material, Assert.Single(weapon.References["material"]).TargetStringId);
    }

    [Fact]
    public void Without_the_imported_material_the_crab_foot_names_none()
    {
        Assert.False(Claw(hasMaterial: false).References.ContainsKey("material"));
    }

    [Fact]
    public void The_names_match_what_the_import_writes_for_the_model()
    {
        string folder = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "ItemModels");
        using var ids = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "ids.json")));
        using var spec = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "crab-foot.json")));

        int block = ids.RootElement.GetProperty("blocks").GetProperty("crab-foot").GetInt32();
        string prefix = "wayofthefist_crab_foot";
        Assert.Equal(CrabFoot.Material, $"{950000 + 100 * block}-WayOfTheFist.mod");
        Assert.Equal("hands", spec.RootElement.GetProperty("worn").GetString());
        Assert.Equal("hand", spec.RootElement.GetProperty("follows").GetString());
        Assert.Equal(CrabFoot.GroundMesh, prefix + ".mesh");
        Assert.Equal(CrabFoot.Mesh, prefix + "_worn.mesh");
        Assert.Equal(CrabFoot.FemaleMesh, prefix + "_worn_female.mesh");
    }

    [Fact]
    public void The_crab_foot_goes_into_the_back_and_hip_weapon_slots_even_when_turned_in_the_grid()
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
        string path = Path.Combine(TestData.RepoRoot(), "WayOfTheFist", "icons", CrabFoot.Icon);

        var ids = Meitou.Data.Gameplay.ItemIconCache.Parse(Path.GetFileNameWithoutExtension(path));
        Assert.Equal((CrabFoot.Weapon, CrabFoot.Material, ""), ids);
        var (width, height) = TestData.PngSize(path);
        Assert.Equal(CrabFoot.FootprintWidth * height, CrabFoot.FootprintHeight * width);
    }

    /// <summary>
    /// The filled weapon record, with the imported material when <paramref name="hasMaterial"/>.
    /// </summary>
    static FcsRecord Claw(bool hasMaterial)
    {
        var weapon = new FcsRecord { StringId = CrabFoot.Weapon, RecordType = FcsRecordType.Weapon, Name = CrabFoot.Name };
        CrabFoot.Fill(weapon, TestData.Fists(), hasMaterial);
        return weapon;
    }

    /// <summary>
    /// A Crab Foot item as the inventory holds it, lying turned in a grid when <paramref name="rotated"/>.
    /// </summary>
    static ItemInstance Item(bool rotated) => new()
    {
        Record = CrabFoot.Weapon,
        Type = FcsRecordType.Weapon,
        AttachSlot = 0,
        Width = CrabFoot.FootprintWidth,
        Height = CrabFoot.FootprintHeight,
        Rotated = rotated,
    };
}
