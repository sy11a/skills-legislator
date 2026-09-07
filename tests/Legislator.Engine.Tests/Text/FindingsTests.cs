using Legislator.Engine.Text;
using Xunit;

namespace Legislator.Engine.Tests.Text;

/// <summary>
/// One place turns a job's findings into what the process says (C-07). The Python engine
/// prints every finding on its own line and exits 1 when there is at least one, 0 otherwise;
/// the order is Python's `sorted()`, which is ordinal, never the current culture's.
/// </summary>
public sealed class FindingsTests
{
    [Fact]
    public void No_findings_is_exit_0_and_silence()
    {
        var result = Findings.AsResult([]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    public void Findings_are_exit_1_one_per_line_each_newline_terminated()
    {
        var result = Findings.AsResult(["second", "first"]);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("first\nsecond\n", result.Stdout);
    }

    /// <summary>
    /// The ruler compares stdout byte for byte against the Python's. A culture-aware sort
    /// puts "a.md" before "Z.md"; Python's does not, and neither may this.
    /// </summary>
    [Fact]
    public void Sorting_is_ordinal_not_cultural()
    {
        var result = Findings.AsResult(["a.md: x", "Z.md: x"]);

        Assert.Equal("Z.md: x\na.md: x\n", result.Stdout);
    }
}
