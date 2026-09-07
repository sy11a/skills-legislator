using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Legislator.Core.Repo;

namespace Legislator.Core.Manifest;

/// <summary>What one reading of `docs/ai/manifest.json` found: absent, present and unreadable, or present and parsed. The three are distinct answers because two callers want different things from the middle one - detect stops the run, audit reports it as a finding and carries on with an empty manifest.</summary>
public sealed record ManifestRead(bool Present, bool Parsed, JsonNode? Node, string? Reason);

/// <summary>
/// The manifest as data, never as a typed model. Its shape is the constitution's own record and
/// grows between editions - a v21 manifest names `profiles` where v25 names `stacks` - and one
/// consumer echoes the whole object back into detect's JSON. A typed model would drop whatever
/// the edition that wrote the file knew and the edition reading it does not.
/// </summary>
public static class ManifestFile
{
    /// <summary>The manifest's own field names, in one place: the record's schema, which the
    /// edition that reads it and the edition that wrote it must spell identically. `profiles`
    /// is the pre-v24 name of `stacks` and is read, never written.</summary>
    public const string StacksKey = "stacks";

    public const string ProfilesKey = "profiles";

    public const string OwnedFilesKey = "ownedFiles";

    public const string KeepKey = "keep";

    public const string VersionKey = "legislatorVersion";

    public const string PathField = "path";

    public const string ReasonField = "reason";

    /// <summary>Reads the manifest without judging it. A missing file is an ordinary answer (the layer is being installed), not a fault.</summary>
    public static ManifestRead TryRead(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        if (!fs.File.Exists(layout.Manifest))
        {
            return new ManifestRead(false, false, null, null);
        }

        try
        {
            return new ManifestRead(true, true, JsonNode.Parse(fs.File.ReadAllText(layout.Manifest)), null);
        }
        catch (JsonException ex)
        {
            return new ManifestRead(true, false, null, ex.Message);
        }
    }

    /// <summary>Reads the manifest and refuses to guess: a file that is there and does not parse stops the run naming itself, never silently reads as absent.</summary>
    public static JsonNode? Read(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var read = TryRead(fs, layout);
        return read is { Present: true, Parsed: false }
            ? throw new InvalidDataException($"{layout.Relative(layout.Manifest)} does not parse: {read.Reason}")
            : read.Node;
    }

    /// <summary>The array under <paramref name="key"/> as strings, empty when the key is absent, null, or not an array of strings - the manifest is a record, and a malformed corner of it is not worth a crash.</summary>
    public static IReadOnlyList<string> Strings(JsonNode? manifest, string key)
    {
        if (manifest is not JsonObject obj || !obj.TryGetPropertyValue(key, out var value) || value is not JsonArray array)
        {
            return [];
        }

        return [.. array.Select(e => e?.GetValue<JsonElement>())
            .Where(e => e?.ValueKind == JsonValueKind.String)
            .Select(e => e!.Value.GetString()!)];
    }

    /// <summary>Whether the manifest declares <paramref name="key"/> at all - the absence of `keep` is itself a finding (a pre-keep-schema manifest), which a defaulted empty list would hide.</summary>
    public static bool Declares(JsonNode? manifest, string key) =>
        manifest is JsonObject obj && obj.ContainsKey(key);

    /// <summary>The object entries under <paramref name="key"/>, each read through <paramref name="field"/>; entries that are not objects, or carry no such string, are skipped.</summary>
    public static IEnumerable<JsonObject> Objects(JsonNode? manifest, string key)
    {
        if (manifest is not JsonObject obj || !obj.TryGetPropertyValue(key, out var value) || value is not JsonArray array)
        {
            yield break;
        }

        foreach (var entry in array.OfType<JsonObject>())
        {
            yield return entry;
        }
    }

    /// <summary>A scalar property rendered as the text a report prints, or null when the manifest does not carry it.</summary>
    public static string? Scalar(JsonNode? manifest, string key) =>
        manifest is JsonObject obj && obj.TryGetPropertyValue(key, out var value) && value is JsonValue
            ? value.ToJsonString().Trim('"')
            : null;
}
