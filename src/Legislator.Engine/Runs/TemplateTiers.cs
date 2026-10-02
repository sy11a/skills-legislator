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

        // Normalise line endings first so the always-tier regex (which anchors on $) does not
        // desynchronise against a CRLF file (f7). $ in RegexOptions.Multiline matches the position
        // immediately before \n, never before \r\n on its own.
        var text = fs.File.ReadAllText(path).ReplaceLineEndings("\n");
        var coreRelative = layout.Relative(layout.RulesCore);
        var okfRelative = layout.Relative(layout.Okf);
        var codeBaseFile = options.CodebaseMapFile.Value;
        var defaultCorePrefix = options.TemplateCorePrefix.Value;
        var defaultCodeBaseMapPath = options.TemplateCodebaseMapPath.Value;

        // The always-tier signal is an `@<default-core-prefix><name>` line; the prefix comes from
        // options (BL-484 Q11, R-001) so the regex is built at runtime, not generated from a
        // source-time literal.
        var alwaysImportRegex = new Regex(
            @"^@" + Regex.Escape(defaultCorePrefix) + @"(\S+)$",
            RegexOptions.Multiline);

        var always = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in alwaysImportRegex.Matches(text))
        {
            // The template writes against the default layout; turn the default-prefixed path
            // into "<repo>/<RulesCore>/<name>.md" before comparing against an owned path.
            always.Add($"{coreRelative}/{match.Groups[1].Value}");
        }

        var lines = text.Split('\n');
        var onDemandLine = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith('@'))
            {
                continue;
            }

            // A pointer line in the template names a path in either of the two default forms.
            // For each, re-root it through the actual repo layout so a non-default RulesDir or
            // OkfDir still matches an owned path. Multiple occurrences on one line each map to
            // that same line - f3 lifts the once-per-line cap that lost a documented second path.
            foreach (var (owned, _) in FoldToOwned(line, defaultCorePrefix, $"{coreRelative}/"))
            {
                onDemandLine[owned] = SubstitutePath(line, defaultCorePrefix, $"{coreRelative}/");
            }

            foreach (var (owned, _) in FoldToOwned(line, defaultCodeBaseMapPath, $"{okfRelative}/{codeBaseFile}"))
            {
                onDemandLine[owned] = SubstitutePath(line, defaultCodeBaseMapPath, $"{okfRelative}/{codeBaseFile}");
            }
        }

        return new TierModel(always, onDemandLine);
    }

    /// <summary>Every owned path the template line names at its default-layout prefix, or empty when the line names no path the helper tracks. Multiple occurrences on one line each yield (f3); the path itself is matched as a run of word chars, dashes, dots and slashes ending at <c>.md</c>, so a trailing period or comma after the path is not absorbed into it (f9). The prefix may be either a directory (<c>docs/ai/rules/core/</c>) or a full file (<c>docs/okf/codebase-map.md</c>); the algorithm finds the longest path starting at the prefix that ends at <c>.md</c>.</summary>
    [GeneratedRegex(@"[\w\./-]+\.md")]
    private static partial Regex PathRun();

    private static IEnumerable<(string Owned, string Default)> FoldToOwned(
        string line, string defaultPrefix, string ownedPrefix)
    {
        var at = 0;
        while ((at = line.IndexOf(defaultPrefix, at, StringComparison.Ordinal)) >= 0)
        {
            // The prefix may be a directory (e.g. `docs/ai/rules/core/`) or a full file
            // (e.g. `docs/okf/codebase-map.md`). Match a `.md` path beginning at `at`; that
            // includes the prefix's own bytes when the prefix is a directory and extends past
            // them when the prefix is itself the path. Either way, the match is the default-
            // layout path the report will re-root into the runtime layout.
            var match = PathRun().Match(line, at);
            if (!match.Success || match.Index != at)
            {
                at++;
                continue;
            }

            var defaultPath = match.Value;
            var ownedPath = ownedPrefix + defaultPath[defaultPrefix.Length..];
            yield return (ownedPath, defaultPath);
            at = match.Index + match.Length;
        }
    }

    /// <summary>Re-root every occurrence of <paramref name="defaultPath"/> in <paramref name="line"/> to <paramref name="ownedPath"/>, leaving the rest of the line untouched.</summary>
    private static string SubstitutePath(string line, string defaultPath, string ownedPath) =>
        line.Replace(defaultPath, ownedPath, StringComparison.Ordinal);

    /// <summary>The two halves of the tier split a template declares. <see cref="Always"/> names the core rules whose template line was an <c>@import</c>; <see cref="OnDemandLine"/> maps every on-demand owned path to the verbatim template text the report's add-half proposes.</summary>
    public sealed record TierModel(IReadOnlySet<string> Always, IReadOnlyDictionary<string, string> OnDemandLine);
}