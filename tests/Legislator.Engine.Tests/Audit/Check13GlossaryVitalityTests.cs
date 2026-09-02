using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 13: a repository with source code and an empty glossary has a domain nobody wrote down. Skipped where there is no source at all, a documentation repository having no domain to name.</summary>
public sealed class Check13GlossaryVitalityTests
{
    private const string Slug = "glossary-vitality";

    private const string Empty = "# Glossary\n\n| Term | Meaning |\n|---|---|\n";

    private const string Seeded = Empty + "| widget | a thing |\n";

    [Fact]
    public void Given_an_empty_glossary_beside_source_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new() { ["docs/okf/glossary.md"] = Empty });

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("glossary empty in a repo with source code", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_glossary_with_one_term_When_audit_runs_Then_the_check_is_clean()
    {
        var report = AuditFixture.Over(new() { ["docs/okf/glossary.md"] = Seeded });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_no_glossary_at_all_When_audit_runs_Then_the_check_does_not_run()
    {
        var report = AuditFixture.Over();

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
