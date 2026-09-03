using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;

namespace Legislator.Engine.Runs;

/// <summary>
/// The file targets of the package's Step-4 table. The table is the declaration of what a
/// legislated repository must have scaffolded, so the engine reads it rather than carrying a
/// second copy that could fall behind the procedure it mirrors. Apply snapshots the targets
/// before it writes and verify after, and the difference is what the report calls scaffolded.
/// </summary>
public static partial class Step4Targets
{
    /// <summary>The table's boundaries and the marker for a row that declares no file - law content, being the procedure document's own structure.</summary>
    private const string SectionStart = "## Step 4";

    private const string SectionEnd = "## Step 5";

    private const string NotAFile = "(empty";

    [GeneratedRegex(@"^\| `([^`]+)` \| ([^|]+) \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    public static IReadOnlyList<string> Of(IFileSystem fs, SkillPackage skill, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(options);

        var path = $"{skill.Root.TrimEnd('/')}/{options.SkillFile.Value}";
        if (!fs.File.Exists(path))
        {
            return [];
        }

        var text = fs.File.ReadAllText(path);
        var start = text.IndexOf(SectionStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return [];
        }

        var section = text[start..];
        var end = section.IndexOf(SectionEnd, StringComparison.Ordinal);
        if (end >= 0)
        {
            section = section[..end];
        }

        return [.. Row().Matches(section).Cast<Match>()
            .Where(m => !m.Groups[2].Value.TrimStart().StartsWith(NotAFile, StringComparison.Ordinal))
            .Select(m => m.Groups[1].Value)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Which of the targets are on disk right now - the snapshot apply takes before it writes and verify takes after.</summary>
    public static IReadOnlyDictionary<string, bool> Snapshot(
        IFileSystem fs, SkillPackage skill, LegislatorOptions options, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        return Of(fs, skill, options).ToDictionary(
            t => t,
            t => fs.File.Exists($"{layout.Root}/{t}") || fs.Directory.Exists($"{layout.Root}/{t}"),
            StringComparer.Ordinal);
    }
}
