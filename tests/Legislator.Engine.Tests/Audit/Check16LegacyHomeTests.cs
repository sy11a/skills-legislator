using Legislator.Core.Abstractions;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 16: a spec or plan born in the legacy home after the repository was legislated. Git-backed both ways - the layer's birth date and the file's, each read as the oldest adding commit.</summary>
public sealed class Check16LegacyHomeTests
{
    private const string Slug = "legacy-home-violation";

    /// <summary>Adding-commit dates by the path asked about; the manifest's own date is the legislation date.</summary>
    private static FakeProcessRunner Born(Dictionary<string, string> dates) => new()
    {
        OnRun = (_, args, _) => new ProcessResult(0, dates.GetValueOrDefault(args[^1], "") + "\n", ""),
    };

    [Fact]
    public void Given_a_legacy_spec_born_after_legislation_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(new() { ["docs/superpowers/specs/x.md"] = "# x\n" }),
            Born(new()
            {
                ["docs/ai/manifest.json"] = "2026-01-01",
                ["docs/superpowers/specs/x.md"] = "2026-03-01",
            }));

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("docs/superpowers/specs/x.md", StringComparison.Ordinal)
                && f.Contains("born in a legacy home after legislation (2026-03-01)", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_legacy_spec_older_than_legislation_When_audit_runs_Then_it_is_history_not_a_finding()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(new() { ["docs/superpowers/plans/x.md"] = "# x\n" }),
            Born(new()
            {
                ["docs/ai/manifest.json"] = "2026-03-01",
                ["docs/superpowers/plans/x.md"] = "2026-01-01",
            }));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_a_tree_with_no_legislation_commit_When_audit_runs_Then_the_check_does_not_run()
    {
        var report = AuditFixture.Audit(AuditFixture.Repo(new() { ["docs/superpowers/specs/x.md"] = "# x\n" }));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
