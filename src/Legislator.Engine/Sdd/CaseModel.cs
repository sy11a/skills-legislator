using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;

namespace Legislator.Engine.Sdd;

/// <summary>
/// One case directory as the lint and the baseline read it (C-08). Ids are unique WITHIN a
/// case only - R-001 lawfully exists in many cases - so every consumer keys by the case and
/// the id together, never by the id alone.
/// </summary>
/// <param name="Path">The case directory, as the file system spells it.</param>
/// <param name="Relative">The repository-relative directory, as a finding prints it.</param>
/// <param name="Requirements">Requirement id to its definition text, from the spec's EARS definition lines.</param>
/// <param name="Converged">True when one of the case's documents carries the closing verdict.</param>
public sealed record CaseFile(
    string Path,
    string Relative,
    IReadOnlyDictionary<string, string> Requirements,
    bool Converged);

/// <summary>Loads every case under the cases directory, in ordinal order.</summary>
public static partial class CaseModel
{
    /// <summary>
    /// A definition line: the id, an em-dash, the requirement - in any of the three forms this
    /// repository's cases actually use. The em-dash after the id is the definition's signature;
    /// `per R-NNN` is always a reference.
    /// </summary>
    [GeneratedRegex(@"^(?:#{1,6}\s+|-\s+)?\*{0,2}(R-\d{3})\*{0,2}\s+— (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex Definition();

    /// <summary>
    /// Line-anchored: the marker closes a case only when a line STARTS with it (bold and a
    /// trailing date are lawful decoration). A spec QUOTING it mid-sentence must not read as a
    /// closure - a quotation is never the artifact (BL-057).
    /// </summary>
    [GeneratedRegex(@"^\s*(?:\*\*)?✅ Converged\b", RegexOptions.Multiline)]
    private static partial Regex ConvergedMarker();

    public static IReadOnlyList<CaseFile> Load(IFileSystem fs, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);

        if (!fs.Directory.Exists(layout.Cases))
        {
            return [];
        }

        return [.. fs.Directory.EnumerateDirectories(layout.Cases)
            .Select(directory => directory.Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .Select(directory => new CaseFile(
                directory,
                directory[(layout.Root.Length + 1)..],
                Requirements(fs, $"{directory}/spec.md"),
                IsConverged(fs, directory)))];
    }

    /// <summary>Every markdown document under the case, in ordinal order - what the lint walks.</summary>
    public static IReadOnlyList<string> Documents(IFileSystem fs, CaseFile file)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(file);

        return [.. fs.Directory.EnumerateFiles(file.Path, "*.md", SearchOption.AllDirectories)
            .Select(document => document.Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];
    }

    private static Dictionary<string, string> Requirements(IFileSystem fs, string spec)
    {
        if (!fs.File.Exists(spec))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var match in Definition().Matches(Prose.BlankFences(fs.File.ReadAllText(spec))).Cast<Match>())
        {
            found[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return found;
    }

    private static bool IsConverged(IFileSystem fs, string directory) =>
        fs.Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories).Any(document => Carries(fs, document));

    /// <summary>A document the process cannot read carries no verdict: a case is closed by a marker somebody wrote, never by a failure to look.</summary>
    private static bool Carries(IFileSystem fs, string document)
    {
        try
        {
            return ConvergedMarker().IsMatch(fs.File.ReadAllText(document));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
