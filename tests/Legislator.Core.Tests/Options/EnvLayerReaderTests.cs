using Legislator.Core.Options;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class EnvLayerReaderTests
{
    [Fact]
    public void Maps_upper_snake_to_key_and_reports_stray_prefixed_names()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_DOCS_DIR"] = "d";
        env.Vars["LEGISLATOR_OTHER"] = "x";

        var raw = EnvLayerReader.Read(env);

        Assert.Equal("d", raw["docs_dir"]);
        Assert.Equal("x", raw["other"]);
        Assert.Equal(2, raw.Count);
    }
}
