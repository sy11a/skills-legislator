using System.IO.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Skill;

namespace Legislator.Engine;

/// <summary>
/// The parsed command line of a job in the `--skill` family (C-09): the package the run was
/// pointed at, and whatever optional flags that job declares. Parsing lives here rather than in
/// each job because the family shares one contract - `--skill` is required and must name a
/// directory, a declared flag takes one value, anything else is a usage exit. `--root` never
/// reaches a job: which repository is being worked on is the host's question.
///
/// Flags are kept in the order they were written rather than folded into a map, because two of
/// them repeat: `--keep-add` and `--keep-remove` are lists the owner builds one entry at a
/// time, and a map would silently keep the last.
/// </summary>
public sealed record SkillArguments(SkillPackage Skill, IReadOnlyList<(string Flag, string Value)> Flags)
{
    private const int UsageError = 2;

    /// <summary>The value of a flag written at most once, or null when it was not written. The last wins where it was written twice - a flag that is not a list has no better answer.</summary>
    public string? Value(string flag)
    {
        string? found = null;
        foreach (var (name, value) in Flags)
        {
            if (string.Equals(name, flag, StringComparison.Ordinal))
            {
                found = value;
            }
        }

        return found;
    }

    /// <summary>Every value written for a repeatable flag, in the order the caller wrote them - the order the keep list applies them in.</summary>
    public IReadOnlyList<string> All(string flag) =>
        [.. Flags.Where(f => string.Equals(f.Flag, flag, StringComparison.Ordinal)).Select(f => f.Value)];

    public static (SkillArguments? Parsed, JobResult? Error) Parse(
        IJob job, JobContext ctx, IFileSystem fs, LegislatorOptions options, params string[] optional)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(fs);

        var flags = new List<(string Flag, string Value)>();
        string? skill = null;
        for (var i = 0; i < ctx.Args.Count; i += 2)
        {
            var flag = ctx.Args[i];
            if (i + 1 >= ctx.Args.Count)
            {
                return (null, Usage(job));
            }

            var value = ctx.Args[i + 1];
            if (flag == "--skill")
            {
                skill = value;
            }
            else if (optional.Contains(flag, StringComparer.Ordinal))
            {
                flags.Add((flag, value));
            }
            else
            {
                return (null, Usage(job));
            }
        }

        // The package must be a directory, not merely a path someone typed: a run pointed at a
        // file would fail later, deep inside a read, naming the wrong thing.
        return skill is null || !fs.Directory.Exists(skill)
            ? (null, new JobResult(UsageError, "", $"{job.Name} requires --skill <skill-path> (the legislator package root)\n"))
            : (new SkillArguments(new SkillPackage(fs, options, skill), flags), null);
    }

    private static JobResult Usage(IJob job) => new(UsageError, "", $"usage: legislator {job.Usage}\n");
}
