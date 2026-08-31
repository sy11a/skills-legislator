using Legislator.Core.Options;
using Legislator.Core.Tests.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class EnvLayerReaderTests
{
    [Fact]
    public void Maps_upper_snake_to_key_and_ignores_unknown()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_DOCS_DIR"] = "d";
        env.Vars["LEGISLATOR_OTHER"] = "x";

        var raw = EnvLayerReader.Read(env, ["docs_dir"]);

        Assert.Equal("d", raw["docs_dir"]);
        Assert.Single(raw);
    }
}
