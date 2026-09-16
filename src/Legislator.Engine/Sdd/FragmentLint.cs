using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Jobs;

namespace Legislator.Engine.Sdd;

/// <summary>
/// Fragment shape checks for the sdd-lint job: front matter present, kind in the closed set,
/// case matching the current branch. This replaces what sdd-lint used to check in the
/// [Unreleased] section by hand — a branch adds a fragment and never edits the three views
/// (core/changelog.md).
/// </summary>
public static class FragmentLint
{
    private static readonly string[] Kinds = ["Added", "Changed", "Fixed", "Removed"];

    public static IEnumerable<string> Findings(IFileSystem fs, RepoLayout layout, IProcessRunner proc, string root, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);

        var changesDir = layout.Changes;
        if (!fs.Directory.Exists(changesDir))
        {
            yield break;
        }

        var branch = TryGetBranch(proc, options, root);

        foreach (var entry in fs.Directory.EnumerateFiles(changesDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var relative = layout.Relative(entry);
            var text = fs.File.ReadAllText(entry);
            var fm = FrontMatter(text);
            if (fm is not var (caseKey, issue, kind, date))
            {
                yield return $"{relative}: no YAML front matter — declare case, issue, kind and date per core/changelog.md";
                continue;
            }

            if (string.IsNullOrWhiteSpace(caseKey))
            {
                yield return $"{relative}: no 'case' in front matter → state the case key per core/changelog.md";
                continue;
            }

            if (string.IsNullOrWhiteSpace(issue))
            {
                yield return $"{relative}: no 'issue' in front matter → state the tracker issue per core/changelog.md";
                continue;
            }

            if (string.IsNullOrWhiteSpace(kind) || !Kinds.Contains(kind, StringComparer.Ordinal))
            {
                yield return $"{relative}: kind '{kind}' is not in the closed set → use Added, Changed, Fixed or Removed per core/changelog.md";
                continue;
            }

            if (date == default)
            {
                yield return $"{relative}: date is missing or unparseable → state the date in YYYY-MM-DD form per core/changelog.md";
                continue;
            }

            if (branch is not null)
            {
                if (!BranchMatchesCase(branch, caseKey))
                {
                    yield return $"{relative}: case '{caseKey}' does not match current branch '{branch}' → a fragment belongs to the branch that writes it per core/changelog.md";
                }
            }
        }
    }

    /// <summary>
    /// The current branch name, or null when git is unavailable.
    /// </summary>
    private static string? TryGetBranch(IProcessRunner proc, LegislatorOptions options, string root)
    {
        var (branch, available) = GitLog.Ask(proc, options, root, "rev-parse", "--abbrev-ref", "HEAD");
        if (!available || branch is null || branch == "HEAD" || branch.StartsWith('('))
        {
            return null;
        }

        return branch;
    }

    /// <summary>
    /// A fragment's case key matches the branch when the branch name contains the case key
    /// after its convention prefix — bl/L-3-short is the third example, bl/NNN-kebab is the
    /// pattern declared in the options model, and a bare l/3-* is the Architector convention.
    /// </summary>
    internal static bool BranchMatchesCase(string branch, string caseKey)
    {
        var key = caseKey.Trim();
        return branch.Contains(key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The YAML front matter fields as parsed, or null when absent.</summary>
    internal static (string Case, string Issue, string Kind, DateOnly Date)? FrontMatter(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        if (lines.Length < 2 || lines[0].Trim() != "---")
        {
            return null;
        }

        string? caseKey = null;
        string? issue = null;
        string? kind = null;
        DateOnly date = default;
        var hasDate = false;

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                break;
            }

            var col = lines[i].IndexOf(':');
            if (col <= 0)
            {
                continue;
            }

            var key = lines[i][..col].Trim();
            var val = lines[i][(col + 1)..].Trim();
            switch (key)
            {
                case "case":
                    caseKey = val;
                    break;
                case "issue":
                    issue = val;
                    break;
                case "kind":
                    kind = val;
                    break;
                case "date":
                    hasDate = DateOnly.TryParse(val, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out date);
                    break;
            }
        }

        if (caseKey is null && issue is null && kind is null && !hasDate)
        {
            return null;
        }

        return (caseKey ?? "", issue ?? "", kind ?? "", hasDate ? date : default);
    }
}