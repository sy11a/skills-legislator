using System.IO.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Engine.Anchors;

/// <summary>
/// The repository's source, as `core/okf.md` defines it: every non-hidden top-level directory
/// except the knowledge layer and build output, walked to any depth, skipping build directories
/// wherever they appear and files too big to be source. Three readers need exactly this walk -
/// the symbol index, the annotated-test scan and, from T-09, the audit - and a walk written
/// three times is three definitions of "source" waiting to drift apart.
/// </summary>
public static class SourceTree
{
    /// <summary>The source roots, ordinal-sorted: the names a finding prints when a symbol resolves nowhere.</summary>
    public static IReadOnlyList<string> Roots(IFileSystem fs, string root, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ignored = new HashSet<string>(options.BuildDirs.Value, StringComparer.Ordinal) { options.DocsDir.Value };

        return [.. SymbolIndex.TopLevelDirectories(fs, root)
            .Where(name => !ignored.Contains(name))
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Every readable source file under the roots, repository-relative and ordinal-sorted.</summary>
    public static IEnumerable<string> Files(IFileSystem fs, string root, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(options);

        var buildDirs = options.BuildDirs.Value.ToHashSet(StringComparer.Ordinal);
        var found = new List<string>();
        foreach (var name in Roots(fs, root, options))
        {
            var directory = $"{root}/{name}";
            if (!fs.Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in fs.Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relative = file.Replace('\\', '/')[(root.Length + 1)..];
                if (relative.Split('/').Any(part => part.StartsWith('.') || buildDirs.Contains(part)))
                {
                    continue;
                }

                if (WithinCeiling(fs, file, options.MaxFileBytes.Value))
                {
                    found.Add(relative);
                }
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static bool WithinCeiling(IFileSystem fs, string file, int ceiling)
    {
        try
        {
            return fs.FileInfo.New(file).Length <= ceiling;
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
