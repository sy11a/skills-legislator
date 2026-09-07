using System.IO.Abstractions.TestingHelpers;
using Xunit;
using static Legislator.Parity.Tests.Engine.RunJobs;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the report job answers (R-8206). The
/// ruler drives four trees through the whole pipeline - apply, scaffold, verify, report - and
/// reads the printed report; the twin drives the same four in the fake. The report is dated, so
/// every run here takes the fixture clock: a byte-stability check that races midnight measures
/// the calendar rather than the job.
/// </summary>
public sealed class ReportTwins
{
    private static (int Exit, string Out, string Err) Report(MockFileSystem fs, params string[] extra) =>
        Run(fs, "report", null, null, extra);

    private static List<string> Heads(string report) =>
        [.. report.Split('\n').Where(l => l.StartsWith("## ", StringComparison.Ordinal))];

    /// <summary>The ruler's `_sec(text, a, b)`: the slice of the report between two headings, empty when either is absent.</summary>
    private static string Section(string report, string from, string to)
    {
        if (!report.Contains(from, StringComparison.Ordinal))
        {
            return "";
        }

        var after = report.Split(from, 2)[1];
        return after.Contains(to, StringComparison.Ordinal) ? after.Split(to, 2)[0] : "";
    }

    /// <summary>The ruler's fresh tree: applied and scaffolded, then wired by hand, then verified - a scaffold report with nothing left to ask for.</summary>
    private static MockFileSystem Scaffolded()
    {
        var fs = Repo(new() { ["README.md"] = "r\n" });
        Run(fs, "apply", null, null, "--stacks", string.Empty);
        ScaffoldAll(fs);
        fs.File.WriteAllText($"{Root}/AGENTS.md", WiredEntry());
        Run(fs, "verify");
        return fs;
    }

