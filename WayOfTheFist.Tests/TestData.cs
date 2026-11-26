using Meitou.Data;
using Meitou.Data.Fcs;

namespace WayOfTheFist.Tests;

/// <summary>
/// What the fist weapons' tests share: a synthetic stand-in for the game's fists and the repo's files.
/// </summary>
public static class TestData
{
    /// <summary>
    /// A PNG's width and height, from its IHDR chunk.
    /// </summary>
    public static (int Width, int Height) PngSize(string path)
    {
        byte[] head = File.ReadAllBytes(path)[16..24];
        int width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(head);
        int height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(head.AsSpan(4));
        return (width, height);
    }

    /// <summary>
    /// The game's <c>FISTS</c>, with the base data's length, indoors bonus and multipliers.
    /// </summary>
    public static GameRecord Fists()
    {
        var fists = new FcsRecord { StringId = FistWeapons.Fists, RecordType = FcsRecordType.Weapon, Name = "FISTS" };
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
        return GameDatabase.Load(order).Find(FistWeapons.Fists)!;
    }

    /// <summary>
    /// The repo's root: the first folder above the test's output that holds README.md.
    /// </summary>
    public static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "README.md"))) return dir.FullName;
        }

        throw new DirectoryNotFoundException("No README.md above " + AppContext.BaseDirectory);
    }
}
