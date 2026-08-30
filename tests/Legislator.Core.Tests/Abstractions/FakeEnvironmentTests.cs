using Legislator.Core.Tests.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Abstractions;

public sealed class FakeEnvironmentTests
{
    [Fact]
    public void GetVariable_returns_null_when_unset()
    {
        var env = new FakeEnvironment();

        Assert.Null(env.GetVariable("LEGISLATOR_NOPE"));
    }

    [Fact]
    public void GetVariable_returns_value_when_set()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_X"] = "1";

        Assert.Equal("1", env.GetVariable("LEGISLATOR_X"));
    }
}
