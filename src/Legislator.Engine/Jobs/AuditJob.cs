using Legislator.Core.Manifest;
using Legislator.Core.Repo;
using Legislator.Engine.Audit;

namespace Legislator.Engine.Jobs;

/// <summary>
/// The read-only health check of a repository's AI layer: the mechanical checks, the model's
/// half merged in from the findings channel, and the pinned report printed from both. Exit 1
/// when anything actionable was found - Warning and above; Info alone exits 0 - 0 when nothing
/// was, and an exit outside that pair means the audit itself failed, which is never the same as
/// a clean layer (ADR-0003, R-661..R-669).
/// </summary>
public sealed class AuditJob : IJob
{
    private const string ModelFindingsFlag = "--model-findings";

    public string Name => "audit";

    public string Usage => $"{Name} --skill <skill-path> [--root <dir>] [{ModelFindingsFlag} <json>]";

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options, ModelFindingsFlag);
        if (parsed is null)
        {
            return error!;
        }

        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var result = new AuditChecks(ctx, parsed.Skill).Run();
        var model = parsed.Value(ModelFindingsFlag) is { } path
            ? ModelFindings.Load(ctx.Fs, path)
            : null;

        if (model is not null)
        {
            foreach (var finding in model.Findings)
            {
                if (finding.Escalates is { } superseded)
                {
                    result.Escalate(finding.Slug, superseded);
                }

                result.Add(new AuditFinding(finding.Severity, finding.Slug, finding.Text));
            }
        }

        var report = AuditReport.Render(
            result,
            RepositoryName(ctx.Root),
            DateOnly.FromDateTime(ctx.Clock.GetLocalNow().DateTime),
            ManifestFile.Scalar(ManifestFile.TryRead(ctx.Fs, layout).Node, ManifestFile.VersionKey) ?? "?",
            parsed.Skill.Version ?? "?",
            model,
            ctx.Options.ArmExecutable.Value);

        // v26 (T-13.2): Info does not raise the exit code - only Warning and above do.
        // An Info line is by construction not a finding to act on (check 20 prints one on
        // every untagged edition), and an audit that exits 1 for it teaches its callers to
        // stop reading the exit code, which is the one signal `verify` and the ladder rest on.
        var actionable = result.Findings.Count(f => f.Severity != Severity.Info);
        return new JobResult(actionable > 0 ? 1 : 0, report, "");
    }

    /// <summary>The repository's own name, which the report's title line carries - the last segment of the root it was given.</summary>
    private static string RepositoryName(string root)
    {
        var path = root.Replace('\\', '/').TrimEnd('/');
        return path[(path.LastIndexOf('/') + 1)..];
    }
}
