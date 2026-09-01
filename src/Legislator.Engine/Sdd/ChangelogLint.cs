using System.IO.Abstractions;
using Legislator.Core.Repo;

namespace Legislator.Engine.Sdd;

/// <summary>The changelog keeps its Keep-a-Changelog structure (`core/changelog.md`): the section a task's completion writes into must be there to write into.</summary>
public static class ChangelogLint
{
    public static IEnumerable<string> Findings(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        if (!fs.File.Exists(layout.Changelog)
            || fs.File.ReadAllText(layout.Changelog).Contains("## [Unreleased]", StringComparison.Ordinal))
        {
            yield break;
        }

        yield return $"{layout.Changelog[(layout.Root.Length + 1)..]}: no ## [Unreleased] section → keep the Keep-a-Changelog structure per core/changelog.md";
    }
}
