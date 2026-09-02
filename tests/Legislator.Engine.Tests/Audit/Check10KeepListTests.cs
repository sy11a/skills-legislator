using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 10: a keep promise the layer can still honour. A kept file has to exist, and a kept document has to be wired into the layer - a protected file nothing points at is protection with no purpose.</summary>
public sealed class Check10KeepListTests
{
    private const string Slug = "keep-list";

    private static string KeepingOnly(string path) =>
        AuditFixture.Manifest(body: $"\"stacks\": [], \"keep\": [{{\"path\": \"{path}\", \"reason\": \"ours\"}}], \"ownedFiles\": []");

    [Fact]
    public void Given_a_kept_project_rule_When_audit_runs_Then_its_wiring_is_not_questioned()
    {
        var report = AuditFixture.Over(new() { [".claude/rules/x.md"] = "# x\n" }, KeepingOnly(".claude/rules/x.md"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_a_kept_document_nothing_references_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new() { ["docs/kept.md"] = "# kept\n" }, KeepingOnly("docs/kept.md"));

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("referenced from nowhere", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_kept_document_the_index_links_When_audit_runs_Then_the_check_is_clean()
    {
        var report = AuditFixture.Over(
            new()
            {
                ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and `docs/kept.md`.\n",
                ["docs/kept.md"] = "# kept\n",
            },
            KeepingOnly("docs/kept.md"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_a_kept_file_that_is_not_a_document_When_audit_runs_Then_only_its_presence_is_judged()
    {
        var report = AuditFixture.Over(new() { ["tools/x.sh"] = "#!/bin/sh\n" }, KeepingOnly("tools/x.sh"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
