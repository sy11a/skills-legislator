using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Engine;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the sdd-lint job answers (R-8206) —
/// the analyze gate's mechanical passes: coverage, dangling references, unresolved
/// placeholders, and the case, ADR, journal, changelog and OKF shapes. The fixtures are the
/// ruler's own `make_case_repo` and `case_repo`, materialised in memory.
/// </summary>
public sealed class SddLintTwins
{
    /// <summary>
    /// The traceability marker, assembled rather than written: `core/artifact-lifecycle.md`
    /// annotates a test by the literal `per R-NNN` it carries, and a fixture that spelled the
    /// marker whole would enter the baseline as an annotation for requirements it does not
    /// test - a false green in the register a human reads.
    /// </summary>
    private const string Per = "per ";

    private const string Tier1Head = "# BL-900 — test\n\n**Tier: 1 (light).** x\n\n**Spec type: feature.** y\n\n";
    private const string Boundary = "## Boundary\n\n**In:** a thing.\n\n**Out:** another thing (out of scope).\n\n";
    private const string Hurt = "## The hurting case\n\nGIVEN a repo, WHEN it runs, THEN it works.\n\n";
    private const string Clarifications = "## Clarifications\n\n- **Q: x?** -> y.\n\n";
    private const string Requirement = "## Requirements\n\n- **R-901** — WHEN x happens the tool SHALL do y.\n\n";
    private const string GoodSpec = Tier1Head + Requirement + Boundary + Hurt + Clarifications;

    private const string AdrOk =
        "# 0001. Record decisions\n\n## Status\n\naccepted\n\n## Context\n\nx\n\n## Decision\n\ny\n\n## Consequences\n\nz\n";

    /// <summary>The ruler's `make_repo`: an empty OKF bundle unless documents are named, sources at their own paths.</summary>
    private static MockFileSystem Repo(Dictionary<string, string>? docs = null, Dictionary<string, string>? files = null)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/okf");
        foreach (var (name, text) in docs ?? [])
        {
            fs.AddFile($"/r/docs/okf/{name}", new MockFileData(text));
        }

