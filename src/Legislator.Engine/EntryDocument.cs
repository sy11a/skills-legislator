using System.IO.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;

namespace Legislator.Engine;

/// <summary>
/// Which document is this repository's entry point, under the v14 file model: the canonical one
/// where it exists, and the alias only where it is a real file. An alias that is the symlink is
/// the model working, not a second entry - and two real entry documents is the state audit
/// reports and apply refuses. Detect and audit both ask, so the question is answered once.
/// </summary>
public static class EntryDocument
{
    /// <summary>The entry document's name, or null when the repository has none.</summary>
    public static string? Of(IFileSystem fs, RepoLayout layout, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        if (fs.File.Exists($"{layout.Root}/{options.EntryDocument.Value}"))
        {
            return options.EntryDocument.Value;
        }

        var alias = $"{layout.Root}/{options.EntryAlias.Value}";
        return fs.File.Exists(alias) && !IsLink(fs, alias) ? options.EntryAlias.Value : null;
    }

    /// <summary>Whether the alias is a real file rather than the symlink the file model puts there.</summary>
    public static bool IsRealAlias(IFileSystem fs, RepoLayout layout, LegislatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        var alias = $"{layout.Root}/{options.EntryAlias.Value}";
        return fs.File.Exists(alias) && !IsLink(fs, alias);
    }

    private static bool IsLink(IFileSystem fs, string path)
    {
        try
        {
            return fs.FileInfo.New(path).LinkTarget is not null;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
