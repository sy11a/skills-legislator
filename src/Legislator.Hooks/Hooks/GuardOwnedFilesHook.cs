using Legislator.Core.Repo;

namespace Legislator.Hooks.Hooks;

/// <summary>
/// PreToolUse guard on machine-managed law (C-11): an edit landing on a legislated
/// repository's rule tree, its delivered engine or its root wiring file is blocked. The
/// manifest is deliberately NOT guarded - the apply job regenerates it on every run, and that
/// rewrite already heals a hand-edit, so guarding it would add nothing but a refusal.
/// </summary>
public sealed class GuardOwnedFilesHook : IHook
{
    public string Name => "guard_owned_files";

    public HookResult Run(HookContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // Stdin that is not a JSON object is a payload this hook cannot read, and a hook that
        // cannot read its input cannot judge: the Python parses before it decides, so a parse
        // failure never reaches the decision at all (C-11).
        if (!ctx.Payload.Usable)
        {
            return HookResult.Allow;
        }

        var edited = Resolve(ctx);
        if (edited is null)
        {
            return HookResult.Allow;
        }

        var root = LegislatedRepo.Find(ctx.Fs, ctx.Options, ctx.Fs.Path.GetDirectoryName(edited) ?? edited);
        if (root is null)
        {
            return HookResult.Allow;
        }

        var layout = new RepoLayout(ctx.Options, root);
        var owned =
            edited.StartsWith(layout.Rules + "/", StringComparison.Ordinal)
            || string.Equals(edited, layout.Opencode, StringComparison.Ordinal);

        return owned ? HookResult.Block(BlockMessage(layout)) : HookResult.Allow;
    }

    /// <summary>
    /// The refusal, assembled from the layout rather than quoted: it names the very paths the
    /// judgement above tested, so the two can never drift apart, and the R-8209 tripwire is not
    /// asked to tell a sentence of law from a path default (the T-10 precedent).
    /// </summary>
    private static string BlockMessage(RepoLayout layout) =>
        $"{layout.Relative(layout.Rules)}/** and {layout.Relative(layout.Opencode)} "
        + "are machine-managed law — edit the legislator "
        + "skill source and re-run /legislator instead.";

    /// <summary>The edited path, made absolute against the payload's own working directory - the hook runs wherever the editor started it, not where the session stands.</summary>
    private static string? Resolve(HookContext ctx)
    {
        var named = ctx.Payload.FilePath;
        if (named is null)
        {
            return null;
        }

        var full = ctx.Fs.Path.IsPathRooted(named)
            ? named
            : ctx.Fs.Path.Combine(ctx.Payload.Cwd ?? ctx.Env.CurrentDirectory, named);

        return ctx.Fs.Path.GetFullPath(full);
    }
}
