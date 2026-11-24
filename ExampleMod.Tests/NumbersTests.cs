using System.Text.RegularExpressions;

namespace ExampleMod.Tests;

/// <summary>
/// The README's <c>## Numbers</c> section (GutenStructure docs/numbers.md): each task type and dialogue code the mod claims lies inside the mod's
/// block of 100, so no two of our mods (or another author's) claim the same one and the game refuses neither.
/// </summary>
public sealed partial class NumbersTests
{
    #region Checks

    [Fact]
    public void The_block_is_one_aligned_hundred_from_1000()
    {
        if (Block() is not var (first, last)) return;

        Assert.True(first >= 1000 && first % 100 == 0 && last == first + 99, $"Block {first}-{last} is not an aligned 100 from 1000.");
    }

    [Fact]
    public void Every_listed_number_lies_in_the_block()
    {
        var listed = Listed();
        if (Block() is not var (first, last))
        {
            Assert.True(listed.Count == 0, "The README lists numbers but names no block.");
            return;
        }

        var outside = listed.Where(n => n < first || n > last).Order().ToList();
        Assert.True(outside.Count == 0, $"Outside block {first}-{last}: {string.Join(", ", outside)}.");
    }

    #endregion

    #region README

    /// <summary>
    /// The README's block line: "Block 4400-4499".
    /// </summary>
    [GeneratedRegex(@"^Block (\d+)-(\d+)", RegexOptions.Multiline)]
    private static partial Regex BlockLine();

    /// <summary>
    /// A table row of the section: "| 4401 | ..." or "| 4510-4515 | ...".
    /// </summary>
    [GeneratedRegex(@"^\| (\d+)(?:-(\d+))? \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    /// <summary>
    /// The block the README names, or null when it names none.
    /// </summary>
    public static (int First, int Last)? Block() =>
        BlockLine().Match(Section()) is { Success: true } m ? (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : null;

    /// <summary>
    /// Every number the README's table lists, ranges expanded. A mod's own test can check its claimed constants are among them.
    /// </summary>
    public static HashSet<int> Listed()
    {
        var numbers = new HashSet<int>();
        foreach (Match row in Row().Matches(Section()))
        {
            int first = int.Parse(row.Groups[1].Value);
            int last = row.Groups[2].Success ? int.Parse(row.Groups[2].Value) : first;
            for (int n = first; n <= last; n++) numbers.Add(n);
        }

        return numbers;
    }

    /// <summary>
    /// The text of the README's <c>## Numbers</c> section, up to the next heading of that level.
    /// </summary>
    static string Section()
    {
        string text = File.ReadAllText(ReadmePath());
        int start = text.IndexOf("\n## Numbers", StringComparison.Ordinal);
        Assert.True(start >= 0, "The README has no '## Numbers' section (GutenStructure docs/numbers.md).");

        int end = text.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }

    /// <summary>
    /// The repo's README: the first README.md found walking up from the test's output folder.
    /// </summary>
    static string ReadmePath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string path = Path.Combine(dir.FullName, "README.md");
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException("No README.md above " + AppContext.BaseDirectory);
    }

    #endregion
}
