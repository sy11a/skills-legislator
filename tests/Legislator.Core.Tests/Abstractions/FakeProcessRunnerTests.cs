using Legislator.Core.Abstractions;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Abstractions;

public sealed class FakeProcessRunnerTests
{
    [Fact]
    public void Run_records_the_call_and_returns_the_scripted_result()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(3, "out", "err") };

        var result = runner.Run("git", ["status"], "/work", TimeSpan.FromSeconds(1));

        Assert.Equal(new ProcessResult(3, "out", "err"), result);
        var call = Assert.Single(runner.Calls);
        Assert.Equal("git", call.FileName);
        Assert.Equal(["status"], call.Args);
        Assert.Equal("/work", call.WorkingDirectory);
    }
}
