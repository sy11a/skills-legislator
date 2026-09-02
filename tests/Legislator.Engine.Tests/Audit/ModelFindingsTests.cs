using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// The model-findings channel: the only way a semantic judgement enters a report the engine
/// prints. It is validated strictly on purpose - a malformed channel that read as "no findings"
/// would turn the model's silence and the model's failure into the same report.
/// </summary>
public sealed class ModelFindingsTests
{
    private const string Path = AuditFixture.Root + "/mf.json";

    private static Legislator.Engine.JobResult With(string json)
    {
        var fs = AuditFixture.Repo();
        fs.AddFile(Path, new MockFileData(json));
        return AuditFixture.Audit(fs, null, "--model-findings", Path);
    }

    [Theory]
    [InlineData("{nope", "malformed")]
    [InlineData("[]", "wrong shape")]
    [InlineData("{\"findings\": {}}", "wrong shape")]
    [InlineData("{\"candidates\": {}}", "wrong shape")]
    [InlineData("{\"findings\": [{\"check\": \"x\", \"severity\": \"Loud\", \"line\": \"y\"}]}", "entry malformed")]
    [InlineData("{\"findings\": [{\"check\": \"x\", \"severity\": \"Info\"}]}", "entry malformed")]
    [InlineData("{\"findings\": [{\"severity\": \"Info\", \"line\": \"y\"}]}", "entry malformed")]
    public void Given_a_channel_that_does_not_hold_its_contract_When_audit_runs_Then_the_run_stops(string json, string _)
    {
        Assert.ThrowsAny<Exception>(() => With(json));
    }

    [Fact]
    public void Given_a_missing_channel_file_When_audit_runs_Then_the_run_stops()
    {
        var fs = AuditFixture.Repo();

        Assert.ThrowsAny<Exception>(() => AuditFixture.Audit(fs, null, "--model-findings", "/r/absent.json"));
    }

    [Fact]
    public void Given_a_finding_line_carrying_the_list_marker_and_its_slug_When_audit_runs_Then_neither_is_printed_twice()
    {
        var report = With(
            "{\"findings\": [{\"check\": \"project-rules\", \"severity\": \"Warning\", "
            + "\"line\": \"- [project-rules] .claude/rules/x.md: contradicts core/sdd.md\"}]}");

        Assert.Contains(
            "- [project-rules] .claude/rules/x.md: contradicts core/sdd.md",
            report.Stdout,
            StringComparison.Ordinal);
        Assert.DoesNotContain("[project-rules] [project-rules]", report.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("- - [", report.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_finding_that_escalates_an_engine_line_When_audit_runs_Then_the_engine_line_is_replaced()
    {
        var fs = AuditFixture.Repo(new() { [".scratch/notes.md"] = "x\n" });
        fs.AddFile(Path, new MockFileData(
            "{\"findings\": [{\"check\": \"foreign-structures\", \"severity\": \"Critical\", "
            + "\"escalates\": \".scratch/notes.md\", "
            + "\"line\": \"- [foreign-structures] .scratch/notes.md: carries live law\"}]}"));

        var report = AuditFixture.Audit(fs, null, "--model-findings", Path);

        Assert.Single(AuditFixture.Findings(report, "foreign-structures"));
        Assert.Contains("carries live law", report.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_verification_note_When_audit_runs_Then_it_is_printed_before_the_stamp()
    {
        var report = With("{\"findings\": [], \"candidates\": [], \"verification\": \"Zero writes confirmed.\"}");

        // The pinned tail, confirmed against the Python on the same fixture: a blank line, the
        // note, a blank line, then the stamp - so the note is the fourth line from the end.
        var lines = report.Stdout.Split('\n');
        Assert.Equal("Zero writes confirmed.", lines[^4]);
        Assert.Equal("", lines[^3]);
        Assert.StartsWith("Emitted by", lines[^2], StringComparison.Ordinal);
    }
}
