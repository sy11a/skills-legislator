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
}
