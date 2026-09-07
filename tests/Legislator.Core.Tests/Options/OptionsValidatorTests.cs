using Legislator.Core.Options;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class OptionsValidatorTests
{
    [Fact]
    public void Valid_layer_yields_no_errors()
    {
        var raw = new Dictionary<string, string> { ["docs_dir"] = "d", ["okf_debt_days"] = "1", ["build_dirs"] = "a,b" };

        Assert.Empty(OptionsValidator.Validate(OptionsLayer.Instance, raw));
    }

    [Fact]
    public void Unknown_key_is_reported_with_the_layer()
    {
        var raw = new Dictionary<string, string> { ["case_dir"] = "x" };

        var error = Assert.Single(OptionsValidator.Validate(OptionsLayer.Environment, raw));

        Assert.Equal(OptionsLayer.Environment, error.Layer);
        Assert.Equal("case_dir", error.Key);
        Assert.Contains("unknown", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Int_below_one_is_rejected()
    {
        var raw = new Dictionary<string, string> { ["okf_debt_days"] = "0" };

        Assert.Equal("okf_debt_days", Assert.Single(OptionsValidator.Validate(OptionsLayer.Machine, raw)).Key);
    }

    [Fact]
    public void Empty_string_is_rejected()
    {
        var raw = new Dictionary<string, string> { ["docs_dir"] = "" };

        Assert.Equal("docs_dir", Assert.Single(OptionsValidator.Validate(OptionsLayer.Machine, raw)).Key);
    }

    [Theory]
    [InlineData("../elsewhere")]
    [InlineData("a/../b")]
    [InlineData(@"a\..\b")]
    public void Parent_segment_is_rejected(string value)
    {
        var raw = new Dictionary<string, string> { ["cases_dir"] = value };

        Assert.Equal("cases_dir", Assert.Single(OptionsValidator.Validate(OptionsLayer.Instance, raw)).Key);
    }

    [Fact]
    public void Dots_inside_a_name_are_allowed()
    {
        var raw = new Dictionary<string, string> { ["cases_dir"] = "a..b" };

        Assert.Empty(OptionsValidator.Validate(OptionsLayer.Instance, raw));
    }
}
