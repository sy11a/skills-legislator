using System.Globalization;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;

namespace Legislator.Engine.Sdd;

/// <summary>The ADR shape `core/adr.md` states: the file name, the four sections, the closed status set, and a gapless sequence - never renumbered, so a gap is a record that was deleted.</summary>
public static partial class AdrLint
{
    private static readonly string[] Sections = ["Status", "Context", "Decision", "Consequences"];

    private static readonly HashSet<string> Statuses = new(StringComparer.Ordinal) { "proposed", "accepted", "deprecated" };

    [GeneratedRegex(@"^(\d{4})-[a-z0-9][a-z0-9-]*\.md$")]
    private static partial Regex Name();

    [GeneratedRegex(@"^## Status\s+(\S[^\n]*)", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex StatusLine();

    public static IEnumerable<string> Findings(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        if (!fs.Directory.Exists(layout.Adr))
        {
            yield break;
        }

        var numbers = new List<int>();
        foreach (var document in fs.Directory.EnumerateFiles(layout.Adr, "*.md")
                     .Select(d => d.Replace('\\', '/')).Order(StringComparer.Ordinal))
        {
            var name = document[(document.LastIndexOf('/') + 1)..];
            if (name == "template.md")
            {
                continue;
            }

            var relative = document[(layout.Root.Length + 1)..];
            var named = Name().Match(name);
            if (!named.Success)
            {
                yield return $"{relative}: ADR filename is not NNNN-kebab-title.md → rename it per core/adr.md";
                continue;
            }

            numbers.Add(int.Parse(named.Groups[1].Value, CultureInfo.InvariantCulture));
            var text = Prose.ProseOnly(fs.File.ReadAllText(document));
            foreach (var section in Sections)
            {
                if (!Regex.IsMatch(text, $@"^## {section}\b", RegexOptions.Multiline))
                {
                    yield return $"{relative}: no ## {section} section → use docs/adr/template.md's shape";
                }
            }

            var status = StatusLine().Match(text);
            if (status.Success)
            {
                var value = status.Groups[1].Value.Trim().ToLowerInvariant();
                if (!Statuses.Contains(value) && !value.StartsWith("superseded by ", StringComparison.Ordinal))
                {
                    yield return $"{relative}: status '{value}' outside the closed set → proposed/accepted/deprecated/superseded by NNNN";
                }
            }
        }

        if (numbers.Count > 0)
        {
            foreach (var missing in Enumerable.Range(1, numbers.Max()).Except(numbers).Order())
            {
                yield return $"{layout.Adr[(layout.Root.Length + 1)..]}/: sequence gap — {missing:0000} is missing → ADRs are numbered gaplessly, never renumbered";
            }
        }
    }
}
