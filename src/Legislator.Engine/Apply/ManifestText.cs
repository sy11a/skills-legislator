using System.Text;
using Legislator.Core.Manifest;

namespace Legislator.Engine.Apply;

/// <summary>
/// Step 3.7's pinned serialization. The manifest is written by hand rather than by a serializer
/// because its layout is part of its contract: one owned path per line, one keep entry per
/// line, in that order - so a diff of two runs shows which file joined the constitution, not a
/// reflowed blob. Byte-stable by construction, which is what makes a second apply a no-op.
/// </summary>
public static class ManifestText
{
    public static string Render(
        string version,
        IReadOnlyList<string> stacks,
        IReadOnlyList<KeepEntry> keep,
        IReadOnlyList<string> owned)
    {
        ArgumentNullException.ThrowIfNull(stacks);
        ArgumentNullException.ThrowIfNull(keep);
        ArgumentNullException.ThrowIfNull(owned);

        var text = new StringBuilder();
        text.Append("{\n");
        text.Append($"  \"{ManifestFile.VersionKey}\": {version},\n");
        text.Append($"  \"{ManifestFile.StacksKey}\": [{string.Join(", ", stacks.Select(Quoted))}],\n");
        if (keep.Count > 0)
        {
            text.Append($"  \"{ManifestFile.KeepKey}\": [\n");
            text.Append(string.Join(",\n", keep.OrderBy(k => k.Path, StringComparer.Ordinal).Select(k =>
                $"    {{\"{ManifestFile.PathField}\": {Quoted(k.Path)}, \"{ManifestFile.ReasonField}\": {Quoted(k.Reason)}}}")));
            text.Append("\n  ],\n");
        }
        else
        {
            text.Append($"  \"{ManifestFile.KeepKey}\": [],\n");
        }

        text.Append($"  \"{ManifestFile.OwnedFilesKey}\": [\n");
        text.Append(string.Join(",\n", owned.Order(StringComparer.Ordinal).Select(o => $"    {Quoted(o)}")));
        text.Append("\n  ]\n}\n");
        return text.ToString();
    }

    /// <summary>A JSON string as this file writes them - the two characters JSON must escape, and nothing else, because these are paths and reasons a person typed.</summary>
    private static string Quoted(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
