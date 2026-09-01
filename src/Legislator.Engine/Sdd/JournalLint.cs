using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;

namespace Legislator.Engine.Sdd;

/// <summary>One file per working day, named for the day (`core/dev-journal.md`). The directory's own README is how-to, not a day.</summary>
public static partial class JournalLint
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}\.md$")]
    private static partial Regex DayName();

    public static IEnumerable<string> Findings(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        if (!fs.Directory.Exists(layout.Journal))
        {
            yield break;
        }

        foreach (var document in fs.Directory.EnumerateFiles(layout.Journal, "*.md")
                     .Select(d => d.Replace('\\', '/')).Order(StringComparer.Ordinal))
        {
            var name = document[(document.LastIndexOf('/') + 1)..];
            if (name == "README.md" || DayName().IsMatch(name))
            {
                continue;
            }

            yield return $"{document[(layout.Root.Length + 1)..]}: journal file is not YYYY-MM-DD.md → one file per working day, per core/dev-journal.md";
        }
    }
}
