using System.IO.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Core.Repo;

/// <summary>
/// The "is this a legislated repository?" test, in one place. Three hooks ask it from three
/// different starting points - the edited file, the payload's working directory, the git
/// toplevel - and all three mean the same thing: the nearest ancestor holding a manifest owns
/// this path. The manifest's own location comes from <see cref="RepoLayout"/>, so a repository
/// that renamed a directory is still recognised (C-07).
/// </summary>
public static class LegislatedRepo
{
    /// <summary>The nearest ancestor of <paramref name="start"/> (itself included) holding a manifest, or null when no ancestor does.</summary>
    public static string? Find(IFileSystem fs, LegislatorOptions options, string start)
    {
        ArgumentNullException.ThrowIfNull(fs);

        for (var at = start; !string.IsNullOrEmpty(at); at = fs.Path.GetDirectoryName(at))
        {
            if (fs.File.Exists(new RepoLayout(options, at).Manifest))
            {
                return at;
            }
        }

        return null;
    }
}
