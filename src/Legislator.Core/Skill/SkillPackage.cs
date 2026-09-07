using System.IO.Abstractions;
using System.Text.Json;
using Legislator.Core.Options;

namespace Legislator.Core.Skill;

/// <summary>
/// The legislator package a run was pointed at with `--skill`: the source side of every
/// comparison the constitution makes about itself. It is addressed by the path the caller gave,
/// never derived - a repository mid-upgrade may carry an older delivered engine, so the package
/// the run reads is always the one the caller named.
/// </summary>
public sealed class SkillPackage(IFileSystem fs, LegislatorOptions options, string root)
{
    private readonly IFileSystem fs = fs;
    private readonly LegislatorOptions options = options;

    public string Root { get; } = root;

    /// <summary>The edition this package ships, or null when it carries no VERSION at all.</summary>
    public string? Version
    {
        get
        {
            var path = $"{Root.TrimEnd('/')}/{options.SkillVersionFile.Value}";
            return fs.File.Exists(path) ? fs.File.ReadAllText(path).Trim() : null;
        }
    }

    /// <summary>
    /// What this edition released, as the tag-time record ships it inside the package
    /// (operator ruling 2026-09-04). It lives here rather than in a legislated repo because
    /// `audit` already takes `--skill` and a fleet member has no `evals/benchmarks/` of its
    /// own: one source, read where it lies, exactly as the rules are. An EMPTY digest map is
    /// the honest state of an edition that has not been tagged yet - the record carries the
    /// number the moment it is assigned, and CI fills the digests when the tag builds them.
    /// </summary>
    public (string? Edition, IReadOnlyDictionary<string, string> Digests) Release
    {
        get
        {
            var path = $"{Root.TrimEnd('/')}/{options.SkillReleaseFile.Value}";
            if (!fs.File.Exists(path))
            {
                return (null, new Dictionary<string, string>(StringComparer.Ordinal));
            }

            try
            {
                using var doc = JsonDocument.Parse(fs.File.ReadAllText(path));
                var edition = doc.RootElement.TryGetProperty("edition", out var e) ? e.GetString() : null;
                var digests = new Dictionary<string, string>(StringComparer.Ordinal);
                if (doc.RootElement.TryGetProperty("digests", out var d) && d.ValueKind == JsonValueKind.Object)
                {
                    foreach (var rid in d.EnumerateObject())
                    {
                        digests[rid.Name] = rid.Value.GetString() ?? "";
                    }
                }

                return (edition, digests);
            }
            catch (JsonException)
            {
                return (null, new Dictionary<string, string>(StringComparer.Ordinal));
            }
        }
    }
}
