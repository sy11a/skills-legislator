using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;

namespace Legislator.Engine.Runs;

/// <summary>
/// The tier split a package's <c>AGENTS.md.tpl</c> declares for its core rules and for
/// <c>docs/okf/codebase-map.md</c> (BL-484 R-001, Q9, Q11). The template is the single source:
/// a core rule path on an <c>@docs/ai/rules/core/&lt;name&gt;.md</c> line is always-tier; a core
/// rule path (or the codebase map) on any other line is on-demand, and the line itself is the
/// pointer text the report emits verbatim. A path the template names into nothing is an
/// always-tier rule the report treats as "add the import" (the today branch).
///
/// The template's paths are written against the default layout (<c>docs/ai/rules/core/...</c>
/// and <c>docs/okf/codebase-map.md</c>); a repo that renamed a directory under <c>RulesDir</c>
/// or <c>OkfDir</c> still matches because the helper folds each template line's path back to
/// the default layout and re-roots owned paths through the actual layout before comparing.
/// </summary>
public static partial class TemplateTiers
{
    /// <summary>Matches an <c>@docs/ai/rules/core/&lt;name&gt;.md</c> import line - the always-tier signal. The prefix is built into the regex because the prefix is part of the template protocol, not a per-repo setting; the per-repo `RulesCore` path is matched separately against the captured name.</summary>
    [GeneratedRegex(@"^@docs/ai/rules/core/(\S+)$", RegexOptions.Multiline)]
    private static partial Regex AlwaysImport();

    /// <summary>The tier split for a package and a repository layout, plus a lookup that returns the template line naming a path on a non-@import line.</summary>
    /// <exception cref="AgentsTemplateMissingException">the template file is absent at the path the package's options name.</exception>
    public static TierModel Read(IFileSystem fs, SkillPackage skill, RepoLayout layout, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        var path = $"{skill.Root.TrimEnd('/')}/{options.SkillAgentsTemplate.Value}";
        if (!fs.File.Exists(path))
        {
            throw new AgentsTemplateMissingException(path);
        }

        var text = fs.File.ReadAllText(path);
        var coreRelative = layout.Relative(layout.RulesCore);
        var okfRelative = layout.Relative(layout.Okf);
        var codeBaseFile = options.CodebaseMapFile.Value;
        var defaultCorePrefix = options.TemplateCorePrefix.Value;
        var defaultCodeBaseMapPath = options.TemplateCodebaseMapPath.Value;

        var always = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in AlwaysImport().Matches(text))
        {
            // The template writes against the default layout; turn the default-prefixed path
            // into "<repo>/<RulesCore>/<name>.md" before comparing against an owned path.
            always.Add($"{coreRelative}/{match.Groups[1].Value}");
        }

        // The codebase map is on-demand by case rule (Q3): even when the template carries it as
        // an `@import`, the report's remove-half fires for it; even when the template carries it
        // only as a pointer, the add-half does. Listing it in `always` would skip the remove.
        always.Remove($"{okfRelative}/{codeBaseFile}");

        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var onDemandLine = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith('@'))
            {
                continue;
            }

            // A pointer line in the template names a path in either of the two default forms.
            // For each, re-root it through the actual repo layout so a non-default RulesDir or
            // OkfDir still matches an owned path.
            var indexed = FoldToOwned(line, defaultCorePrefix, $"{coreRelative}/");
            if (indexed is not null)
            {
                onDemandLine[indexed.Value.Owned] = SubstitutePath(line, defaultCorePrefix, $"{coreRelative}/");
                continue;
            }

            if (line.Contains(defaultCodeBaseMapPath, StringComparison.Ordinal))
            {
                onDemandLine[$"{okfRelative}/{codeBaseFile}"] =
                    SubstitutePath(line, defaultCodeBaseMapPath, $"{okfRelative}/{codeBaseFile}");
            }
        }

        return new TierModel(always, onDemandLine);
    }

    /// <summary>The owned path that the template line's default-layout path is equivalent to, or null when the line names no path the helper tracks.</summary>
    private static (string Owned, string Replaced)? FoldToOwned(
        string line, string defaultPrefix, string ownedPrefix)
    {
        var at = line.IndexOf(defaultPrefix, StringComparison.Ordinal);
        if (at < 0)
        {
            return null;
        }

        // Walk to the next whitespace or backtick so we cut at the end of the path, not at
        // every character that happens to look like the prefix (a prose mention of the prefix
        // followed by another path needs no special handling: the next path starts with a
        // different prefix).
        var tail = line[(at + defaultPrefix.Length)..];
        var end = tail.Length;
        for (var i = 0; i < tail.Length; i++)
        {
            if (char.IsWhiteSpace(tail[i]) || tail[i] == '`')
            {
                end = i;
                break;
            }
        }

        var defaultPath = defaultPrefix + tail[..end];
        var ownedPath = ownedPrefix + tail[..end];
        return (ownedPath, defaultPath);
    }

    /// <summary>Re-root every occurrence of <paramref name="defaultPath"/> in <paramref name="line"/> to <paramref name="ownedPath"/>, leaving the rest of the line untouched.</summary>
    private static string SubstitutePath(string line, string defaultPath, string ownedPath) =>
        line.Replace(defaultPath, ownedPath, StringComparison.Ordinal);

    /// <summary>The two halves of the tier split a template declares. <see cref="Always"/> names the core rules whose template line was an <c>@import</c>; <see cref="OnDemandLine"/> maps every on-demand owned path to the verbatim template text the report's add-half proposes.</summary>
    public sealed record TierModel(IReadOnlySet<string> Always, IReadOnlyDictionary<string, string> OnDemandLine);
}