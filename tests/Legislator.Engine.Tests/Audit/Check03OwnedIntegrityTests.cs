using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 3: the owned set is the skill's, byte for byte. No ruler assertion reaches this one, and it is the check the whole one-way law stratum rests on.</summary>
public sealed class Check03OwnedIntegrityTests
{
    private const string Slug = "owned-integrity";

    [Fact]
    public void Given_a_manifest_that_does_not_parse_When_audit_runs_Then_it_is_a_critical_finding_not_a_crash()
    {
        var report = AuditFixture.Over(manifest: "{nope");

        var findings = AuditFixture.Findings(report, Slug);
        Assert.Single(findings);
        Assert.Contains("does not parse as JSON", findings[0], StringComparison.Ordinal);
        Assert.Equal(1, report.ExitCode);
    }

    [Fact]
    public void Given_an_owned_file_missing_from_disk_When_audit_runs_Then_it_is_named()
    {
        var report = AuditFixture.Over(manifest: AuditFixture.Manifest(
            body: "\"stacks\": [], \"keep\": [], \"ownedFiles\": [\"docs/ai/rules/core/okf.md\"]"));

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("docs/ai/rules/core/okf.md", StringComparison.Ordinal)
                && f.Contains("missing from disk", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_an_owned_rule_that_diverges_from_the_skill_source_When_audit_runs_Then_it_is_named()
    {
        var fs = AuditFixture.Repo(
            new() { ["docs/ai/rules/core/okf.md"] = "edited in place\n" },
            AuditFixture.Manifest(body: "\"stacks\": [], \"keep\": [], \"ownedFiles\": [\"docs/ai/rules/core/okf.md\"]"));
        fs.AddFile($"{AuditFixture.SkillPath}/assets/rules/core/okf.md", new MockFileData("the source\n"));

        var report = AuditFixture.Audit(fs);

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("diverges from the skill source", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_an_owned_rule_identical_to_the_skill_source_When_audit_runs_Then_the_check_is_clean()
    {
        var fs = AuditFixture.Repo(
            new() { ["docs/ai/rules/core/okf.md"] = "the source\n" },
            AuditFixture.Manifest(body: "\"stacks\": [], \"keep\": [], \"ownedFiles\": [\"docs/ai/rules/core/okf.md\"]"));
        fs.AddFile($"{AuditFixture.SkillPath}/assets/rules/core/okf.md", new MockFileData("the source\n"));

        var report = AuditFixture.Audit(fs);

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_an_owned_path_with_no_skill_source_When_audit_runs_Then_only_its_presence_is_judged()
    {
        var report = AuditFixture.Over(
            new() { ["docs/project-owned.md"] = "x\n" },
            AuditFixture.Manifest(body: "\"stacks\": [], \"keep\": [], \"ownedFiles\": [\"docs/project-owned.md\"]"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
