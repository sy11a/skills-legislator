using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Checks 15 and 17: the audit is the second caller of the anchors and okf-debt jobs. It does not re-derive what they know - it re-prints their lines with the sentence that tells a reader what to do about one.</summary>
public sealed class Check15OkfAnchorsTests
{
    [Fact]
    public void Given_a_document_naming_a_path_that_is_gone_When_audit_runs_Then_the_anchors_line_is_a_warning()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/widgets.md"] = "# Widgets\n\nImplemented in `src/Gone.cs`.\n",
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and `docs/okf/widgets.md`.\n",
        });

        Assert.Contains(
            AuditFixture.Findings(report, "okf-anchors"),
            f => f.Contains("src/Gone.cs", StringComparison.Ordinal)
                && f.Contains("the repo no longer contains it", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_leftover_engine_file_When_audit_runs_Then_the_check_neither_consults_nor_reports_it()
    {
        // v26 (R-8207): the Python engine is retired, so `docs/ai/engine.py` is an ordinary
        // file a repository may still carry from an earlier edition. The check runs in-process
        // and says nothing about it - the v25 branch that reported an absent engine as Info
        // died with the engine it named.
        var fs = AuditFixture.Repo(new() { ["docs/ai/engine.py"] = "# left over from v25\n" });

        var report = AuditFixture.Audit(fs);

        Assert.DoesNotContain(
            AuditFixture.Findings(report, "okf-anchors"),
            f => f.Contains("engine", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_no_okf_bundle_at_all_When_audit_runs_Then_neither_check_runs()
    {
        var fs = AuditFixture.Repo();
        fs.File.Delete($"{AuditFixture.Root}/docs/okf/index.md");
        fs.File.Delete($"{AuditFixture.Root}/docs/okf/codebase-map.md");
        fs.Directory.Delete($"{AuditFixture.Root}/docs/okf");

        var report = AuditFixture.Audit(fs);

        Assert.Empty(AuditFixture.Findings(report, "okf-anchors"));
        Assert.Empty(AuditFixture.Findings(report, "okf-sync-debt"));
    }
}
