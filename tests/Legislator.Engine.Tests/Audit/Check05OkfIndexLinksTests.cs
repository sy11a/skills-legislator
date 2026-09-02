using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>Check 5: every markdown link the OKF index makes resolves, relative to the index or to the repository root.</summary>
public sealed class Check05OkfIndexLinksTests
{
    private const string Slug = "okf-index-links";

    [Fact]
    public void Given_a_link_that_resolves_to_nothing_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and [gone](gone.md).\n",
        });

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("`gone.md` does not resolve", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_link_relative_to_the_index_When_audit_runs_Then_it_resolves()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and [map](codebase-map.md).\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    [Fact]
    public void Given_an_http_link_When_audit_runs_Then_it_is_never_probed()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and [spec](https://example.invalid/x).\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
