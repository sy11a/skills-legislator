using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// Confirmation that the core-rule tier split (BL-484) needed no change to checks 1 and 7:
/// neither assumes an owned rule is `@`-imported. `docs/okf/codebase-map.md` is the subject for
/// check 7 (not exempt, reached only by a pointer under the new design) because
/// `docs/ai/rules/**` is already exempt wholesale and a core-rule-file version of this test would
/// pass vacuously.
/// </summary>
public sealed class CoreRuleTierAuditTests
{
    [Fact]
    public void Given_the_codebase_map_reached_only_by_a_pointer_line_in_AGENTS_md_When_audit_runs_Then_check_7_does_not_flag_it_as_orphaned()
    {
        var report = AuditFixture.Over(new()
        {
            ["AGENTS.md"] =
                "# Repo\n\n@docs/okf/index.md\n\n"
                + "- When finding where something lives, read `docs/okf/codebase-map.md` — it is law, not a reference.\n",
        });

        Assert.DoesNotContain(
            AuditFixture.Findings(report, "orphan-docs"),
            f => f.Contains("codebase-map.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_an_on_demand_rules_pointer_line_naming_it_rather_than_importing_it_When_audit_runs_Then_check_1_raises_nothing_for_it()
    {
        var report = AuditFixture.Over(new()
        {
            ["AGENTS.md"] =
                "# Repo\n\n@docs/okf/index.md\n\n"
                + "- Before changing code that implements a concept, read `docs/ai/rules/core/okf.md` — "
                + "it is law, not a reference.\n",
        });

        Assert.Empty(AuditFixture.Findings(report, "imports-resolve"));
    }
}
