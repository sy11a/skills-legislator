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
    /// The one form rule that got its own bullet in the law, and had no test: a `|` inside a
    /// command breaks its row. The kernel splits on `|` and demands five pieces, so a sixth piece
    /// skips the row — silently, there and here.
    /// </summary>
    [Fact]
    public void Given_a_pipe_inside_the_command_When_audit_runs_Then_that_row_does_not_count()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "| Gate | Command | What it proves |\n"
                   + "|---|---|---|\n"
                   + "| test | `dotnet test | tail -1` | it runs |\n",
        });

        Assert.NotEmpty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>
    /// The real migration shape: one gate converted to a table row, the rest left as prose. The
    /// check is silent — correctly, because the file **does** declare a readable row — and the
    /// prose gates are invisible to the kernel. The check answers *is anything readable*, never
    /// *is everything here readable*, and this test pins that boundary so nobody reads a green as
    /// the second thing.
    /// </summary>
    [Fact]
    public void Given_one_real_row_beside_prose_gates_When_audit_runs_Then_it_is_silent()
    {
        var report = AuditFixture.Over(new()
        {
            [Path] = "# Verification Bindings\n\n"
                   + "- build: run `dotnet build -warnaserror` from the root.\n"
                   + "- e2e: drive the app through Playwright.\n\n"
                   + "| anchors | `legislator anchors` | it resolves |\n",
        });

        Assert.Empty(AuditFixture.Findings(report, Slug));
    }

    /// <summary>
    /// An absent file is silent — and this test pins the reasoning, because the refutation round
    /// argued for an Info note here and the argument was half right. Absence is the ladder's own
    /// declared fallback (`core/verification.md`: "When that file is absent, the ladder still
    /// applies with repo defaults"), five repositories of this fleet are in that state, and a note
    /// on every audit of every one of them is a class of item that yields no action —
    /// `core/artifact-lifecycle.md` requires such a class to be excluded mechanically. A worklist
    /// that is mostly noise gets ignored, and this check's one real finding would go with it.
    ///
    /// That the kernel handles absence *worse* than zero rows — it throws rather than parking —
    /// is true and is filed as a kernel defect. It is not reportable here without paying that cost.
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
