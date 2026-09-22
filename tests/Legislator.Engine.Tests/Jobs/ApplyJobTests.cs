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

    /// <summary>
    /// The upgrade that retires a delivered file and the declaration that names it move in one
    /// act, or neither moves (BL-397). The fixture is an edition-25 repository whose manifest
    /// owns `docs/ai/engine.py`, upgraded by a package that no longer ships it.
    /// </summary>
    private static System.IO.Abstractions.TestingHelpers.MockFileSystem AtEdition25(string entry)
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = entry,
            ["docs/ai/engine.py"] = "# the delivered engine\n",
            ["docs/ai/rules/core/okf.md"] = "# okf law\n",
            ["docs/ai/manifest.json"] =
                "{\n  \"legislatorVersion\": 24,\n  \"stacks\": [],\n  \"keep\": [],\n  \"ownedFiles\": [\n"
                + "    \"docs/ai/engine.py\",\n    \"docs/ai/rules/core/okf.md\"\n  ]\n}\n",
        });
        return fs;
    }

    [Fact]
    public void Given_an_upgrade_retiring_the_engine_When_the_entry_document_declares_it_Then_the_declaration_moves_with_it()
    {
        var fs = AtEdition25("## Build & Test\n\n- `python3 docs/ai/engine.py anchors`\n- `python3 docs/ai/engine.py sdd-lint`\n");

        var result = RunApply(fs, null, "--stacks", "");

        Assert.Equal(0, result.ExitCode);
        Assert.False(fs.File.Exists($"{Root}/docs/ai/engine.py"));
        Assert.Equal(
            "## Build & Test\n\n- `legislator anchors`\n- `legislator sdd-lint`\n",
            fs.File.ReadAllText($"{Root}/AGENTS.md"));
        Assert.Contains("retired commands: 2 declaration(s) rewritten", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("rewritten: AGENTS.md:3", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_an_upgrade_retiring_the_engine_When_a_gate_script_runs_it_Then_the_script_moves_too()
    {
        var fs = AtEdition25("# Entry\n");
        fs.AddFile($"{Root}/tools/gate.sh", new System.IO.Abstractions.TestingHelpers.MockFileData(
            "run anchors python3 docs/ai/engine.py anchors\nrun sdd-lint python3 docs/ai/engine.py sdd-lint\n"));

        var result = RunApply(fs, null, "--stacks", "");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "run anchors legislator anchors\nrun sdd-lint legislator sdd-lint\n",
            fs.File.ReadAllText($"{Root}/tools/gate.sh"));
    }

    [Fact]
    public void Given_a_declaration_in_a_shape_the_migration_cannot_rewrite_When_apply_runs_Then_it_stops_before_writing()
    {
        var fs = AtEdition25("Verification: read docs/ai/engine.py and run what it says.\n");

        var result = RunApply(fs, null, "--stacks", "");

        Assert.Equal(4, result.ExitCode);
        Assert.Contains("AGENTS.md:1", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("have to move together", result.Stderr, StringComparison.Ordinal);
        // The promise the stop makes: the retirement did not happen either.
        Assert.True(fs.File.Exists($"{Root}/docs/ai/engine.py"));
        Assert.Equal("{\n  \"legislatorVersion\": 24,\n  \"stacks\": [],\n  \"keep\": [],\n  \"ownedFiles\": [\n"
            + "    \"docs/ai/engine.py\",\n    \"docs/ai/rules/core/okf.md\"\n  ]\n}\n",
            fs.File.ReadAllText($"{Root}/docs/ai/manifest.json"));
    }

    [Fact]
    public void Given_a_case_summary_naming_the_retired_command_When_apply_runs_Then_it_is_reported_and_left()
    {
        var fs = AtEdition25("# Entry\n");
        fs.AddFile($"{Root}/docs/cases/BL-001-a/summary.md", new System.IO.Abstractions.TestingHelpers.MockFileData(
            "The gate was `python3 docs/ai/engine.py anchors`.\n"));

        var result = RunApply(fs, null, "--stacks", "");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("left as history: docs/cases/BL-001-a/summary.md:1", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("`python3 docs/ai/engine.py anchors`",
            fs.File.ReadAllText($"{Root}/docs/cases/BL-001-a/summary.md"), StringComparison.Ordinal);
    }
}
