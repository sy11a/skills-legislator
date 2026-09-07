using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The apply job's command line and the lines it prints. What apply DOES to a tree is the
/// parity twins' subject; what it refuses to be asked, and the five stdout lines C-10 pins,
/// have no ruler label at all - the ruler reads the manifest and the record, never the report
/// apply prints to the caller who is watching it run.
/// </summary>
public sealed class ApplyJobTests
{
    [Fact]
    public void Given_no_stacks_flag_When_apply_runs_Then_it_exits_usage_without_writing()
    {
        var fs = Repo(new() { ["README.md"] = "r\n" });

        var result = RunApply(fs);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("--stacks", result.Stderr, StringComparison.Ordinal);
        Assert.False(fs.File.Exists($"{Root}/docs/ai/manifest.json"));
    }

    [Fact]
    public void Given_a_keep_add_without_the_separator_When_apply_runs_Then_it_exits_usage_without_writing()
    {
        var fs = Repo(new() { ["README.md"] = "r\n" });

        var result = RunApply(fs, null, "--stacks", string.Empty, "--keep-add", "docs/notes/a.md");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("--keep-add", result.Stderr, StringComparison.Ordinal);
        Assert.False(fs.File.Exists($"{Root}/docs/ai/manifest.json"));
    }

    [Fact]
    public void Given_a_fresh_repository_When_apply_runs_Then_stdout_carries_the_mode_the_counts_and_the_record()
    {
        var fs = Repo(new() { ["README.md"] = "r\n" });

        var result = RunApply(fs, null, "--stacks", "dotnet");
        var lines = result.Stdout.TrimEnd('\n').Split('\n');

        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"apply: fresh mode, constitution v{SkillVersion}, stacks [\"dotnet\"]", lines[0]);
        Assert.Equal("  owned: 4 created, 0 overwritten, 0 unchanged, 0 deleted", lines[1]);
        Assert.Equal("  keep: 0 added, 0 removed, 0 refused", lines[2]);
        Assert.StartsWith("run record: ", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_file_model_event_When_apply_runs_Then_stdout_carries_it_between_the_keep_line_and_the_record()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });

        var result = RunApply(fs, null, "--stacks", string.Empty);
        var lines = result.Stdout.TrimEnd('\n').Split('\n');

        Assert.Equal("  file model: linked CLAUDE.md -> AGENTS.md", lines[3]);
        Assert.StartsWith("run record: ", lines[4], StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_second_run_over_an_unchanged_tree_When_apply_runs_Then_every_owned_file_counts_as_unchanged()
    {
        var fs = Repo(new() { ["README.md"] = "r\n" });
        RunApply(fs, null, "--stacks", string.Empty);

        var result = RunApply(fs, null, "--stacks", string.Empty);

        Assert.Contains("  owned: 0 created, 0 overwritten, 3 unchanged, 0 deleted", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_both_entry_documents_are_real_When_apply_runs_Then_it_stops_at_exit_4_with_the_reason_on_stderr()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n", ["CLAUDE.md"] = "# C\n" });

        var result = RunApply(fs, null, "--stacks", string.Empty);

        Assert.Equal(4, result.ExitCode);
        Assert.StartsWith("apply stopped: ", result.Stderr, StringComparison.Ordinal);
        Assert.Equal("", result.Stdout);
    }
}
