using System.IO.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Engine.Anchors;

/// <summary>
/// The source text a symbol-anchor is resolved against (C-07): every file under the
/// repository's source roots, read once. What is excluded is the load-bearing half - a stale
/// <c>obj/Debug/App.js</c> still carrying a deleted symbol would resolve it and the check
/// would miss the rot it exists to find, differently on a clean clone and a working one.
/// </summary>
public sealed class SymbolIndex
{
    private readonly List<string> texts;

    private SymbolIndex(IReadOnlyList<string> sourceRoots, List<string> texts)
    {
        SourceRoots = sourceRoots;
        this.texts = texts;
    }

    /// <summary>The root directory names the search covered, ordinally sorted - the finding text names them.</summary>
    public IReadOnlyList<string> SourceRoots { get; }

    /// <summary>
    /// Reads every source file once and keeps the text, because a symbol is answered by
    /// substring and not by identity. The Python holds one file at a time and stops early; the
    /// price of the wider contract here is the repository's source bytes in memory.
    /// </summary>
    public static SymbolIndex Build(IFileSystem fs, string root, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(options);

        var roots = SourceTree.Roots(fs, root, options);
        var texts = new List<string>();
        foreach (var file in SourceTree.Files(fs, root, options))
        {
            try
            {
                texts.Add(fs.File.ReadAllText($"{root}/{file}"));
            }
            catch (IOException)
            {
                // A file that cannot be read resolves nothing; the Python skips it the same way.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return new SymbolIndex(roots, texts);
    }

    /// <summary>Whether the identifier's leading segment occurs literally anywhere under the source roots.</summary>
    public bool Contains(string leadingSegment) =>
        texts.Exists(text => text.Contains(leadingSegment, StringComparison.Ordinal));

    /// <summary>Every non-hidden directory directly under the root - the repository's own top level.</summary>
    public static IEnumerable<string> TopLevelDirectories(IFileSystem fs, string root)
    {
        ArgumentNullException.ThrowIfNull(fs);

        if (!fs.Directory.Exists(root))
        {
            return [];
        }

        return fs.Directory.EnumerateDirectories(root)
            .Select(dir => dir.Replace('\\', '/').TrimEnd('/'))
            .Select(dir => dir[(dir.LastIndexOf('/') + 1)..])
            .Where(name => !name.StartsWith('.'));
    }

    private static IEnumerable<string> Files(IFileSystem fs, string directory) =>
        fs.Directory.Exists(directory)
            ? fs.Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            : [];
}
