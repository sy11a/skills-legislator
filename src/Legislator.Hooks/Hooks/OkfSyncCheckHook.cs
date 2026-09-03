using Legislator.Core.Repo;

namespace Legislator.Hooks.Hooks;

/// <summary>
/// The Stop hook that reminds a session to sync the OKF when the sources moved without it
/// (C-11) - the enforcement arm of okf.md's sync law. Its scope is deliberately narrow:
/// uncommitted working-tree state only, in a legislated git repository, at most once per stop.
/// Everything else is silence, including a tree it could not read.
/// </summary>
public sealed class OkfSyncCheckHook : IHook
{
    public string Name => "okf_sync_check";

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

        // Claude Code sets this when a Stop hook already fired for this stop; without the guard
        // the reminder would fire on the stop its own reminder caused.
        if (ctx.Payload.StopHookActive)
        {
            return HookResult.Allow;
        }

        var cwd = ctx.Payload.Cwd ?? ctx.Env.CurrentDirectory;
        var toplevel = GitLog.Ask(ctx.Proc, ctx.Options, cwd, "rev-parse", "--show-toplevel").Output;
        if (toplevel is null)
        {
            return HookResult.Allow;
        }

        // The manifest is looked for AT the toplevel, not up from the payload's directory: this
        // hook's subject is the repository as git sees it, not the tree the session stands in.
        if (!ctx.Fs.File.Exists(new RepoLayout(ctx.Options, toplevel).Manifest))
        {
            return HookResult.Allow;
        }

        var changed = ChangedPaths(ctx, toplevel);
        var sources = ctx.Options.SrcDir.Value;
        var okf = $"{ctx.Options.DocsDir.Value}/{ctx.Options.OkfDir.Value}";

        return Touched(changed, sources) && !Touched(changed, okf)
            ? HookResult.Block(
                $"{sources}/ changed but {okf}/ didn't — update the OKF (map/log/glossary) "
                + "or state why no update is needed.")
            : HookResult.Allow;
    }

    /// <summary>Whether any changed path is the named directory or lies inside it - a sibling whose name merely starts the same way is a different directory.</summary>
    private static bool Touched(IEnumerable<string> changed, string directory) =>
        changed.Any(p => string.Equals(p, directory, StringComparison.Ordinal)
                         || p.StartsWith(directory + "/", StringComparison.Ordinal));

    /// <summary>Every path git's porcelain reports as changed. A rename names two, and the one that changed is where it landed.</summary>
    private static List<string> ChangedPaths(HookContext ctx, string root)
    {
        // Read, not Ask: porcelain's first two columns are the status, and a trim would eat one
        // of them - the paths would come back shifted and no directory would ever match.
        var output = GitLog.Read(ctx.Proc, ctx.Options, root, "status", "--porcelain").Output;
        if (output is null)
        {
            return [];
        }

        var paths = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            if (line.Length < 4)
            {
                continue;
            }

            var rest = line[3..];
            var arrow = rest.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 0)
            {
                rest = rest[(arrow + 4)..];
            }

            rest = rest.Trim().Trim('"');
            if (rest.Length > 0)
            {
                paths.Add(rest);
            }
        }

        return paths;
    }
}
