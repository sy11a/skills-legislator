using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 7: a document nothing points at. The exemptions are the point - the record homes are meant to be unreferenced, and reporting them would make the worklist noise (`core/artifact-lifecycle.md`).</summary>
public sealed class Check07OrphanDocsTests
{
    private const string Slug = "orphan-docs";

    [Theory]
    [InlineData("docs/ai/rules/core/x.md")]
    [InlineData("docs/adr/0001-x.md")]
    [InlineData("docs/journal/2026-01-01.md")]
    [InlineData("docs/superpowers/specs/x.md")]
    [InlineData("docs/cases/BL-001-x/spec.md")]
    [InlineData("docs/backlog.md")]
    public void Given_a_document_in_an_exempt_home_When_audit_runs_Then_it_is_no_orphan(string path)
    {
        var report = AuditFixture.Over(new() { [path] = "# x\n" });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_a_document_referenced_only_by_a_relative_markdown_link_When_audit_runs_Then_it_is_no_orphan()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and [notes](notes.md).\n",
            ["docs/okf/notes.md"] = "# Notes\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_a_document_nothing_names_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new() { ["docs/okf/notes.md"] = "# Notes\n" });

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("docs/okf/notes.md", StringComparison.Ordinal));
    }
}
