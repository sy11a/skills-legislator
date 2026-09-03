using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Legislator.Core.Options;

namespace Legislator.Engine.Runs;

/// <summary>
/// Where a run's record lives. Outside the repository by construction: the record describes the
/// tree, and a description sitting inside what it describes is picked up by the audit that
/// reads the tree, kept by the keep list, and committed by whoever runs `git add -A`. The
/// digest is what lets two checkouts of the same project - a worktree and its origin - keep
/// separate records instead of overwriting each other's.
/// </summary>
public static class RecordPath
{
    public static string Of(IFileSystem fs, LegislatorOptions options, string root, string? explicitPath)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(options);

        if (explicitPath is not null)
        {
            return explicitPath;
        }

        var full = fs.Path.GetFullPath(root).Replace('\\', '/').TrimEnd('/');
        var name = full[(full.LastIndexOf('/') + 1)..];
#pragma warning disable CA5350 // Not a security primitive: the digest only separates two repositories of the same name, and the algorithm is the Python's, which parity fixes.
        var digest = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(full)))[..8];
#pragma warning restore CA5350
        return $"{fs.Path.GetTempPath().Replace('\\', '/').TrimEnd('/')}/{options.RunRecordDir.Value}/{name}-{digest}.json";
    }
}
