using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The verify job's two failure lines and its quiet success. The parity twins drive the re-copy
/// and the missing Step-4 artifact; the branch no ruler builds is a manifest naming an owned
/// file the package no longer delivers - the state a downgrade leaves behind, where a re-copy
/// is not even possible.
/// </summary>
public sealed class VerifyJobTests
{
    [Fact]
    public void Given_an_applied_and_scaffolded_repository_When_verify_runs_Then_it_says_nothing_and_exits_clean()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        RunApply(fs, null, "--stacks", string.Empty);
        fs.AddFile($"{Root}/docs/cases/README.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# x\n"));

        var result = RunVerify(fs);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void Given_ownedFiles_names_a_path_the_package_no_longer_delivers_When_verify_runs_Then_it_says_so_and_asks_for_apply()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# A\n",
            ["docs/ai/rules/core/retired.md"] = "old\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], "
                + "\"ownedFiles\": [\"docs/ai/rules/core/retired.md\"]}",
        });

        var result = RunVerify(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("docs/ai/rules/core/retired.md", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("apply", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_no_run_record_exists_When_verify_runs_Then_it_still_verifies_and_writes_no_record()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# A\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
        });

        var result = RunVerify(fs, "--record", "/tmp/absent-record.json");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("docs/cases/README.md", result.Stdout, StringComparison.Ordinal);
        Assert.False(fs.File.Exists("/tmp/absent-record.json"));
    }

    private const string JournalTarget = "docs/journal/<today>.md";

    /// <summary>Step 4 table carrying the journal row (R-012) alongside the ordinary `cases.md` row, overriding the fixture's own one-row table.</summary>
    private const string Step4WithJournalRow =
        "## Step 4\n\n| Target | Template | Notes |\n|---|---|---|\n"
        + "| `docs/cases/README.md` | cases.md | |\n"
        + "| `" + JournalTarget + "` | journal.md | Fresh-scaffold mode only |\n"
        + "\n## Step 5\n";

    /// <summary>R-012, R-013: an upgrade-mode run (manifest present, so `Detection.Of` reads `upgrade` before apply writes its record) with no journal entry on disk verifies clean - the row is excluded from the missing list because the record's mode differs from fresh.</summary>
    [Fact]
    public void Given_an_upgrade_mode_run_with_no_journal_entry_on_disk_When_verify_runs_Then_it_verifies_clean()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# A\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 24, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
        });
        fs.File.WriteAllText($"{SkillPath}/SKILL.md", Step4WithJournalRow);
        RunApply(fs, null, "--stacks", string.Empty);
        fs.AddFile($"{Root}/docs/cases/README.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# x\n"));

        var result = RunVerify(fs);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>R-012 regression guard: a fresh-mode repo (no entry document, no manifest) missing the journal row still reports it missing - the row's actual duty is unchanged by this case.</summary>
    [Fact]
    public void Given_a_fresh_mode_run_with_no_journal_entry_on_disk_When_verify_runs_Then_it_still_reports_it_missing()
    {
        var fs = Repo();
        fs.File.WriteAllText($"{SkillPath}/SKILL.md", Step4WithJournalRow);
        RunApply(fs, null, "--stacks", string.Empty);
        fs.AddFile($"{Root}/docs/cases/README.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# x\n"));

        var result = RunVerify(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(JournalTarget, result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>R-013: with no `--record` file to read, the journal row stays in the failure list - no `Detection.Of` fallback (code review round 1's ruling: after apply has written the manifest, a repo can never read back as fresh).</summary>
    [Fact]
    public void Given_no_record_file_When_verify_runs_Then_the_journal_row_stays_in_the_failure_list()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# A\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
            ["docs/cases/README.md"] = "# x\n",
        });
        fs.File.WriteAllText($"{SkillPath}/SKILL.md", Step4WithJournalRow);

        var result = RunVerify(fs, "--record", "/tmp/absent-record-journal.json");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(JournalTarget, result.Stdout, StringComparison.Ordinal);
    }
}
