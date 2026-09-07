using System.IO.Abstractions;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>
/// The ruler's own environment guard, twinned (R-8206). `check_hooks.py` asserts git is on PATH
/// before its two git-dependent regions run, and asserts it as a FAILURE when it is not, so a
/// machine without git cannot report a green ruler over checks that never executed. The same
/// claim holds this side: the .NET twins answer git at its interface, but the parity run in
/// `check_hooks.py` still drives real repositories, and a git-less machine would make that run
/// green by emptiness. One test carries both labels because it is one condition, asserted twice.
/// </summary>
public sealed class HooksHarnessTwins
{
    /// <summary>The one place these twins step outside the fakes, because the claim IS about this machine.</summary>
    [Fact]
    [Parity("hooks", "git available for guard_git_conduct.py tests")]
    [Parity("hooks", "git available for okf_sync_check.py tests")]
    public void Git_is_available_to_the_machine_running_the_parity_suite()
    {
        var options = new LegislatorOptions();

        var found = ExecutableLookup.Which(
            new FileSystem(), new SystemEnvironment(), options, options.GitExecutable.Value);

        Assert.NotNull(found);
    }
}
