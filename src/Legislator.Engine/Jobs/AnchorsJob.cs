using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;
using Legislator.Engine.Anchors;
using Legislator.Engine.Text;

namespace Legislator.Engine.Jobs;

/// <summary>
/// The executing arm of the OKF link-hardness rule (C-07, R-8205): every path and symbol the
/// knowledge layer backticks still exists in this repository. It writes nothing and reports
/// every anchor that no longer resolves - a finding means a document describes code that is
/// gone.
/// </summary>
public sealed partial class AnchorsJob : IJob
{
    public string Name => "anchors";

    public string Usage => Name;

    [GeneratedRegex("`([^`\n]+)`")]
    private static partial Regex Token();

    [GeneratedRegex(@"^status:\s*(\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex Status();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var fs = ctx.Fs;
        var topLevel = SymbolIndex.TopLevelDirectories(fs, ctx.Root).ToHashSet(StringComparer.Ordinal);
        var index = SymbolIndex.Build(fs, ctx.Root, ctx.Options);
        var roots = index.SourceRoots.Count > 0
            ? string.Join(", ", index.SourceRoots.Select(name => name + "/"))
            : "(no source roots)";

        var findings = new List<string>();
        var sites = new List<(string Document, int Line, string Token)>();

        foreach (var document in AnchoredDocuments(fs, layout, ctx.Options.HumanClassDocs.Value))
        {
            var relative = document[(ctx.Root.Length + 1)..].Replace('\\', '/');
            foreach (var (lineNumber, line) in ScannableLines(fs.File.ReadAllText(document)))
            {
                foreach (var match in Token().Matches(line).Cast<Match>())
                {
                    var token = match.Groups[1].Value.Trim();
                    switch (AnchorClassifier.Classify(token, topLevel))
                    {
                        case AnchorKind.Path when !PathResolves(fs, ctx, token):
                            findings.Add($"{relative}:{lineNumber}: path-anchor: {token} → no such file");
                            break;
                        case AnchorKind.Symbol:
                            sites.Add((relative, lineNumber, token));
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        foreach (var (document, line, token) in sites)
        {
            var leading = token.Split('.', 2)[0];
            if (!index.Contains(leading))
            {
                findings.Add($"{document}:{line}: symbol-anchor: {token} → not found in {roots}");
            }
        }

        return Findings.AsResult(findings);
    }

    /// <summary>
    /// A path-anchor resolves when its target exists; <c>Type.Member()</c> resolves when the
    /// stem exists wearing one of the source extensions, which is the probe the classifier
    /// cannot do without a file system.
    /// </summary>
    private static bool PathResolves(IFileSystem fs, JobContext ctx, string token)
    {
        var stem = AnchorClassifier.MemberStem(token);
        if (stem is not null
            && ctx.Options.SourceExtensions.Value.Any(ext => Exists(fs, $"{ctx.Root}/{stem}{ext}")))
        {
            return true;
        }

        return Exists(fs, $"{ctx.Root}/{AnchorClassifier.PathTarget(token)}");
    }

    private static bool Exists(IFileSystem fs, string path) =>
        fs.File.Exists(path) || fs.Directory.Exists(path);

    /// <summary>
    /// The anchored class: every markdown document of the bundle except the human class and
    /// anything flipped to <c>status: removed</c>, which the checklist tells the owner to keep
    /// and mark. The walk yields directories as well as files, as the Python's <c>rglob</c>
    /// does - a bundle entry that is not a readable document is a fault, not a silent skip.
    /// </summary>
    private static IEnumerable<string> AnchoredDocuments(
        IFileSystem fs, RepoLayout layout, IReadOnlyList<string> humanClass)
    {
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

    /// <summary>Every line outside the front matter and outside fenced code blocks, numbered from one.</summary>
    private static IEnumerable<(int Number, string Line)> ScannableLines(string text)
    {
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
}
