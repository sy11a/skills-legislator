using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Xunit;
using static Legislator.Parity.Tests.Engine.RunJobs;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the verify job answers (R-8206). The
/// ruler's fixture is one tree walked twice: applied and scaffolded, then corrupted, then
/// robbed of a Step-4 artifact - so the twin builds it once and runs verify at each state, the
/// way the ruler does.
/// </summary>
public sealed class VerifyTwins
{
    /// <summary>The ruler's fixture at the moment verify first runs: applied, fully scaffolded, one owned file corrupted on disk.</summary>
    private static MockFileSystem Corrupted()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        Run(fs, "apply", null, null, "--stacks", string.Empty);
        ScaffoldAll(fs);
        fs.File.WriteAllText($"{Root}/docs/ai/rules/core/okf.md", "corrupted\n");
        return fs;
    }

    [Fact]
    [Parity("engine", "verify_recopies_a_diverged_owned_file_once_and_is_clean")]
    public void Verify_recopies_a_diverged_owned_file_once_and_is_clean()
    {
        var fs = Corrupted();

        var (exit, stdout, stderr) = Run(fs, "verify");

        Assert.Equal(0, exit);
        Assert.Equal("", stdout);
        Assert.Equal("", stderr);
        Assert.Equal(
            fs.File.ReadAllBytes($"{SkillPath}/assets/rules/core/okf.md"),
            fs.File.ReadAllBytes($"{Root}/docs/ai/rules/core/okf.md"));
    }

    [Fact]
    [Parity("engine", "verify_names_a_missing_step4_artifact")]
    public void Verify_names_a_missing_step4_artifact()
    {
        var fs = Corrupted();
        Run(fs, "verify");
        fs.File.Delete($"{Root}/docs/cases/README.md");

        var (exit, stdout, _) = Run(fs, "verify");

        Assert.Equal(1, exit);
        Assert.Contains("docs/cases/README.md", stdout, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "verify_appends_post_snapshot_to_record")]
    public void Verify_appends_post_snapshot_to_record()
    {
        var fs = Corrupted();
        Run(fs, "verify");
        fs.File.Delete($"{Root}/docs/cases/README.md");
        Run(fs, "verify");

        var post = Record(fs).GetProperty("post");

        Assert.Equal(JsonValueKind.False, post.GetProperty("step4").GetProperty("docs/cases/README.md").ValueKind);
        Assert.Equal(JsonValueKind.False, post.GetProperty("verify").GetProperty("clean").ValueKind);
    }
}
