using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// Check 22: the gate declaration a machine reads. `.claude/rules/verification.md` carries a
/// repository's gates as rows of a three-column table, and the kernel's merge queue reads it that
/// way; a file written as prose parses to zero rows, and zero rows is read as no gates at all.
/// Two products shipped a whole release in that state — honestly written, carefully reasoned, and
/// unreadable by the machine that enforces them (sy11a/Architector#398).
/// </summary>
public sealed class Check22BindingsFormTests
{
    private const string Slug = "bindings-form";
    private const string Path = ".claude/rules/verification.md";

    /// <summary>aidispatcher's actual file, shortened — five prose bullets, not one pipe.</summary>
    [Fact]
    public void Given_bindings_written_as_prose_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "# Verification bindings (this repo)\n\n"
                   + "- No real gates exist yet: this repository opened with a README and the AI layer only.\n"
                   + "- Do not invent or run a build/test command that would fail.\n"
                   + "- The `legislator anchors` rung is the one gate already runnable.\n",
        });

        Assert.Contains(
            AuditFixture.Findings(report, Slug),
            f => f.Contains("declares no gate row a machine can read", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_three_column_table_When_audit_runs_Then_it_is_silent()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "# Verification Bindings (this repo)\n\n"
                   + "| Gate | Command | What it proves |\n"
                   + "|------|---------|----------------|\n"
                   + "| anchors | `legislator anchors` | every anchored path still resolves |\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>A header is optional: the reader does not require one and never reads its text.</summary>
    [Fact]
    public void Given_a_table_with_no_header_When_audit_runs_Then_it_is_silent()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "| anchors | `legislator anchors` | it resolves |\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>
    /// A file of nothing but a header and a separator declares nothing — and this is the case a
    /// kinder check would miss, because the file *looks* like a table.
    /// </summary>
    [Fact]
    public void Given_a_header_and_separator_only_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "| Gate | Command | What it proves |\n|------|---------|----------------|\n",
        });

        Assert.NotEmpty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>
    /// Four columns is not three. The kernel splits on `|` and demands exactly five pieces, so a
    /// wider table is skipped row by row — silently, which is why this check mirrors that parse
    /// rather than being more generous than the reader it stands in for.
    /// </summary>
    [Fact]
    public void Given_a_four_column_table_When_audit_runs_Then_it_is_a_warning()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "| Gate | Command | Bound by | Owner |\n"
                   + "|---|---|---|---|\n"
                   + "| build | `dotnet build` | repo law | me |\n",
        });

        Assert.NotEmpty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>
    /// An absent file is the ladder's own declared fallback (`core/verification.md`: "When that
    /// file is absent, the ladder still applies with repo defaults"), so this check says nothing
    /// about it. That it is a *worse* state for the merge queue than zero rows is a kernel defect,
    /// filed separately, and not this check's to report.
    /// </summary>
    [Fact]
    public void Given_no_bindings_file_at_all_When_audit_runs_Then_this_check_says_nothing()
    {
        var report = AuditFixture.Over(new()
        {
            ["docs/okf/index.md"] = "# OKF\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }
}
