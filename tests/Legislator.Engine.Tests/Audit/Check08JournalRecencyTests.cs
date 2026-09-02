using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 8: the journal has kept up with the code. Git-backed, so the last commit outside the docs tree is the reference date and the journal's own newest entry is the claim.</summary>
public sealed class Check08JournalRecencyTests
{
    private const string Slug = "journal-recency";

    private static Dictionary<string, string> Journal(string entry, string body = "# entry\n") =>
        new() { [$"docs/journal/{entry}"] = body, ["docs/journal/README.md"] = "# j\n" };

    [Fact]
    public void Given_an_entry_within_the_threshold_When_audit_runs_Then_the_check_is_clean()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(Journal("2026-06-20.md")), AuditFixture.Dated("2026-07-01"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_the_newest_entry_is_older_than_the_threshold_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(Journal("2026-01-15.md")), AuditFixture.Dated("2026-07-01"));

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("newest entry is 2026-01-15", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_date_only_in_the_content_When_audit_runs_Then_it_is_read_from_there()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(Journal("setup.md", "# 2026-06-20 — setup\n")), AuditFixture.Dated("2026-07-01"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_no_dated_entry_anywhere_When_audit_runs_Then_the_finding_says_so()
    {
        var report = AuditFixture.Audit(
            AuditFixture.Repo(Journal("setup.md", "# setup\n")), AuditFixture.Dated("2026-07-01"));

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("no dated entries found", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_no_journal_directory_When_audit_runs_Then_the_check_does_not_run()
    {
        var report = AuditFixture.Audit(AuditFixture.Repo(), AuditFixture.Dated("2026-07-01"));

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
