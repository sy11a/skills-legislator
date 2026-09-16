using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Text;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Renders the three views — CHANGELOG.md, docs/okf/log.md, and docs/journal/YYYY-MM-DD.md —
/// from change fragments under docs/changes/. A task branch adds a fragment and never edits
/// the views. Idempotent by case key: a fragment whose case key already appears in the view
/// is not inserted twice. Pre-fragment content survives byte-for-byte.
/// </summary>
public sealed partial class RenderJob : IJob
{
    private static readonly string[] KindOrder = ["Added", "Changed", "Fixed", "Removed"];

    public string Name => "render";

    public string Usage => Name;

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var findings = new List<string>();
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var fs = ctx.Fs;

        var (isTaskBranch, reason) = IsTaskBranch(ctx.Proc, ctx.Options, ctx.Root);
        if (isTaskBranch)
        {
            return new JobResult(3, "", $"legislator render: refused — {reason}\n");
        }

        var changesDir = layout.Changes;
        if (!fs.Directory.Exists(changesDir))
        {
            return new JobResult(0, "", "");
        }

        var fragments = LoadFragments(fs, changesDir, layout, findings);
        if (findings.Count > 0)
        {
            return Findings.AsResult(findings);
        }

        var existingCases = ReadRenderedCases(fs, layout);

        var newFragments = fragments
            .Where(f => !existingCases.Contains(f.Case))
            .OrderBy(f => f.Date, Comparer<DateOnly>.Default)
            .ThenBy(f => f.Case, StringComparer.Ordinal)
            .ToList();

        if (newFragments.Count == 0)
        {
            return new JobResult(0, "", "");
        }

        RenderChangelog(fs, layout, newFragments);
        RenderOkfLog(fs, layout, newFragments);
        RenderJournal(fs, layout, newFragments);

