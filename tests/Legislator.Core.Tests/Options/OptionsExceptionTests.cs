using Legislator.Core.Options;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class OptionsExceptionTests
{
    [Fact]
    public void Message_is_one_line_per_error_layer_key_reason()
    {
        var ex = new OptionsException([new(OptionsLayer.Machine, "nope", "unknown key"), new(OptionsLayer.Environment, "okf_debt_days", "must be an integer of 1 or more")]);

        Assert.Equal("machine: nope: unknown key\nenvironment: okf_debt_days: must be an integer of 1 or more", ex.Message);
    }

    [Fact]
    public void Document_fault_line_omits_the_empty_key()
    {
        var ex = new OptionsException([new(OptionsLayer.Instance, "", "document is not a mapping")]);

        Assert.Equal("instance: document is not a mapping", ex.Message);
    }
}
