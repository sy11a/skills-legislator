using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 9: agent-tooling debris beside the AI layer. Info, except the one case the v14 file model calls a defect - a real entry alias next to the canonical document.</summary>
public sealed class Check09ForeignStructuresTests
{
    private const string Slug = "foreign-structures";

    [Fact]
    public void Given_a_foreign_directory_When_audit_runs_Then_every_file_under_it_is_named()
    {
        var report = AuditFixture.Over(new()
        {
            [".cursor/rules/a.md"] = "x\n",
            [".cursor/rules/b.md"] = "y\n",
        });

        var findings = AuditFixture.Findings(report, Slug);
        Assert.Contains(findings, f => f.Contains(".cursor/rules/a.md", StringComparison.Ordinal));
        Assert.Contains(findings, f => f.Contains(".cursor/rules/b.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_kept_foreign_path_When_audit_runs_Then_it_is_not_reported()
    {
        var report = AuditFixture.Over(
            new() { ["CONTEXT.md"] = "x\n" },
            AuditFixture.Manifest(body: "\"stacks\": [], \"keep\": [{\"path\": \"CONTEXT.md\", \"reason\": \"ours\"}], \"ownedFiles\": []"));

        Assert.DoesNotContain(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("CONTEXT.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_real_alias_beside_the_canonical_entry_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new() { ["CLAUDE.md"] = "# Repo\n" });

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("CLAUDE.md", StringComparison.Ordinal)
                && f.Contains("should be", StringComparison.Ordinal));
    }
}
