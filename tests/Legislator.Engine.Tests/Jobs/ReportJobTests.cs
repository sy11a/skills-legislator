using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The report job's branches the ruler never reaches: the third title, the Deleted section, and
/// the promise that a job which only reads leaves the tree exactly as it found it. The pinned
/// section order, the Health gating and the model-findings channel are the parity twins'.
/// </summary>
public sealed class ReportJobTests
{
    [Fact]
    public void Given_a_migration_run_When_the_report_is_emitted_Then_it_is_titled_Migration()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        RunApply(fs, null, "--stacks", string.Empty);

        var result = RunReport(fs);

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("# Legislator Migration — ", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_run_that_retired_an_owned_file_When_the_report_is_emitted_Then_the_Deleted_section_names_it()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# A\n",
            ["docs/ai/rules/core/retired.md"] = "old\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [], \"keep\": [], "
                + "\"ownedFiles\": [\"docs/ai/rules/core/retired.md\"]}",
        });
        RunApply(fs, null, "--stacks", string.Empty);

        var report = RunReport(fs).Stdout;
        var deleted = report.Split("## Deleted", 2)[1].Split("## Needs your review", 2)[0];

        Assert.Contains("- `docs/ai/rules/core/retired.md`", deleted, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_no_deletions_When_the_report_is_emitted_Then_the_empty_section_says_none()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        RunApply(fs, null, "--stacks", string.Empty);

        var report = RunReport(fs).Stdout;

        Assert.Contains("## Deleted\n- none\n", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_report_run_When_it_finishes_Then_the_tree_is_exactly_as_it_was()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        RunApply(fs, null, "--stacks", string.Empty);
        var before = fs.AllPaths.Order(StringComparer.Ordinal).ToList();

        RunReport(fs);

        Assert.Equal(before, fs.AllPaths.Order(StringComparer.Ordinal));
    }
}
