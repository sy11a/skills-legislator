using Legislator.Core.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Core.Repo;

/// <summary>
/// The one place the system asks git when a path last changed (C-08). The answer carries
/// availability beside the date because the two failures are not the same fault: a path with
/// no history is ordinary, an absent instrument is a job that must not report clean (R-665).
/// </summary>
public static class GitLog
{
    /// <summary>
    /// The committer date of the newest commit touching <paramref name="relative"/>, ISO-8601,
    /// or null when the path is untracked or the tree is no repository. The flag is false only
    /// when git itself could not be started.
    /// </summary>
    public static (string? Iso, bool GitAvailable) NewestCommit(
        IProcessRunner proc, LegislatorOptions options, string root, string relative)
    {
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);

        ProcessResult result;
        try
        {
            result = proc.Run(
                options.GitExecutable.Value,
                ["log", "-1", "--format=%cI", "--", relative],
                root,
                TimeSpan.FromSeconds(options.GitTimeoutSeconds.Value));
        }
        catch (ProcessStartException)
        {
            return (null, false);
        }

        var iso = result.Stdout.Trim();
        return (iso.Length == 0 ? null : iso, true);
    }
}
