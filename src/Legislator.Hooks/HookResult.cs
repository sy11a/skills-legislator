namespace Legislator.Hooks;

/// <summary>
/// What a hook decided (C-11): exit 0 to allow, exit 2 to block with a message Claude Code
/// feeds back to the model. There is no third answer - a hook that cannot tell allows, because
/// a hook that stops the user's work over its own uncertainty is worse than the edit it feared.
/// </summary>
public sealed record HookResult(int ExitCode, string Message)
{
    /// <summary>The answer every undecidable case gives: no exit code of its own, nothing on stderr.</summary>
    public static HookResult Allow { get; } = new(0, "");

    public static HookResult Block(string message) => new(2, message);
}
