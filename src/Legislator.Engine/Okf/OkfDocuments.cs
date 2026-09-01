using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;

namespace Legislator.Engine.Okf;

/// <summary>
/// Which documents of the OKF bundle carry anchors, and which of their lines are read - the
/// rule `core/okf.md` states and two jobs execute: `anchors` asks whether an anchor still
/// resolves, `okf-debt` asks whether its source moved on without it. The rule lives here once,
/// so the two can never disagree about what an anchored document is.
/// </summary>
public static partial class OkfDocuments
{
    [GeneratedRegex(@"^status:\s*(\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex Status();

    /// <summary>
    /// The anchored class: every markdown document of the bundle except the human class and
    /// anything flipped to <c>status: removed</c>, which the checklist tells the owner to keep
    /// and mark. The walk yields directories as well as files, as the Python's <c>rglob</c>
    /// does - a bundle entry that is not a readable document is a fault, not a silent skip.
    /// </summary>
    public static IEnumerable<string> Anchored(
        IFileSystem fs, RepoLayout layout, IReadOnlyList<string> humanClass)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(humanClass);

        if (!fs.Directory.Exists(layout.Okf))
        {
            return [];
        }

        return fs.Directory.EnumerateFileSystemEntries(layout.Okf, "*.md", SearchOption.AllDirectories)
            .Select(entry => entry.Replace('\\', '/'))
            .Where(entry => !humanClass.Contains(entry[(entry.LastIndexOf('/') + 1)..], StringComparer.Ordinal))
            .Where(entry => !IsRemoved(fs, entry))
            .Order(StringComparer.Ordinal);
    }

    /// <summary>The repository-relative, forward-slashed form a finding prints.</summary>
    public static string Relative(string root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(path);

        return path[(root.Length + 1)..].Replace('\\', '/');
    }

    /// <summary>Every line outside the front matter and outside fenced code blocks, numbered from one.</summary>
    public static IEnumerable<(int Number, string Line)> ScannableLines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lines = text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        var i = 0;
        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            i = 1;
            while (i < lines.Length && lines[i].Trim() != "---")
            {
                i++;
            }

            i++;
        }

        var fenced = false;
        for (; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
            }
            else if (!fenced)
            {
                yield return (i + 1, lines[i]);
            }
        }
    }

    private static bool IsRemoved(IFileSystem fs, string document)
    {
        string text;
        try
        {
            text = fs.File.ReadAllText(document);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        var match = Status().Match(FrontMatter(text));
        return match.Success && match.Groups[1].Value.Trim('"', '\'') == "removed";
    }

    /// <summary>The YAML front-matter block, or empty when the document opens with anything else.</summary>
    private static string FrontMatter(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return "";
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                return string.Join('\n', lines[1..i]);
            }
        }

        return "";
    }
}
