using Legislator.Hooks.Hooks;

namespace Legislator.Hooks;

/// <summary>The hooks this edition ships, by name. The names are the Python scripts' own stems, because that is what `hooks.json` and the parity ruler address them by (C-11); the registry is data, never a place a caller registers into - the `JobRegistry` precedent.</summary>
public static class HookRegistry
{
    public static IReadOnlyDictionary<string, Func<IHook>> Hooks { get; } = new Dictionary<string, Func<IHook>>(StringComparer.Ordinal)
    {
        ["format_on_edit"] = () => new FormatOnEditHook(),
        ["guard_git_conduct"] = () => new GuardGitConductHook(),
        ["guard_owned_files"] = () => new GuardOwnedFilesHook(),
        ["okf_sync_check"] = () => new OkfSyncCheckHook(),
    };

    public static IReadOnlyList<string> Names => [.. Hooks.Keys.Order(StringComparer.Ordinal)];
}
