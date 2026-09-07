using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Legislator.Core.Options;

namespace Legislator.Engine.Detect;

/// <summary>
/// Which stacks a repository looks like it is, from what is on disk alone - Step 2's signals.
/// A candidate is an offer the run makes to the user, never a subscription: nothing here writes
/// a stack into a manifest. Every signal is an option, so a project laying its files out
/// differently is configured rather than patched.
/// </summary>
public static class StackCandidates
{
    private const string Dependencies = "dependencies";

    private const string DevDependencies = "devDependencies";

    /// <summary>The candidates in signal order - .NET first, then the front end, as the report prints them.</summary>
    public static IReadOnlyList<string> Of(IFileSystem fs, string root, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(options);

        var found = new List<string>();
        if (options.DotnetProjectPatterns.Value.Any(pattern => AnyFile(fs, root, pattern)))
        {
            found.Add(options.DotnetStack.Value);
        }

        if (IsAurelia(fs, root, options))
        {
            found.Add(options.AureliaStack.Value);
        }

        return found;
    }

    private static bool IsAurelia(IFileSystem fs, string root, LegislatorOptions options)
    {
        var stack = options.AureliaStack.Value;
        if (fs.File.Exists($"{root}/{options.AureliaMarkerFile.Value}"))
        {
            return true;
        }

        var package = $"{root}/{options.NodePackageFile.Value}";
        if (!fs.File.Exists(package))
        {
            return false;
        }

        JsonNode? manifest;
        try
        {
            manifest = JsonNode.Parse(fs.File.ReadAllText(package));
        }
        catch (JsonException)
        {
            return false; // a package manifest that does not parse is no signal, not a crash
        }

        return Names(manifest, Dependencies).Concat(Names(manifest, DevDependencies))
            .Any(name => name == stack || name.StartsWith($"{stack}-", StringComparison.Ordinal));
    }

    private static IEnumerable<string> Names(JsonNode? manifest, string section) =>
        manifest is JsonObject obj && obj.TryGetPropertyValue(section, out var value) && value is JsonObject deps
            ? deps.Select(pair => pair.Key)
            : [];

    private static bool AnyFile(IFileSystem fs, string root, string pattern)
    {
        try
        {
            return fs.Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).Any();
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
