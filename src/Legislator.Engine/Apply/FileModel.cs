using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;

namespace Legislator.Engine.Apply;

/// <summary>
/// The v14 file model: one canonical entry document, and the alias as a symlink to it. A
/// repository arriving with a real alias is moved into the model rather than left in two
/// minds - through git where git tracks the file, so the rename survives as a rename in the
/// history instead of appearing as one deletion and one birth.
/// </summary>
public static class FileModel
{
    /// <summary>Wires the model, returning the events it performed - the lines the run record carries and the report explains a file by.</summary>
    public static IReadOnlyList<string> Wire(
        IFileSystem fs, IProcessRunner proc, LegislatorOptions options, RepoLayout layout)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(layout);

        var canonical = options.EntryDocument.Value;
        var alias = options.EntryAlias.Value;
        var canonicalPath = $"{layout.Root}/{canonical}";
        var aliasPath = $"{layout.Root}/{alias}";
        var events = new List<string>();

        if (EntryDocument.IsRealAlias(fs, layout, options) && !fs.File.Exists(canonicalPath))
        {
            Rename(fs, proc, options, layout, alias, canonical, aliasPath, canonicalPath);
            events.Add($"renamed {alias} -> {canonical}");
        }

        if (!fs.File.Exists(canonicalPath))
        {
            return events;
        }

        var linkTarget = LinkTargetOf(fs, aliasPath);
        if (linkTarget is not null)
        {
            if (linkTarget != canonical)
            {
                fs.File.Delete(aliasPath);
                fs.File.CreateSymbolicLink(aliasPath, canonical);
                events.Add($"relinked {alias} -> {canonical}");
            }
        }
        else if (!fs.File.Exists(aliasPath))
        {
            fs.File.CreateSymbolicLink(aliasPath, canonical);
            events.Add($"linked {alias} -> {canonical}");
        }

        return events;
    }

    /// <summary>Through git where git tracks the file, by hand otherwise - and by hand too when git is absent, because the model is the point and git is only how the history reads afterwards.</summary>
    private static void Rename(
        IFileSystem fs,
        IProcessRunner proc,
        LegislatorOptions options,
        RepoLayout layout,
        string alias,
        string canonical,
        string aliasPath,
        string canonicalPath)
    {
        var tracked = GitLog.Succeeded(proc, options, layout.Root, "ls-files", "--error-unmatch", alias);
        if (tracked is { Ok: true, Available: true }
            && GitLog.Succeeded(proc, options, layout.Root, "mv", alias, canonical).Ok
            && fs.File.Exists(canonicalPath))
        {
            return;
        }

        fs.File.Move(aliasPath, canonicalPath, overwrite: true);
    }

    /// <summary>What the alias points at, or null when it is not a link at all.</summary>
    private static string? LinkTargetOf(IFileSystem fs, string path)
    {
        try
        {
            return fs.File.Exists(path) ? fs.FileInfo.New(path).LinkTarget : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
