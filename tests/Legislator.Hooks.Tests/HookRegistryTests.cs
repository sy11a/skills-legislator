using Xunit;

namespace Legislator.Hooks.Tests;

/// <summary>
/// The hooks this edition ships, by name. The names are the Python scripts' own stems because
/// that is what `hooks.json` and the parity ruler address them by (C-11); the registry is data,
/// never a place a caller registers into - the `JobRegistry` precedent.
/// </summary>
public sealed class HookRegistryTests
{
    [Fact]
    public void The_registry_carries_the_four_hooks_under_the_names_hooks_json_uses()
    {
        Assert.Equal(
            ["format_on_edit", "guard_git_conduct", "guard_owned_files", "okf_sync_check"],
            HookRegistry.Names);
    }

    [Fact]
    public void Every_registered_hook_answers_to_the_name_it_is_registered_under()
    {
        foreach (var (name, make) in HookRegistry.Hooks)
        {
            Assert.Equal(name, make().Name);
        }
    }
}
