namespace Legislator.Hooks;

/// <summary>One Claude Code hook - the unit `legislator hook &lt;name&gt;` dispatches to (C-11). The hook owns its judgement and nothing about how it was invoked: it neither reads stdin nor writes a stream, so the same class answers a payload from a test and from the editor.</summary>
public interface IHook
{
    /// <summary>The name `hooks.json` and the parity ruler address this hook by - the Python script's own stem.</summary>
    string Name { get; }

    HookResult Run(HookContext ctx);
}
