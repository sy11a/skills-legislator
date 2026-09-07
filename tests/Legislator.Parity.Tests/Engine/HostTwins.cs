using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The three `check_engine.py` labels the HOST owns rather than any job (R-8206): what the
/// process does when it is given a job it does not have, or no job at all. T-05 built the
/// contract and T-06 named these three the only genuine passes in the engine ruler's usage
/// region — everything around them asserts an absence a no-op imitates — but their twins were
/// deferred to the CLI task, which is this one.
///
/// Two of the three ruler sites reached `sys.executable` directly until this task, so on the
/// .NET arm they measured the Python engine and reported it as the binary's answer (the T-09
/// decision gate, which fixed four such sites and left these two here). They go through
/// `_engine_argv` now, which is what makes these twins twins of something.
/// </summary>
public sealed class HostTwins
{
    /// <summary>The ruler's argv, in process: `legislator <args>` over an empty repository.</summary>
    private static int Exit(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        return Program.Run(
            args, JobRegistry.Jobs, HookRegistry.Hooks, new MockFileSystem(), TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);
    }

    [Fact]
    [Parity("engine", "an unknown job exits 2")]
    public void An_unknown_job_exits_2() => Assert.Equal(2, Exit("nonsense"));

    [Fact]
    [Parity("engine", "no job exits 2")]
    public void No_job_exits_2() => Assert.Equal(2, Exit());

    [Fact]
    [Parity("engine", "exit_codes_unchanged: usage error is 2")]
    public void Usage_error_is_2() => Assert.Equal(2, Exit("no-such-job", "--root", "/r"));

    /// <summary>
    /// Not a ruler label — the reason the three above are worth having. Exit 2 is the usage
    /// code, so a test asserting only "2" cannot tell a refusal from a crash that happens to
    /// exit 2; the host says which job it did not recognise, on stderr, and prints nothing to
    /// stdout, because a caller parsing stdout must never receive a usage message as data.
    /// </summary>
    [Fact]
    public void Given_an_unknown_job_When_run_Then_the_name_is_on_stderr_and_stdout_is_empty()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["nonsense"], JobRegistry.Jobs, HookRegistry.Hooks, new MockFileSystem(),
            TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal("", stdout.ToString());
        Assert.Contains("nonsense", stderr.ToString(), StringComparison.Ordinal);
    }
}