        foreach (var (rel, text) in files ?? [])
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        return fs;
    }

    /// <summary>The ruler's `case_repo`: one case directory, its spec written only when there is one.</summary>
    private static MockFileSystem CaseRepo(
        string? spec = null, string name = "BL-900-test-case", Dictionary<string, string>? extra = null)
    {
        var files = new Dictionary<string, string>(extra ?? []);
        if (spec is not null)
        {
            files[$"docs/cases/{name}/spec.md"] = spec;
        }

        return Repo(files: files);
    }

    /// <summary>
    /// The ruler's `make_case_repo`: spec defines R-001..R-003, the plan traces R-001 and
    /// R-002 and dangles on R-999, the test tree carries R-001's marker only, and a bare
    /// token sits in notes.md while a backticked one in the spec is quotation.
    /// </summary>
    private static MockFileSystem CaseTree() => Repo(files: new()
    {
        ["docs/cases/BL-001-widget-flow/spec.md"] =
            "# BL-001 — widget flow\n\n"
            + "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
            + "Prose may quote a template token like `{{PROJECT_NAME}}` safely.\n\n"
            + "### R-001 — widgets persist\n\nWHEN a widget is saved THEN it SHALL persist.\n\n"
            + "### R-002 — widgets list\n\nWHEN listed THEN widgets SHALL appear.\n\n"
            + "### R-003 — widgets delete\n\nWHEN deleted THEN widgets SHALL disappear.\n",
        ["docs/cases/BL-001-widget-flow/plan.md"] =
            "# plan\n\n1. store layer, " + Per + "R-001\n2. list endpoint, " + Per + "R-002\n3. cleanup, " + Per + "R-999\n",
        ["docs/cases/BL-001-widget-flow/notes.md"] = "# notes\n\nLeft behind: {{PROJECT_OVERVIEW}}\n",
        ["src/App.Tests/WidgetStoreTests.cs"] = "// " + Per + "R-001\npublic class WidgetStoreTests { }\n",
    });

    /// <summary>The ruler's `run(root, "sdd-lint")`, in process: the same argv the binary is given.</summary>
    private static (int Exit, string Out, string Err) SddLint(IFileSystem fs)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["sdd-lint", "--root", "/r"], JobRegistry.Jobs, fs, TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    [Parity("engine", "sdd_lint_findings_exit_1")]
    public void Sdd_lint_findings_exit_1() => Assert.Equal(1, SddLint(CaseTree()).Exit);

    [Fact]
    [Parity("engine", "sdd_lint_dangling_reference_named")]
    public void Sdd_lint_dangling_reference_named()
    {
        var (_, output, _) = SddLint(CaseTree());

        Assert.Contains("R-999", output, StringComparison.Ordinal);
        Assert.Contains("dangling", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "sdd_lint_uncovered_requirement_named")]
    public void Sdd_lint_uncovered_requirement_named()
    {
        var (_, output, _) = SddLint(CaseTree());

        Assert.Contains("R-003", output, StringComparison.Ordinal);
        Assert.Contains("uncovered", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "sdd_lint_bare_token_reported")]
    public void Sdd_lint_bare_token_reported()
    {
        var (_, output, _) = SddLint(CaseTree());

        Assert.Contains("notes.md", output, StringComparison.Ordinal);
        Assert.Contains("{{PROJECT_OVERVIEW}}", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "sdd_lint_quoted_token_exempt: a backticked token is quotation, not a placeholder")]
    public void A_backticked_token_is_quotation_not_a_placeholder()
    {
        var (exit, output, _) = SddLint(CaseTree());

        Assert.Equal(1, exit);
        Assert.DoesNotContain("{{PROJECT_NAME}}", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "sdd_lint_covered_requirement_silent")]
    public void Sdd_lint_covered_requirement_silent()
    {
        var (exit, output, _) = SddLint(CaseTree());

        Assert.Equal(1, exit);
        Assert.DoesNotContain("uncovered: R-001", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dangling: R-001", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "sdd_lint_planless_case_clean: tier 0/1 is lawful — no plan, no coverage findings")]
    public void A_case_without_a_plan_yields_no_coverage_findings()
    {
        var fs = CaseTree();
        fs.RemoveFile("/r/docs/cases/BL-001-widget-flow/plan.md");
        fs.RemoveFile("/r/docs/cases/BL-001-widget-flow/notes.md");

        var (exit, output, _) = SddLint(fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "sdd_lint_clean_exit_0")]
    public void Sdd_lint_clean_exit_0()
    {
        var fs = CaseTree();
        fs.RemoveFile("/r/docs/cases/BL-001-widget-flow/notes.md");
        fs.File.WriteAllText(
            "/r/docs/cases/BL-001-widget-flow/plan.md",
            "# plan\n\n1. store, " + Per + "R-001\n2. list, " + Per + "R-002\n3. delete, " + Per + "R-003\n");

        var (exit, output, _) = SddLint(fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "lint_tier_and_type (control): a headered spec is silent")]
    public void A_headered_spec_is_silent() => Assert.Equal(0, SddLint(CaseRepo(GoodSpec)).Exit);

    [Fact]
    [Parity("engine", "lint_tier_required: missing tier is a finding")]
    public void Missing_tier_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec.Replace("**Tier: 1 (light).** x\n\n", "", StringComparison.Ordinal)));

        Assert.Equal(1, exit);
        Assert.Contains("tier", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_type_required: missing spec type is a finding")]
    public void Missing_spec_type_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec.Replace("**Spec type: feature.** y\n\n", "", StringComparison.Ordinal)));

        Assert.Equal(1, exit);
        Assert.Contains("spec type", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_specless_case_clean: tier-0 direct case stays silent")]
    public void A_case_with_no_spec_at_all_stays_silent() =>
        Assert.Equal(0, SddLint(CaseRepo(extra: new() { ["docs/cases/BL-901-direct/summary.md"] = "# done\n" })).Exit);

    [Fact]
    [Parity("engine", "lint_bugfix (control): current/expected/unchanged present is silent")]
    public void A_bugfix_spec_stating_all_three_behaviours_is_silent()
    {
        var bugfix = GoodSpec.Replace("**Spec type: feature.**", "**Spec type: bugfix.**", StringComparison.Ordinal);

        Assert.Equal(0, SddLint(CaseRepo(bugfix + "## Behavior\n\nCurrent behavior: a. Expected behavior: b. Unchanged: c.\n")).Exit);
    }

    [Fact]
    [Parity("engine", "lint_bugfix_sections: missing unchanged statement is a finding")]
    public void A_bugfix_spec_missing_the_unchanged_statement_is_a_finding()
    {
        var bugfix = GoodSpec.Replace("**Spec type: feature.**", "**Spec type: bugfix.**", StringComparison.Ordinal);

        var (exit, output, _) = SddLint(CaseRepo(bugfix));

        Assert.Equal(1, exit);
        Assert.Contains("unchanged", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_boundary_required: missing boundary is a finding")]
    public void A_tier_one_spec_without_a_boundary_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(Tier1Head + Requirement + Hurt + Clarifications));

        Assert.Equal(1, exit);
        Assert.Contains("boundary", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_hurting_case_required: missing GIVEN/WHEN/THEN is a finding")]
    public void A_tier_one_spec_without_a_hurting_case_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(Tier1Head + Requirement + Boundary + Clarifications));

        Assert.Equal(1, exit);
        Assert.True(
            output.Contains("hurting", StringComparison.OrdinalIgnoreCase) || output.Contains("GIVEN", StringComparison.Ordinal),
            output);
    }

    [Fact]
    [Parity("engine", "lint_ears_two_shalls: two SHALLs on one R-line is a finding")]
    public void Two_shalls_on_one_requirement_line_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(
            GoodSpec.Replace("the tool SHALL do y.", "the tool SHALL do y and SHALL do z.", StringComparison.Ordinal)));

        Assert.Equal(1, exit);
        Assert.Contains("SHALL", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "lint_ears_no_shall: an R-line with no SHALL is a finding")]
    public void A_requirement_line_with_no_shall_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(
            GoodSpec.Replace("the tool SHALL do y.", "the tool does y.", StringComparison.Ordinal)));

        Assert.Equal(1, exit);
        Assert.Contains("SHALL", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "lint_ears_quoted_shall (control): backticked SHALL is quotation")]
    public void A_backticked_shall_is_quotation() =>
        Assert.Equal(0, SddLint(CaseRepo(GoodSpec.Replace(
            "the tool SHALL do y.", "the tool SHALL do y (never write `SHALL SHALL` bare).", StringComparison.Ordinal))).Exit);

    [Fact]
    [Parity("engine", "lint_clarifications_required: missing session is a finding")]
    public void A_tier_one_spec_without_clarifications_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(Tier1Head + Requirement + Boundary + Hurt));

        Assert.Equal(1, exit);
        Assert.Contains("clarification", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_adr (control): well-shaped ADR + template are silent")]
    public void A_well_shaped_adr_and_its_template_are_silent() =>
        Assert.Equal(0, SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["docs/adr/0001-record.md"] = AdrOk,
            ["docs/adr/template.md"] = "{{TITLE}} skeleton\n",
        })).Exit);

    [Fact]
    [Parity("engine", "lint_adr_sequence_gap: 0002 missing is a finding")]
    public void A_gap_in_the_adr_sequence_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["docs/adr/0001-record.md"] = AdrOk,
            ["docs/adr/0003-gap.md"] = AdrOk.Replace("0001.", "0003.", StringComparison.Ordinal),
        }));

        Assert.Equal(1, exit);
        Assert.Contains("sequence", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_adr_status_closed_set: unknown status is a finding")]
    public void An_adr_status_outside_the_closed_set_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["docs/adr/0001-record.md"] = AdrOk.Replace("accepted", "done-ish", StringComparison.Ordinal),
        }));

        Assert.Equal(1, exit);
        Assert.Contains("status", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_adr_missing_section: absent Consequences is a finding")]
    public void An_adr_without_its_consequences_section_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["docs/adr/0001-record.md"] = AdrOk.Replace("## Consequences\n\nz\n", "", StringComparison.Ordinal),
        }));

        Assert.Equal(1, exit);
        Assert.Contains("consequences", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_journal (control): dated file + README are silent")]
    public void A_dated_journal_file_and_its_readme_are_silent() =>
        Assert.Equal(0, SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["docs/journal/2026-08-26.md"] = "# day\n",
            ["docs/journal/README.md"] = "# how\n",
        })).Exit);

    [Fact]
    [Parity("engine", "lint_journal_filename: a stray name is a finding")]
    public void A_journal_file_that_is_not_a_day_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec, extra: new() { ["docs/journal/notes.md"] = "# stray\n" }));

        Assert.Equal(1, exit);
        Assert.Contains("journal", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_changelog (control): Unreleased present is silent")]
    public void A_changelog_carrying_unreleased_is_silent() =>
        Assert.Equal(0, SddLint(CaseRepo(GoodSpec, extra: new()
        {
            ["CHANGELOG.md"] = "# Changelog\n\n## [Unreleased]\n",
        })).Exit);

    [Fact]
    [Parity("engine", "lint_changelog_unreleased: missing heading is a finding")]
    public void A_changelog_without_unreleased_is_a_finding()
    {
        var (exit, output, _) = SddLint(CaseRepo(GoodSpec, extra: new() { ["CHANGELOG.md"] = "# Changelog\n\nstuff\n" }));

        Assert.Equal(1, exit);
        Assert.Contains("unreleased", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_okf_status (control): implemented is silent")]
    public void An_okf_document_declaring_implemented_is_silent() =>
        Assert.Equal(0, SddLint(Repo(
            new() { ["widgets.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\nx\n" },
            new() { ["docs/cases/BL-900-t/spec.md"] = GoodSpec })).Exit);

    [Fact]
    [Parity("engine", "lint_okf_status_closed_set: 'shipped' is a finding")]
    public void An_okf_status_outside_the_closed_set_is_a_finding()
    {
        var (exit, output, _) = SddLint(Repo(
            new() { ["widgets.md"] = "---\ntype: Concept\nstatus: shipped\n---\n\nx\n" },
            new() { ["docs/cases/BL-900-t/spec.md"] = GoodSpec }));

        Assert.Equal(1, exit);
        Assert.Contains("status", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_okf_status_human_exempt: glossary.md is never linted")]
    public void The_human_class_is_never_linted_for_status() =>
        Assert.Equal(0, SddLint(Repo(
            new() { ["glossary.md"] = "---\ntype: System\nstatus: whatever\n---\n\nterms\n" },
            new() { ["docs/cases/BL-900-t/spec.md"] = GoodSpec })).Exit);

    [Fact]
    [Parity("engine", "lint_quoted_converge_marker_not_a_closure: an inline mention does not exempt the case")]
    public void A_quoted_converge_marker_does_not_close_the_case()
    {
        var quoted = Tier1Head + Requirement + Boundary + Hurt + Clarifications
            + "Note: the \"✅ Converged\" close marker binds a closing act.\n";

        var (exit, output, _) = SddLint(CaseRepo(quoted.Replace(Clarifications, "", StringComparison.Ordinal)));

        Assert.Equal(1, exit);
        Assert.Contains("clarification", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "lint_standalone_converge_marker_closes (control): the standalone line still exempts")]
    public void A_standalone_converge_marker_still_exempts_the_case()
    {
        var quoted = Tier1Head + Requirement + Boundary + Hurt + Clarifications
            + "Note: the \"✅ Converged\" close marker binds a closing act.\n";

        Assert.Equal(0, SddLint(CaseRepo(quoted + "\n✅ Converged\n")).Exit);
    }

    [Fact]
    [Parity("engine", "lint_converged_case_exempt: history is never re-linted")]
    public void A_converged_case_is_never_re_linted() =>
        Assert.Equal(0, SddLint(CaseRepo(
            Tier1Head.Replace("**Tier: 1 (light).** x\n\n", "", StringComparison.Ordinal) + "done\n\n✅ Converged\n")).Exit);

    /// <summary>
    /// The ruler asserts an absence — the file set is the same before and after — which an
    /// engine that does not know the job satisfies for the wrong reason. The twin asserts the
    /// findings that prove the job actually ran beside the silence of the tree (T-07's pattern).
    /// </summary>
    [Fact]
    [Parity("engine", "sdd_lint_writes_nothing")]
    public void Sdd_lint_writes_nothing()
    {
        var fs = CaseTree();
        var before = fs.AllFiles.Order(StringComparer.Ordinal).ToList();

        var (exit, _, _) = SddLint(fs);

        Assert.Equal(1, exit);
        Assert.Equal(before, fs.AllFiles.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    [Parity("engine", "sdd_lint_accepts_three_definition_forms_and_list_refs")]
    public void All_three_definition_forms_and_the_list_reference_parse()
    {
        var (exit, output, _) = SddLint(DefinitionForms());

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    /// <summary>The ruler's `BL-002-forms`: the heading, bullet and bare definition forms, traced by one list reference.</summary>
    internal static MockFileSystem DefinitionForms() => Repo(files: new()
    {
        ["docs/cases/BL-002-forms/spec.md"] =
            "# BL-002 — forms\n\n"
            + "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
            + "### R-001 — heading form\n\nWHEN a THEN b SHALL c.\n\n"
            + "- **R-002** — bullet form: the store SHALL persist `docs/x.md` rows.\n\n"
            + "R-003 — bare form SHALL hold.\n",
        ["docs/cases/BL-002-forms/plan.md"] = "# plan\n\n1. all of it, " + Per + "R-001, R-002, R-003\n",
    });

    [Fact]
    [Parity("engine", "sdd_lint_cross_case_reference_resolves: a rider may trace a sibling case's requirement")]
    public void A_rider_may_trace_a_sibling_cases_requirement()
    {
        var fs = Repo(files: new()
        {
            ["docs/cases/BL-003-owner/spec.md"] =
                "# BL-003-owner\n\n**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
                + "### R-001 — owner req\n\na SHALL b.\n",
            ["docs/cases/BL-004-rider/spec.md"] =
                "# BL-004-rider\n\n**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
                + "# rider\n\n(no requirements of its own)\n",
            ["docs/cases/BL-004-rider/plan.md"] = "# plan\n\n1. fix the sibling too, " + Per + "R-001\n",
        });

        var (exit, output, _) = SddLint(fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "sdd_lint_converged_case_skipped: completed lifecycle artifacts are never re-judged")]
    public void A_converged_case_carrying_findings_is_skipped()
    {
        var fs = CaseTree();
        fs.AddFile("/r/docs/cases/BL-001-widget-flow/summary.md", new MockFileData("# done\n\n✅ Converged.\n"));

        var (exit, output, _) = SddLint(fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }
}
