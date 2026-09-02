using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Engine;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// The emitter. The report is a pinned document, not a log: the model reads it, the user pastes
/// it, and a run over an unchanged repository has to produce the same bytes twice. What is
/// tested here is the shape - the header, the ordering, the clean-checks line and the stamp.
/// </summary>
public sealed class AuditReportTests
{
    private static readonly DateTimeOffset Day = new(2026, 7, 4, 9, 0, 0, TimeSpan.Zero);

    private static JobResult Report(IFileSystem fs, params string[] extra) =>
        new AuditJob().Run(new JobContext(
            fs, new FixedTimeProvider(Day), new FakeEnvironment(), AuditFixture.NoRepo(),
            new LegislatorOptions(), AuditFixture.Root, ["--skill", AuditFixture.SkillPath, .. extra]));

    private static string[] Lines(JobResult report) => report.Stdout.Split('\n');

    [Fact]
    public void Given_a_clean_repository_When_audit_runs_Then_the_report_is_the_pinned_clean_shape()
    {
        var report = Report(AuditFixture.Repo());

        var lines = Lines(report);
        Assert.Equal("# AI-Layer Audit — r, 2026-07-04", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("Constitution: v25 (skill source: v25) — up to date", lines[2]);
        Assert.Equal("", lines[3]);
        Assert.Equal("No findings.", lines[4]);
        Assert.Equal(0, report.ExitCode);
    }

    [Fact]
    public void Given_any_repository_When_audit_runs_Then_the_last_line_is_the_emitter_stamp()
    {
        var report = Report(AuditFixture.Repo());

        var lines = Lines(report);
        Assert.Equal("Emitted by docs/ai/engine.py audit — constitution v25.", lines[^2]);
        Assert.Equal("", lines[^1]);
    }

    [Fact]
    public void Given_no_model_findings_file_When_audit_runs_Then_the_clean_line_omits_the_model_checks_and_says_so()
    {
        var report = Report(AuditFixture.Repo());

        var clean = Lines(report).Single(l => l.StartsWith("Clean checks:", StringComparison.Ordinal));
        Assert.DoesNotContain("project-rules", clean, StringComparison.Ordinal);
        Assert.DoesNotContain("stray-rulebooks", clean, StringComparison.Ordinal);
        Assert.Contains(
            Lines(report),
            l => l.StartsWith("Model checks (project-rules, stray-rulebooks, constitution candidates): not supplied", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_a_model_findings_file_When_audit_runs_Then_the_model_checks_join_the_clean_line()
    {
        var fs = AuditFixture.Repo();
        fs.AddFile($"{AuditFixture.Root}/mf.json", new MockFileData("{\"findings\": [], \"candidates\": []}"));

        var report = Report(fs, "--model-findings", $"{AuditFixture.Root}/mf.json");

        var clean = Lines(report).Single(l => l.StartsWith("Clean checks:", StringComparison.Ordinal));
        Assert.Contains("project-rules", clean, StringComparison.Ordinal);
        Assert.Contains("stray-rulebooks", clean, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(report), l => l.StartsWith("Model checks (", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_findings_of_several_checks_When_audit_runs_Then_they_are_ordered_by_the_pinned_check_order()
    {
        var report = Report(AuditFixture.Repo(new()
        {
            ["docs/okf/index.md"] = "# OKF\n\nSee `docs/okf/codebase-map.md` and [gone](gone.md).\n",
            ["docs/okf/orphan.md"] = "# Orphan\n",
        }));

        var warnings = Lines(report)
            .SkipWhile(l => l != "## Warning").Skip(1).TakeWhile(l => l.StartsWith("- ", StringComparison.Ordinal))
            .Select(l => l.Split(']')[0] + "]").ToList();
        Assert.Equal(warnings.Distinct(), warnings.Distinct());
        Assert.True(
            warnings.IndexOf("- [okf-index-links]") < warnings.IndexOf("- [orphan-docs]"),
            $"check 5 must print before check 7, got {string.Join(", ", warnings)}");
    }

    [Fact]
    public void Given_only_info_findings_When_audit_runs_Then_the_empty_severity_sections_are_omitted()
    {
        var report = Report(AuditFixture.Repo(new() { [".scratch/notes.md"] = "x\n" }));

        Assert.DoesNotContain("## Critical", report.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("## Warning", report.Stdout, StringComparison.Ordinal);
        Assert.Contains("## Info", report.Stdout, StringComparison.Ordinal);
        Assert.Equal(1, report.ExitCode);
    }

    [Fact]
    public void Given_a_dirty_check_When_audit_runs_Then_it_is_absent_from_the_clean_line()
    {
        var report = Report(AuditFixture.Repo(new() { ["docs/okf/orphan.md"] = "# Orphan\n" }));

        var clean = Lines(report).Single(l => l.StartsWith("Clean checks:", StringComparison.Ordinal));
        Assert.DoesNotContain("orphan-docs", clean, StringComparison.Ordinal);
        Assert.Contains("imports-resolve", clean, StringComparison.Ordinal);
    }
}
