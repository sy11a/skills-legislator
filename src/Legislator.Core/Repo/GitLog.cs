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
    /// One git question, asked the one way this system asks them: the trimmed output when git
    /// answered, null when git ran and refused (no repository, no history for the path), and
    /// the flag false only when git itself could not be started. The three are distinct because
    /// a job may report clean over the middle one and must never report clean over the last.
    /// </summary>
    public static (string? Output, bool Available) Ask(
        IProcessRunner proc, LegislatorOptions options, string root, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);

        ProcessResult result;
        try
        {
            result = proc.Run(
                options.GitExecutable.Value, args, root,
                TimeSpan.FromSeconds(options.GitTimeoutSeconds.Value));
        }
        catch (ProcessStartException)
        {
            return (null, false);
        }

        if (result.ExitCode != 0)
        {
            return (null, true);
        }

        var output = result.Stdout.Trim();
        return (output.Length == 0 ? null : output, true);
    }

    /// <summary>
    /// The command's output VERBATIM - no trim - or null when git ran and refused. <see cref="Ask"/>
    /// trims because a branch name or a date carries no leading space; porcelain does, and its
    /// first two columns ARE the answer. A caller reading a fixed-width format through
    /// <see cref="Ask"/> loses one column and never learns it.
    /// </summary>
    public static (string? Output, bool Available) Read(
        IProcessRunner proc, LegislatorOptions options, string root, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var result = proc.Run(
                options.GitExecutable.Value, args, root,
                TimeSpan.FromSeconds(options.GitTimeoutSeconds.Value));

            return result.ExitCode == 0 ? (result.Stdout, true) : (null, true);
        }
        catch (ProcessStartException)
        {
            return (null, false);
        }
    }

    /// <summary>
    /// Whether git ran the command and it succeeded. Distinct from <see cref="Ask"/>, which
    /// answers with output: a command that writes rather than reports - `mv` - succeeds
    /// silently, and an empty answer from <see cref="Ask"/> cannot be told from a refusal.
    /// The flag is false only when git itself could not be started.
    /// </summary>
    public static (bool Ok, bool Available) Succeeded(
        IProcessRunner proc, LegislatorOptions options, string root, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            return (proc.Run(
                options.GitExecutable.Value, args, root,
                TimeSpan.FromSeconds(options.GitTimeoutSeconds.Value)).ExitCode == 0, true);
        }
        catch (ProcessStartException)
        {
            return (false, false);
        }
    }

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

        return Ask(proc, options, root, "log", "-1", "--format=%cI", "--", relative);
    }
}