    /// <summary>The ruler's upgrade tree: a stale owned file, an import that resolves to nothing, an owned rule nobody imports, one kept path and one refused.</summary>
    private static MockFileSystem Upgraded()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# P\n\n@docs/ai/rules/core/okf.md\n@docs/ai/rules/core/ghost.md\n",
            ["docs/notes/a.md"] = "a\n",
            ["docs/ai/rules/core/okf.md"] = "stale\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [], \"keep\": [], "
                + "\"ownedFiles\": [\"docs/ai/rules/core/okf.md\"]}",
        });
        Run(fs, "apply", null, null,
            "--stacks", string.Empty,
            "--keep-add", "docs/notes/a.md::notes",
            "--keep-add", "opencode.json::x");
        ScaffoldAll(fs);
        Run(fs, "verify");
        return fs;
    }

    /// <summary>The ruler's `root2`: an upgrade whose every one of audit checks 1-6 comes out clean, and whose keep list stayed empty.</summary>
    private static MockFileSystem HealthClean()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = WiredEntry(),
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
        });
        Run(fs, "apply", null, null, "--stacks", string.Empty);
        ScaffoldAll(fs);
        fs.File.WriteAllText($"{Root}/docs/okf/index.md", "# OKF\n\nSee `docs/okf/codebase-map.md`.\n");
        fs.File.WriteAllText($"{Root}/docs/okf/codebase-map.md", "# Map\n\n| Directory | What |\n|---|---|\n| `docs/` | docs |\n");
        Run(fs, "verify");
        return fs;
    }

    [Fact]
    [Parity("engine", "report_scaffold_title")]
    public void Report_scaffold_title()
    {
        var (exit, report, stderr) = Report(Scaffolded());

        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.StartsWith("# Legislator Scaffold — ", report, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_scaffold_sections_pinned_order_no_health_no_keep")]
    public void Report_scaffold_sections_pinned_order_no_health_no_keep()
    {
        var (_, report, _) = Report(Scaffolded());

        Assert.Equal(
            ["## Created", "## Overwritten", "## Deleted", "## Needs your review"],
            Heads(report));
    }

    [Fact]
    [Parity("engine", "report_created_lists_owned_files_and_step4_artifacts_from_snapshots")]
    public void Report_created_lists_owned_files_and_step4_artifacts_from_snapshots()
    {
        var (_, report, _) = Report(Scaffolded());
        var created = Section(report, "## Created", "## Overwritten");

        Assert.Contains("- `docs/ai/rules/core/okf.md`", created, StringComparison.Ordinal);
        Assert.Contains("- `docs/cases/README.md`", created, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_stamp_is_last_line")]
    public void Report_stamp_is_last_line()
    {
        var (_, report, _) = Report(Scaffolded());

        Assert.NotEqual("", report.Trim());
        Assert.Equal(
            $"Emitted by legislator report — constitution v{SkillVersion}.",
            report.TrimEnd('\n').Split('\n')[^1]);
    }

    /// <summary>
    /// Strengthened past the ruler, which compares two reports and is satisfied when both are
    /// empty - green against an empty implementation. The twin requires the report to exist
    /// before requiring it to be stable.
    /// </summary>
    [Fact]
    [Parity("engine", "report_byte_stable")]
    public void Report_byte_stable()
    {
        var fs = Scaffolded();

        var (exit, first, _) = Report(fs);
        var (_, second, _) = Report(fs);

        Assert.Equal(0, exit);
        Assert.StartsWith("# Legislator ", first, StringComparison.Ordinal);
        Assert.Equal(first, second);
    }

    [Fact]
    [Parity("engine", "report_upgrade_title")]
    public void Report_upgrade_title()
    {
        var (_, report, _) = Report(Upgraded());

        Assert.StartsWith("# Legislator Upgrade — ", report, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_upgrade_sections_with_keep_and_health")]
    public void Report_upgrade_sections_with_keep_and_health()
    {
        var (_, report, _) = Report(Upgraded());

        Assert.Equal(
            ["## Created", "## Overwritten", "## Deleted", "## Needs your review", "## Keep list", "## Health"],
            Heads(report));
    }

    [Fact]
    [Parity("engine", "report_overwritten_lists_changed_owned_file")]
    public void Report_overwritten_lists_changed_owned_file()
    {
        var (_, report, _) = Report(Upgraded());

        Assert.Contains(
            "- `docs/ai/rules/core/okf.md`",
            Section(report, "## Overwritten", "## Deleted"),
            StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_review_carries_import_deltas_and_scaffold_wiring")]
    public void Report_review_carries_import_deltas_and_scaffold_wiring()
    {
        var (_, report, _) = Report(Upgraded());
        var review = Section(report, "## Needs your review", "## Keep list");

        Assert.Contains("@docs/ai/rules/core/sdd.md", review, StringComparison.Ordinal);
        Assert.Contains("remove", review, StringComparison.Ordinal);
        Assert.Contains("@docs/ai/rules/core/ghost.md", review, StringComparison.Ordinal);
        Assert.Contains("@docs/okf/codebase-map.md", review, StringComparison.Ordinal);
        Assert.Contains("## Boundaries", review, StringComparison.Ordinal);
        Assert.Contains("docs/okf/glossary.md", review, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_keep_list_added_and_refused")]
    public void Report_keep_list_added_and_refused()
    {
        var (_, report, _) = Report(Upgraded());
        var keep = Section(report, "## Keep list", "## Health");

        Assert.Contains("docs/notes/a.md", keep, StringComparison.Ordinal);
        Assert.Contains("opencode.json", keep, StringComparison.Ordinal);
        Assert.Contains("owned", keep, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_health_runs_audit_checks_1_to_6")]
    public void Report_health_runs_audit_checks_1_to_6()
    {
        var (_, report, _) = Report(Upgraded());
        var health = report.Split("## Health", 2)[1];

        Assert.Contains("[imports-resolve]", health, StringComparison.Ordinal);
        Assert.Contains("ghost.md", health, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_health_clean_and_no_keep_section_without_delta")]
    public void Report_health_clean_and_no_keep_section_without_delta()
    {
        var (_, report, _) = Report(HealthClean());

        Assert.Contains("Health: clean", report, StringComparison.Ordinal);
        Assert.DoesNotContain("## Keep list", report, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "report_entry_document_listed_once_by_its_file_model_event")]
    public void Report_entry_document_listed_once_by_its_file_model_event()
    {
        var fs = Repo(new()
        {
            ["CLAUDE.md"] = "# Real\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
        });
        Run(fs, "apply", null, null, "--stacks", string.Empty);
        ScaffoldAll(fs);
        Run(fs, "verify");

        var (_, report, _) = Report(fs);
        var created = Section(report, "## Created", "## Overwritten");

        Assert.Equal(1, created.Split("- `AGENTS.md`").Length - 1);
        Assert.Contains("renamed from CLAUDE.md", created, StringComparison.Ordinal);
        Assert.Equal(1, created.Split("- `CLAUDE.md`").Length - 1);
    }

    [Fact]
    [Parity("engine", "report_merges_candidates_and_review_lines_into_pinned_sections")]
    public void Report_merges_candidates_and_review_lines_into_pinned_sections()
    {
        var fs = HealthClean();
        fs.File.WriteAllText(
            $"{Root}/mf.json",
            "{\"candidates\": [\"- \\\"Always deploy on Fridays.\\\" — AGENTS.md\"], "
            + "\"review\": [\"- remove `docs/superpowers/` from .gitignore\"]}");

        var (_, report, _) = Report(fs, "--model-findings", $"{Root}/mf.json");

        Assert.Equal(
            ["## Created", "## Overwritten", "## Deleted", "## Needs your review", "## Constitution candidates", "## Health"],
            Heads(report));
        Assert.Contains(
            "Always deploy on Fridays",
            Section(report, "## Constitution candidates", "## Health"),
            StringComparison.Ordinal);
        Assert.Contains(
            ".gitignore",
            Section(report, "## Needs your review", "## Constitution"),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Strengthened past the ruler, whose "not 0 or 1 with something on stderr" is satisfied by
    /// the usage exit an unimplemented job gives - green against an empty implementation. The
    /// twin names the exit the host renders for a job that threw, and requires the reason to
    /// name the file it could not read.
    /// </summary>
    [Fact]
    [Parity("engine", "report_malformed_findings_is_a_loud_exit")]
    public void Report_malformed_findings_is_a_loud_exit()
    {
        var fs = HealthClean();
        fs.File.WriteAllText($"{Root}/mf.json", "{\"candidates\": \"nope\"}");

        var (exit, report, stderr) = Report(fs, "--model-findings", $"{Root}/mf.json");

        Assert.Equal(3, exit);
        Assert.Equal("", report);
        Assert.Contains("mf.json", stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// Strengthened past the ruler, whose "the heading is absent" holds of an empty stdout -
    /// green against an empty implementation. The twin requires a real scaffold report first,
    /// and only then that the candidates the model supplied are not in it.
    /// </summary>
    [Fact]
    [Parity("engine", "report_scaffold_never_prints_candidates")]
    public void Report_scaffold_never_prints_candidates()
    {
        var fs = Scaffolded();
        fs.File.WriteAllText($"{Root}/mf.json", "{\"candidates\": [\"- x\"]}");

        var (exit, report, _) = Report(fs, "--model-findings", $"{Root}/mf.json");

        Assert.Equal(0, exit);
        Assert.StartsWith("# Legislator Scaffold — ", report, StringComparison.Ordinal);
        Assert.DoesNotContain("## Constitution candidates", report, StringComparison.Ordinal);
    }
}
