using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Jobs;

namespace Legislator.Engine.Sdd;

/// <summary>
/// Fragment shape checks for the sdd-lint job: front matter present, kind in the closed set,
/// case matching the current branch, exactly one changelog bullet, and a journal section that
/// is present. This replaces what sdd-lint used to check in the [Unreleased] section by hand —
/// a branch adds a fragment and never edits the three views (core/changelog.md).
///
/// The one-bullet rule is the only machine-checkable half of the composition law: a case is one
/// changelog line pointing at its summary, and a second bullet says the case was two cases. The
/// journal section's *content* — dead ends, open questions, decisions — is not checkable and is
/// deliberately not pretended to be; only its presence is, so a day file cannot go silent.
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

            var bullets = ChangelogBullets(text);
            if (bullets is null)
            {
                yield return $"{relative}: no '## changelog' section → a fragment carries the one line its case adds to [Unreleased] per core/changelog.md";
            }
            else if (bullets.Value != 1)
            {
                yield return $"{relative}: '## changelog' carries {bullets.Value} bullets, not one → one case is one changelog line pointing at its summary; a second bullet says the case was two cases (core/changelog.md)";
            }

            if (!HasSection(text, "journal"))
            {
                yield return $"{relative}: no '## journal' section → carry the dead ends, open questions and decisions, or one line saying there were none (core/dev-journal.md)";
            }
        }
    }

    /// <summary>
    /// The number of top-level '-' bullets under '## changelog', or null when the section is
    /// absent. A bullet is a line whose first non-space character is '-' at column zero —
    /// continuation lines and nested bullets are indented and do not count.
    /// </summary>
    internal static int? ChangelogBullets(string text)
    {
        var body = Section(text, "changelog");
        if (body is null)
        {
            return null;
        }

        var count = 0;
        foreach (var line in body)
        {
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Whether the fragment carries the named '## ' section at all.</summary>
    internal static bool HasSection(string text, string name) => Section(text, name) is not null;

    /// <summary>
    /// The lines of the named '## ' section, up to the next '## ' heading or the end of the
    /// file; null when the section is absent. Fenced code blocks are skipped so that an example
    /// fragment inside a fence cannot be read as the fragment's own sections.
    /// </summary>
    private static List<string>? Section(string text, string name)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        List<string>? body = null;
        var fenced = false;
        foreach (var line in lines)
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                continue;
            }

            if (fenced)
            {
                // an example fragment inside a fence is illustration, not this fragment's
                // own sections — its bullets and headings must not be counted
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (body is not null)
                {
                    return body;
                }

                if (string.Equals(line[3..].Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    body = [];
                }

                continue;
            }

            body?.Add(line);
        }

        return body;
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
    /// A fragment's case key matches the branch when the branch names that case. Two forms
    /// count, because the fleet writes both: the key verbatim somewhere in the name, and the
    /// key with its separator written as the branch's path separator — case <c>BL-347</c> on
    /// branch <c>bl/347-retelling-layer</c>, case <c>L-3</c> on <c>l/3-change-fragments</c>.
    ///
    /// The second form is the one every real branch uses and the one the original check could
    /// not see: <c>"bl/347-x".Contains("BL-347")</c> is false, so the check fired on every
    /// correct fragment, including this repository's own <c>L-3</c>. A lint that fires on
    /// correct work teaches its reader to ignore it (sy11a/Architector#395 reports the same
    /// class in the delivered Python engine).
    ///
    /// The number must end where the key's number ends, so <c>BL-347</c> does not match
    /// <c>bl/3470-other</c>.
    /// </summary>
    internal static bool BranchMatchesCase(string branch, string caseKey)
    {
        var key = caseKey.Trim();
        if (key.Length == 0)
        {
            return true;
        }

        if (branch.Contains(key, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var dash = key.LastIndexOf('-');
        if (dash <= 0 || dash == key.Length - 1)
        {
            return false;
        }

        var slashed = string.Concat(key.AsSpan(0, dash), "/", key.AsSpan(dash + 1));
        if (!branch.StartsWith(slashed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return branch.Length == slashed.Length || !char.IsAsciiDigit(branch[slashed.Length]);
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