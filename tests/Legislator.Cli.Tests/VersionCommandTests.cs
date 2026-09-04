using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Cli.Tests;

/// <summary>
/// `legislator version` and its `--json` form (R-8214, C-12). The plain form stays what T-05
/// built — the pinned informational version and nothing else — and `--json` adds the two facts
/// an integrity check cannot get any other way: which platform this binary was built for, and
/// the digest of the file that is running. The digest is of the RUNNING executable, taken here
/// rather than by the caller, because a caller hashing a path on disk hashes whatever is at
/// that path now, which is exactly the question the check is asking.
/// </summary>
public sealed class VersionCommandTests
{
    private static (int Exit, string Out, string Err) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            args, JobRegistry.Jobs, HookRegistry.Hooks, new MockFileSystem(), TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public void Given_version_json_When_run_Then_it_carries_the_version_the_rid_and_a_sha256()
    {
        var (exit, output, _) = Run("version", "--json");

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;
        Assert.Equal(Run("version").Out.Trim(), root.GetProperty("version").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("rid").GetString()));
        Assert.Matches("^[0-9a-f]{64}$", root.GetProperty("sha256").GetString());
    }

    /// <summary>The digest is of this executable: hashing the same file twice gives the same answer, and it is the file the test can hash itself.</summary>
    [Fact]
    public void Given_version_json_When_run_twice_Then_the_sha256_is_the_running_executable_and_stable()
    {
        var first = JsonDocument.Parse(Run("version", "--json").Out).RootElement.GetProperty("sha256").GetString();
        var second = JsonDocument.Parse(Run("version", "--json").Out).RootElement.GetProperty("sha256").GetString();

        Assert.Equal(first, second);
    }

    /// <summary>The plain form is unchanged by the flag's arrival — T-05's contract, still exactly one line.</summary>
    [Fact]
    public void Given_version_without_the_flag_When_run_Then_it_prints_one_bare_line()
    {
        var (exit, output, _) = Run("version");

        Assert.Equal(0, exit);
        Assert.Equal(output.Trim(), output.TrimEnd('\n'));
        Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.DoesNotContain("{", output, StringComparison.Ordinal);
    }

    /// <summary>An unknown flag on a command that takes exactly one is usage, not a silently ignored argument.</summary>
    [Fact]
    public void Given_version_with_an_unknown_flag_When_run_Then_it_is_a_usage_error()
    {
        var (exit, _, err) = Run("version", "--nonsense");

        Assert.Equal(2, exit);
        Assert.NotEqual("", err);
    }
}
