using System.Text.Json;
using System.Text.Json.Serialization;

namespace StructureImport;

/// <summary>
/// How one model becomes an item's look: the optional <c>&lt;model&gt;.json</c> beside a <c>.glb</c> in <c>ItemModels</c> (docs/structures.md
/// "Item models"). Every field has a default, so a model needs no file at all.
/// </summary>
public sealed class ItemSpec
{
    /// <summary>
    /// The material record's name; default: the file name, dashes and underscores as spaces.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The longest side in metres (the model is scaled evenly to it); default: the model's own size, glTF being in metres.
    /// </summary>
    public float? Size { get; set; }

    /// <summary>
    /// Degrees the model is turned about the vertical axis before anything else.
    /// </summary>
    public float Yaw { get; set; }

    /// <summary>
    /// The largest texture side in pixels; bigger textures are halved until they fit.
    /// </summary>
    public int TextureSize { get; set; } = 1024;

    /// <summary>
    /// The JSON options: camelCase names, comments and trailing commas allowed, unknown names refused (a typo would else go unnoticed).
    /// </summary>
    static readonly JsonSerializerOptions s_json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>
    /// The settings file beside <paramref name="model"/> (same name, <c>.json</c>), or the defaults when there is none. Throws when the file is
    /// malformed or a value is out of range.
    /// </summary>
    public static ItemSpec ForModel(string model)
    {
        string path = Path.ChangeExtension(model, ".json");
        var spec = File.Exists(path)
            ? JsonSerializer.Deserialize<ItemSpec>(File.ReadAllText(path), s_json) ?? new ItemSpec()
            : new ItemSpec();
        spec.Validate(path);
        return spec;
    }

    /// <summary>
    /// The name of the model file <paramref name="model"/>'s material record.
    /// </summary>
    public string DisplayName(string model) =>
        string.IsNullOrWhiteSpace(Name) ? Path.GetFileNameWithoutExtension(model).Replace('-', ' ').Replace('_', ' ') : Name;

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/> naming <paramref name="path"/> for a value the import can't use.
    /// </summary>
    void Validate(string path)
    {
        string? problem = Size is { } s && (!float.IsFinite(s) || s <= 0) ? "size must be a number above 0"
            : !float.IsFinite(Yaw) ? "yaw must be a number"
            : TextureSize is < 4 or > StructureSpec.MaxTextureSize || (TextureSize & (TextureSize - 1)) != 0
                ? $"textureSize must be a power of two from 4 to {StructureSpec.MaxTextureSize}"
            : null;
        if (problem is not null) throw new InvalidDataException($"{path}: {problem}");
    }
}
