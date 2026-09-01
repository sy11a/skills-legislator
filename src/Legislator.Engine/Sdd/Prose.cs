using System.Text.RegularExpressions;

namespace Legislator.Engine.Sdd;

/// <summary>
/// Reading a markdown document the way `core/sdd.md` reads one: a token inside a fenced block
/// or inline backticks is quotation, not content (BL-057). Two scanners disagreeing about that
/// is how a document explaining a rule comes to be judged as breaking it.
/// </summary>
public static partial class Prose
{
    /// <summary>`per R-NNN` and the list form `per R-001, R-002` — the first plan written under this law used the list, so the reference form admits it.</summary>
    [GeneratedRegex(@"\bper (R-\d{3}(?:,\s*R-\d{3})*)\b")]
    private static partial Regex PerRef();

    [GeneratedRegex(@"R-\d{3}")]
    private static partial Regex RequirementId();

    [GeneratedRegex(@"`[^`\n]*`")]
    private static partial Regex InlineCode();

    /// <summary>Fenced blocks blanked, inline code kept — a definition line lawfully backticks the paths it binds.</summary>
    public static string BlankFences(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var kept = new List<string>();
        var fenced = false;
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                kept.Add("");
            }
            else
            {
                kept.Add(fenced ? "" : line);
            }
        }

        return string.Join('\n', kept);
    }

    /// <summary>Markdown with fenced blocks AND inline code blanked: the form the shape lints judge.</summary>
    public static string ProseOnly(string text) =>
        string.Join('\n', BlankFences(text).Split('\n').Select(line => InlineCode().Replace(line, "")));

    /// <summary>Every R-NNN referenced by a `per ...` in the text.</summary>
    public static IReadOnlySet<string> PerRefs(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in PerRef().Matches(text).Cast<Match>())
        {
            foreach (var id in RequirementId().Matches(match.Groups[1].Value).Cast<Match>())
            {
                ids.Add(id.Value);
            }
        }

        return ids;
    }
}
