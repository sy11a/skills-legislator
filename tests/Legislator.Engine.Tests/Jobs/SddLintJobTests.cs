using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The sdd-lint job, branch by branch — the analyze gate's mechanical passes. Where the twins
/// assert what the ruler asserts, these assert the finding text itself: a lint whose wording
/// drifts is a lint whose findings stop being greppable by the people who act on them.
/// </summary>
public sealed class SddLintJobTests
{
    /// <summary>
    /// The traceability marker, assembled rather than written: `core/artifact-lifecycle.md`
    /// annotates a test by the literal `per R-NNN` it carries, and a fixture that spelled the
    /// marker whole would enter the baseline as an annotation for requirements it does not
    /// test - a false green in the register a human reads.
    /// </summary>
    private const string Per = "per ";

    private const string Tier0Head = "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n";

    private static MockFileSystem Repo(Dictionary<string, string> files)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/okf");
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        return fs;
    }

    private static JobResult Run(IFileSystem fs, LegislatorOptions? options = null) =>
        new SddLintJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
            options ?? new LegislatorOptions(), "/r", []));

    private static JobResult Run(Dictionary<string, string> files) => Run(Repo(files));

    [Fact]
    public void A_repository_with_no_cases_directory_is_clean()
    {
        var result = Run(new Dictionary<string, string> { ["README.md"] = "# hi\n" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_dangling_reference_names_the_id_and_where_a_definition_would_live()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}### R-001 — a\n\nx SHALL y.\n",
            ["docs/cases/BL-001-x/plan.md"] = "# plan\n\n1. a, " + Per + "R-001\n2. b, " + Per + "R-999\n",
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/cases/BL-001-x/plan.md: dangling: " + Per + "R-999 resolves to no EARS definition in any docs/cases/*/spec.md\n",
            result.Stdout);
    }

    [Fact]
    public void An_uncovered_requirement_names_the_plan_that_should_trace_it()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] =
                $"# BL-001\n\n{Tier0Head}### R-001 — a\n\nx SHALL y.\n\n### R-002 — b\n\nx SHALL z.\n",
            ["docs/cases/BL-001-x/plan.md"] = "# plan\n\n1. a, " + Per + "R-001\n",
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/cases/BL-001-x/plan.md: uncovered: R-002 has no per-R-002 task\n",
            result.Stdout);
    }

    [Fact]
    public void An_unresolved_placeholder_names_the_token()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/cases/BL-001-x/notes.md"] = "# notes\n\nLeft behind: {{PROJECT_OVERVIEW}}\n",
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/cases/BL-001-x/notes.md: unresolved-placeholder: {{PROJECT_OVERVIEW}}\n",
            result.Stdout);
    }

    /// <summary>Ids are unique within a case only: the same R-001 in two cases is two requirements, and each is covered by its own plan.</summary>
    [Fact]
    public void The_same_id_in_two_cases_is_two_requirements()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}### R-001 — a\n\nx SHALL y.\n",
            ["docs/cases/BL-001-x/plan.md"] = "# plan\n\n1. a, " + Per + "R-001\n",
            ["docs/cases/BL-002-y/spec.md"] = $"# BL-002\n\n{Tier0Head}### R-001 — b\n\nx SHALL z.\n",
            ["docs/cases/BL-002-y/plan.md"] = "# plan\n\n1. b, " + Per + "R-001\n",
        });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>`docs/superpowers/**` is retired history and never enters a lint pass — the scope is the cases directory alone.</summary>
    [Fact]
    public void Only_the_cases_directory_is_linted()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/superpowers/specs/old.md"] = "# old\n\n1. a, " + Per + "R-999\n\nLeft: {{TOKEN}}\n",
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
        });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void Findings_are_sorted_ordinally()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/cases/BL-001-x/b.md"] = "{{BETA}}\n",
            ["docs/cases/BL-001-x/a.md"] = "{{ALPHA}}\n",
        });

        Assert.Equal(
            "docs/cases/BL-001-x/a.md: unresolved-placeholder: {{ALPHA}}\n"
            + "docs/cases/BL-001-x/b.md: unresolved-placeholder: {{BETA}}\n",
            result.Stdout);
    }

    /// <summary>The closing verdict counts only when a line starts with it; a spec quoting the marker mid-sentence is explaining it, not closing itself (BL-057).</summary>
    [Theory]
    [InlineData("✅ Converged\n", 0)]
    [InlineData("  **✅ Converged** 2026-01-01.\n", 0)]
    [InlineData("The \"✅ Converged\" marker closes a case.\n", 1)]
    public void The_converge_marker_closes_a_case_only_on_its_own_line(string tail, int expected)
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}{tail}",
            ["docs/cases/BL-001-x/notes.md"] = "{{TOKEN}}\n",
        });

        Assert.Equal(expected, result.ExitCode);
    }

    [Fact]
    public void The_adr_template_is_not_an_adr()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/adr/template.md"] = "{{TITLE}}\n\nskeleton with no sections\n",
        });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void An_adr_sequence_gap_names_the_missing_number()
    {
        const string adr = "# 0001\n\n## Status\n\naccepted\n\n## Context\n\nx\n\n## Decision\n\ny\n\n## Consequences\n\nz\n";

        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/adr/0001-a.md"] = adr,
            ["docs/adr/0003-c.md"] = adr,
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/adr/: sequence gap — 0002 is missing → ADRs are numbered gaplessly, never renumbered\n",
            result.Stdout);
    }

    [Fact]
    public void A_superseded_by_status_is_inside_the_closed_set()
    {
        var result = Run(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/adr/0001-a.md"] =
                "# 0001\n\n## Status\n\nsuperseded by 0002\n\n## Context\n\nx\n\n## Decision\n\ny\n\n## Consequences\n\nz\n",
            ["docs/adr/0002-b.md"] =
                "# 0002\n\n## Status\n\naccepted\n\n## Context\n\nx\n\n## Decision\n\ny\n\n## Consequences\n\nz\n",
        });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_repository_with_no_changelog_is_not_a_finding()
    {
        var result = Run(new Dictionary<string, string> { ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>Every directory the tree lints is named by the options model, never by a literal (R-8209).</summary>
    [Fact]
    public void The_linted_directories_come_from_the_options_model()
    {
        var options = new LegislatorOptions
        {
            CasesDir = new("tickets", OptionsLayer.Instance),
            JournalDir = new("diary", OptionsLayer.Instance),
        };

        var result = Run(
            Repo(new Dictionary<string, string>
            {
                ["docs/tickets/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
                ["docs/tickets/BL-001-x/notes.md"] = "{{TOKEN}}\n",
                ["docs/diary/stray.md"] = "# stray\n",
            }),
            options);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("docs/tickets/BL-001-x/notes.md: unresolved-placeholder: {{TOKEN}}", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("docs/diary/stray.md: journal file is not YYYY-MM-DD.md", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void The_job_writes_nothing()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/cases/BL-001-x/spec.md"] = $"# BL-001\n\n{Tier0Head}",
            ["docs/cases/BL-001-x/notes.md"] = "{{TOKEN}}\n",
        });
        var before = fs.AllFiles.Order(StringComparer.Ordinal).ToList();

        Run(fs);

        Assert.Equal(before, fs.AllFiles.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void The_job_is_named_sdd_lint() => Assert.Equal("sdd-lint", new SddLintJob().Name);
}
