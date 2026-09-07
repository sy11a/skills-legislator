using System.IO.Abstractions;

namespace Legislator.Engine.Anchors;

/// <summary>
/// What a path-anchor points at on disk. The classifier is pure and cannot answer this: a
/// <c>Type.Member()</c> token names a file only if the stem exists wearing one of the source
/// extensions, which is a question for a file system. Both jobs that read anchors ask it here,
/// so `anchors` and `okf-debt` can never disagree about which file a token means.
/// </summary>
public static class AnchorTarget
{
    /// <summary>The repository-relative path the token resolves to - file or directory - or null when nothing of that name is there.</summary>
    public static string? Resolve(
        IFileSystem fs, string root, string token, IReadOnlyList<string> sourceExtensions)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(sourceExtensions);

        var stem = AnchorClassifier.MemberStem(token);
        if (stem is not null)
        {
            foreach (var extension in sourceExtensions)
            {
                if (Exists(fs, $"{root}/{stem}{extension}"))
                {
                    return $"{stem}{extension}";
                }
            }
        }

        var target = AnchorClassifier.PathTarget(token);
        return Exists(fs, $"{root}/{target}") ? target : null;
    }

    private static bool Exists(IFileSystem fs, string path) =>
        fs.File.Exists(path) || fs.Directory.Exists(path);
}