        return new JobResult(0, "", "");
    }

    public static (bool IsTask, string? Reason) IsTaskBranch(IProcessRunner proc, LegislatorOptions options, string root)
    {
        var (branch, available) = GitLog.Ask(proc, options, root, "rev-parse", "--abbrev-ref", "HEAD");

        if (!available || branch is null || branch == "HEAD" || branch.StartsWith('('))
        {
            return (false, null);
        }

        var defaultBranches = options.ConventionalDefaultBranches.Value;
        foreach (var def in defaultBranches)
        {
            if (branch == def)
            {
                return (false, null);
            }
        }

        return (true, $"current branch '{branch}' is a task branch — render must run on the default branch (core/changelog.md)");
    }

    internal static List<ChangeFragment> LoadFragments(
        IFileSystem fs, string changesDir, RepoLayout layout, List<string> findings)
    {
        var fragments = new List<ChangeFragment>();
        foreach (var entry in fs.Directory.EnumerateFiles(changesDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var relative = layout.Relative(entry);
            var text = fs.File.ReadAllText(entry);
            if (ParseFrontMatter(text) is not var (caseKey, issue, kind, date))
            {
                findings.Add($"{relative}: malformed fragment — no YAML front matter with case, issue, kind and date");
                continue;
            }

            if (string.IsNullOrWhiteSpace(caseKey))
            {
                findings.Add($"{relative}: malformed fragment — missing 'case' in front matter");
                continue;
            }

            if (string.IsNullOrWhiteSpace(issue))
            {
                findings.Add($"{relative}: malformed fragment — missing 'issue' in front matter");
                continue;
            }

            if (!KindOrder.Contains(kind, StringComparer.Ordinal))
            {
                findings.Add($"{relative}: malformed fragment — 'kind' must be Added, Changed, Fixed or Removed, not '{kind}'");
                continue;
            }

            if (date == default)
            {
                findings.Add($"{relative}: malformed fragment — missing or unparseable 'date' in front matter");
                continue;
            }

            var sections = ParseSections(text);
            if (!sections.TryGetValue("changelog", out var changelog) || !sections.TryGetValue("okf-log", out var okfLog) || !sections.TryGetValue("journal", out var journal))
            {
                findings.Add($"{relative}: malformed fragment — missing one or more of ## changelog, ## okf-log, ## journal sections");
                continue;
            }

            fragments.Add(new ChangeFragment(
                caseKey.Trim(), issue.Trim(), kind.Trim(), date, changelog.Trim(), okfLog.Trim(), journal.Trim()));
        }

        return fragments;
    }

    /// <summary>
    /// Returns every case key already present in rendered views — the idempotency base.
    /// </summary>
    public static HashSet<string> ReadRenderedCases(IFileSystem fs, RepoLayout layout)
    {
        var cases = new HashSet<string>(StringComparer.Ordinal);
        if (fs.File.Exists(layout.Changelog))
        {
            cases.UnionWith(RenderedMarkers().Matches(fs.File.ReadAllText(layout.Changelog))
                .Select(m => m.Groups[1].Value));
        }

        var okfLog = $"{layout.Okf}/log.md";
        if (fs.File.Exists(okfLog))
        {
            cases.UnionWith(RenderedMarkers().Matches(fs.File.ReadAllText(okfLog))
                .Select(m => m.Groups[1].Value));
        }

        if (fs.Directory.Exists(layout.Journal))
        {
            foreach (var entry in fs.Directory.EnumerateFiles(layout.Journal, "20??????.md", SearchOption.TopDirectoryOnly))
            {
                cases.UnionWith(RenderedMarkers().Matches(fs.File.ReadAllText(entry))
                    .Select(m => m.Groups[1].Value));
            }
        }

        return cases;
    }

    internal static void RenderChangelog(IFileSystem fs, RepoLayout layout, List<ChangeFragment> fragments)
    {
        var path = layout.Changelog;
        var text = fs.File.Exists(path) ? fs.File.ReadAllText(path) : "# Changelog\n\nAll notable changes to this project are documented here.\n";

        foreach (var kind in KindOrder)
        {
            var ofKind = fragments.Where(f => f.Kind == kind).OrderBy(f => f.Date).ThenBy(f => f.Case, StringComparer.Ordinal).ToList();
            if (ofKind.Count == 0)
            {
                continue;
            }

            var heading = $"### {kind}";
            var insert = new StringBuilder();
            insert.AppendLine(heading);
            foreach (var f in ofKind)
            {
                insert.AppendLine(FormatMarker(f.Case));
                insert.AppendLine(f.Changelog.TrimEnd('\n'));
            }

            // Replace the heading line followed by its blank separator line
            text = text.Replace($"{heading}\n\n", insert.ToString());
        }

        fs.File.WriteAllText(path, text);
    }

    internal static void RenderOkfLog(IFileSystem fs, RepoLayout layout, List<ChangeFragment> fragments)
    {
        var path = $"{layout.Okf}/log.md";
        var text = fs.File.Exists(path) ? fs.File.ReadAllText(path) : "# OKF Log\n\n";

        var byDate = fragments
            .GroupBy(f => f.Date)
            .OrderBy(g => g.Key);

        foreach (var group in byDate)
        {
            foreach (var f in group.OrderBy(f => f.Case, StringComparer.Ordinal))
            {
                text += $"\n## {group.Key:yyyy-MM-dd}\n{FormatMarker(f.Case)}\n{f.OkfLog.TrimEnd()}";
                break;
            }

            foreach (var f in group.OrderBy(f => f.Case, StringComparer.Ordinal).Skip(1))
            {
                text += $"\n{FormatMarker(f.Case)}\n{f.OkfLog.TrimEnd()}";
            }

            text += "\n";
        }

        fs.File.WriteAllText(path, text);
    }

    internal static void RenderJournal(IFileSystem fs, RepoLayout layout, List<ChangeFragment> fragments)
    {
        var byDate = fragments.GroupBy(f => f.Date).OrderBy(g => g.Key);
        foreach (var group in byDate)
        {
            var path = $"{layout.Journal}/{group.Key:yyyy-MM-dd}.md";
            var text = fs.File.Exists(path) ? fs.File.ReadAllText(path).TrimEnd('\n') : "";

            if (text != "")
            {
                text += "\n\n";
            }

            foreach (var f in group.OrderBy(f => f.Case, StringComparer.Ordinal))
            {
                text += $"{FormatMarker(f.Case)}\n{f.Journal.TrimEnd('\n')}\n";
            }

            fs.File.WriteAllText(path, text);
        }
    }

    private static string FormatMarker(string caseKey) => $"<!-- rendered: {caseKey} -->";

    [GeneratedRegex(@"<!--\s*rendered:\s*(\S+)\s*-->")]
    internal static partial Regex RenderedMarkers();

    /// <summary>
    /// The YAML front matter as raw fields, or null when the document has none or its keys are absent.
    /// </summary>
    private static (string Case, string Issue, string Kind, DateOnly Date)? ParseFrontMatter(string text)
    {
        var (fm, start, end) = FrontMatterSpan(text);
        if (fm is null)
        {
            return null;
        }

        var caseKey = FieldValue(fm, "case");
        var issue = FieldValue(fm, "issue");
        var kind = FieldValue(fm, "kind");
        var dateStr = FieldValue(fm, "date");

        if (string.IsNullOrEmpty(caseKey) || string.IsNullOrEmpty(issue) || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(dateStr))
        {
            return (caseKey, issue, kind, default);
        }

        var date = DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed : default;

        return (caseKey, issue, kind, date);
    }

    /// <summary>Reads a key: value line from YAML front matter.</summary>
    private static string FieldValue(string fm, string key)
    {
        foreach (var line in fm.Split('\n'))
        {
            var col = line.IndexOf(':');
            if (col > 0 && string.Equals(line[..col].Trim(), key, StringComparison.Ordinal))
            {
                return line[(col + 1)..].Trim();
            }
        }

        return "";
    }

    /// <summary>
    /// The body after the front matter, split into sections by ## heading. Keys are normalized to lowercase.
    /// </summary>
    private static Dictionary<string, string> ParseSections(string text)
    {
        var (_, start, end) = FrontMatterSpan(text);
        if (start < 0)
        {
            return [];
        }

        var body = text[(end + 1)..];
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in SectionRegex().Matches(body).Cast<Match>())
        {
            var heading = match.Groups[1].Value.Trim().ToLowerInvariant();
            var content = match.Groups[2].Value;
            sections[heading] = content;
        }

        return sections;
    }

    [GeneratedRegex(@"^##\s+(\S[^\n]*)\n((?:(?!^##\s+).*\n?)*)", RegexOptions.Multiline)]
    private static partial Regex SectionRegex();

    /// <summary>
    /// The YAML front matter span: (text between --- markers, line index of opening ---, line index of closing ---).
    /// Returns (-1,-1,-1) when the document has no front matter.
    /// </summary>
    private static (string? Fm, int Start, int End) FrontMatterSpan(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        if (lines.Length < 2 || lines[0].Trim() != "---")
        {
            return (null, -1, -1);
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                return (string.Join('\n', lines[1..i]), 0, i);
            }
        }

        return (null, -1, -1);
    }
}

/// <summary>
/// One change fragment as parsed from docs/changes/*.md.
/// </summary>
internal sealed record ChangeFragment(
    string Case, string Issue, string Kind, DateOnly Date,
    string Changelog, string OkfLog, string Journal);