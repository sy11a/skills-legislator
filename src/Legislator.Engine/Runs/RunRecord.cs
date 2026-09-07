using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Legislator.Engine.Runs;

/// <summary>
/// The record one apply wrote, as data rather than as a typed model. Three jobs read different
/// corners of it and one appends to it, and the edition that wrote a field must not lose it to
/// the edition that reads - the same reason the manifest is a <c>JsonNode</c>. The field names
/// live here as constants, because they are the record's schema and three jobs must spell them
/// identically.
/// </summary>
public static class RunRecord
{
    public const string ModeKey = "mode";

    public const string ReconstructedKey = "reconstructed";

    public const string VersionKey = "version";

    public const string VersionOldKey = "version_old";

    public const string StacksKey = "stacks";

    public const string RootKey = "root";

    public const string EntryKey = "entry";

    public const string OwnedKey = "owned";

    public const string ManifestKey = "manifest";

    public const string KeepKey = "keep";

    public const string FileModelKey = "file_model";

    public const string PreKey = "pre";

    public const string PostKey = "post";

    public const string CreatedField = "created";

    public const string OverwrittenField = "overwritten";

    public const string UnchangedField = "unchanged";

    public const string DeletedField = "deleted";

    public const string WrittenField = "written";

    public const string ExistedField = "existed";

    public const string AddedField = "added";

    public const string RemovedField = "removed";

    public const string RefusedField = "refused";

    public const string FinalField = "final";

    public const string Step4Field = "step4";

    public const string ImportsField = "imports";

    public const string RecopiedField = "recopied";

    public const string VerifyField = "verify";

    public const string CleanField = "clean";

    public const string FailuresField = "failures";

    public const string PathField = "path";

    public const string ReasonField = "reason";

    /// <summary>Reads the record apply left, refusing to guess: a run asked to verify or report without one is a run out of order, and saying so beats describing a tree from nothing.</summary>
    public static JsonObject Read(IFileSystem fs, string path)
    {
        ArgumentNullException.ThrowIfNull(fs);

        if (!fs.File.Exists(path))
        {
            throw new InvalidOperationException($"no run record at {path} - run `apply` first");
        }

        try
        {
            return JsonNode.Parse(fs.File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidOperationException($"run record malformed ({path}): not an object");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"run record malformed ({path}): {ex.Message}", ex);
        }
    }

    /// <summary>Writes the record, refusing any path inside the repository it describes.</summary>
    public static void Write(IFileSystem fs, string root, string path, JsonObject record)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(record);

        var full = fs.Path.GetFullPath(path).Replace('\\', '/');
        var inside = fs.Path.GetFullPath(root).Replace('\\', '/').TrimEnd('/');
        if (full == inside || full.StartsWith(inside + "/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"the run record must live outside the repository: {path}");
        }

        var directory = full[..full.LastIndexOf('/')];
        fs.Directory.CreateDirectory(directory);
        fs.File.WriteAllText(
            path,
            record.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentSize = 1 }) + "\n");
    }

    /// <summary>A JSON array of the strings given, in the order given - the shape every list in the record takes.</summary>
    public static JsonArray Array(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add((JsonNode?)JsonValue.Create(value));
        }

        return array;
    }

    /// <summary>The strings under <paramref name="path"/>, empty where the record does not carry them - a record written by an older edition is read, not judged.</summary>
    public static IReadOnlyList<string> Strings(JsonObject? record, params string[] path)
    {
        ArgumentNullException.ThrowIfNull(path);

        JsonNode? at = record;
        foreach (var step in path)
        {
            at = at is JsonObject obj && obj.TryGetPropertyValue(step, out var next) ? next : null;
        }

        return at is JsonArray array
            ? [.. array.Select(e => e?.GetValue<string>()).OfType<string>()]
            : [];
    }

    /// <summary>The object under <paramref name="path"/>, or null where the record does not carry it.</summary>
    public static JsonObject? Section(JsonObject? record, params string[] path)
    {
        ArgumentNullException.ThrowIfNull(path);

        JsonNode? at = record;
        foreach (var step in path)
        {
            at = at is JsonObject obj && obj.TryGetPropertyValue(step, out var next) ? next : null;
        }

        return at as JsonObject;
    }
}
