using System.Text.RegularExpressions;
using Legislator.Core.Abstractions;
using Legislator.Core.Repo;

namespace Legislator.Hooks.Hooks;

/// <summary>
/// PreToolUse guard on git conduct (BL-064, C-11): it enforces the pair-development law where
/// the decision is taken - on the agent's command line - and only inside legislated
/// repositories. Every undecidable case allows: no git, a detached HEAD, an ambiguous default
/// branch, a line no shell splitter can read. The human path stays open by construction,
/// because hooks fire on the agent's tool calls and never on the user's own terminal.
/// </summary>
public sealed partial class GuardGitConductHook : IHook
{
    private const string MergeMessage =
        "merging into the default branch is the user's act — push the task branch and leave "
        + "merging to them (core/pair-development.md).";

    private const string PushMessage =
        "pushing the default branch is the user's act — push the task branch and leave "
        + "integration to them (core/pair-development.md).";

    private const string PrMergeMessage =
        "merging the PR is the user's act, whatever the channel — leave it to them "
        + "(core/pair-development.md).";

    private const string AttributionMessage =
        "AI attribution in the VCS record is forbidden — drop the Co-Authored-By trailer / "
        + "Generated-with footer (core/pair-development.md).";

    /// <summary>The tool whose calls this guard judges; a payload from any other is not its business.</summary>
    private const string BashTool = "Bash";

    private const string GitHubCli = "gh";
    private const string PrNoun = "pr";
    private const string MergeVerb = "merge";
    private const string PushVerb = "push";
    private const string CommitVerb = "commit";

    /// <summary>git's own global options that consume the token behind them - a `--git-dir` value read as a subcommand is a guard that judges the wrong thing.</summary>
    private static readonly HashSet<string> GitOptionsWithArgument =
        new(StringComparer.Ordinal) { "-C", "-c", "--exec-path", "--git-dir", "--work-tree", "--namespace" };

    private static readonly HashSet<string> PushOptionsWithArgument =
        new(StringComparer.Ordinal) { "-o", "--push-option", "--receive-pack", "--exec", "--repo" };

    /// <summary>The options that push every branch, whichever one the command was run from.</summary>
    private static readonly HashSet<string> PushEverything =
        new(StringComparer.Ordinal) { "--all", "--branches", "--mirror" };

    /// <summary>Cleaning up a merge is not merging.</summary>
    private static readonly HashSet<string> MergeCleanup =
        new(StringComparer.Ordinal) { "--abort", "--quit" };

    public string Name => "guard_git_conduct";

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

        if (!string.Equals(ctx.Payload.ToolName, BashTool, StringComparison.Ordinal))
        {
            return HookResult.Allow;
        }

        var command = ctx.Payload.Command;
        if (string.IsNullOrEmpty(command))
        {
            return HookResult.Allow;
        }

        // The fast path the Python takes: a line naming neither tool cannot be judged by either
        // rule, and asking git about it would cost a process per keystroke.
        if (!command.Contains(ctx.Options.GitExecutable.Value, StringComparison.OrdinalIgnoreCase)
            && !command.Contains(GitHubCli, StringComparison.OrdinalIgnoreCase))
        {
            return HookResult.Allow;
        }

        var cwd = ctx.Payload.Cwd ?? ctx.Env.CurrentDirectory;
        if (LegislatedRepo.Find(ctx.Fs, ctx.Options, cwd) is null)
        {
            return HookResult.Allow;
        }

        var message = Judge(ctx, command, cwd);
        return message is null ? HookResult.Allow : HookResult.Block(message);
    }

    private static string? Judge(HookContext ctx, string command, string cwd)
    {
        foreach (var raw in CommandLine.Segments(command))
        {
            var segment = StripEnvironmentPrefix(raw);
            if (segment.Count == 0)
            {
                continue;
            }

            var message = JudgeSegment(ctx, segment, cwd);
            if (message is not null)
            {
                return message;
            }
        }

        return null;
    }

    private static string? JudgeSegment(HookContext ctx, List<string> segment, string cwd)
    {
        var head = Head(segment[0]);

        if (string.Equals(head, ctx.Options.GitExecutable.Value, StringComparison.Ordinal))
        {
            var (verb, arguments, redirected) = Subcommand(segment);
            var repository = redirected ?? cwd;

            return verb switch
            {
                MergeVerb => JudgeMerge(ctx, arguments, repository),
                PushVerb => JudgePush(ctx, arguments, repository),
                CommitVerb when HasAttribution(string.Join(' ', segment)) => AttributionMessage,
                _ => null,
            };
        }

        if (string.Equals(head, GitHubCli, StringComparison.Ordinal)
            && segment.Count >= 3
            && string.Equals(segment[1], PrNoun, StringComparison.Ordinal))
        {
            if (string.Equals(segment[2], MergeVerb, StringComparison.Ordinal))
            {
                return PrMergeMessage;
            }

            if (segment[2] is "create" or "edit" && HasAttribution(string.Join(' ', segment)))
            {
                return AttributionMessage;
            }
        }

        return null;
    }

    private static string? JudgeMerge(HookContext ctx, List<string> arguments, string repository)
    {
        if (arguments.Any(MergeCleanup.Contains))
        {
            return null;
        }

        var current = CurrentBranch(ctx, repository);
        var @default = DefaultBranch(ctx, repository);

        return current is not null && @default is not null && string.Equals(current, @default, StringComparison.Ordinal)
            ? MergeMessage
            : null;
    }

    private static string? JudgePush(HookContext ctx, List<string> arguments, string repository)
    {
        var @default = DefaultBranch(ctx, repository);

        return @default is not null && TargetsDefault(arguments, @default, CurrentBranch(ctx, repository))
            ? PushMessage
            : null;
    }

    /// <summary>Whether this push writes the default branch: explicitly by refspec, wholesale by option, or implicitly because a bare push writes the branch you are standing on.</summary>
    private static bool TargetsDefault(List<string> arguments, string @default, string? current)
    {
        var positional = new List<string>();
        for (var i = 0; i < arguments.Count; i++)
        {
            var token = arguments[i];
            if (PushOptionsWithArgument.Contains(token))
            {
                i++;
            }
            else if (PushEverything.Contains(token))
            {
                return true;
            }
            else if (!token.StartsWith('-'))
            {
                positional.Add(token);
            }
        }

        // The first positional is the remote; what follows it are the refspecs.
        var refspecs = positional.Skip(1).ToList();
        if (refspecs.Count == 0)
        {
            return string.Equals(current, @default, StringComparison.Ordinal);
        }

        return refspecs.Any(spec =>
        {
            var trimmed = spec.TrimStart('+');
            var destination = trimmed.Contains(':', StringComparison.Ordinal)
                ? trimmed.Split(':', 2)[1]
                : trimmed;

            return string.Equals(destination.Replace("refs/heads/", "", StringComparison.Ordinal), @default, StringComparison.Ordinal);
        });
    }

    private static string? CurrentBranch(HookContext ctx, string repository) =>
        GitLog.Ask(ctx.Proc, ctx.Options, repository, "symbolic-ref", "--quiet", "--short", "HEAD").Output;

    /// <summary>The default branch: what origin points at, else the single conventional name among the local branches. Two candidates are an ambiguity, and an ambiguity allows.</summary>
    private static string? DefaultBranch(HookContext ctx, string repository)
    {
        var head = GitLog.Ask(
            ctx.Proc, ctx.Options, repository, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD").Output;

        if (head is not null && head.Contains('/', StringComparison.Ordinal))
        {
            return head.Split('/', 2)[1];
        }

        var listed = GitLog.Ask(
            ctx.Proc, ctx.Options, repository, "for-each-ref", "--format=%(refname:short)", "refs/heads").Output;

        if (listed is null)
        {
            return null;
        }

        var names = listed.Split('\n').Select(n => n.Trim()).ToHashSet(StringComparer.Ordinal);
        var candidates = ctx.Options.ConventionalDefaultBranches.Value.Where(names.Contains).ToList();
        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// The command's head as a bare program name: a directory prefix in either separator is
    /// dropped, a Windows executable suffix with it, and the result lower-cased. `git.exe` and
    /// `C:\...\git.exe` are git; `github` is not (R-701).
    /// </summary>
    private static string Head(string token)
    {
        var name = token.Replace('\\', '/').Split('/')[^1].ToLowerInvariant();
        return name.EndsWith(".exe", StringComparison.Ordinal) ? name[..^4] : name;
    }

    /// <summary>`VAR=value git merge` is a git command; the assignments in front of it are the shell's, not the program's.</summary>
    private static List<string> StripEnvironmentPrefix(List<string> segment)
    {
        var at = 0;
        while (at < segment.Count && EnvironmentAssignment().IsMatch(segment[at]))
        {
            at++;
        }

        return segment.Skip(at).ToList();
    }

    /// <summary>The subcommand, what follows it, and the repository `-C` redirected the command to.</summary>
    private static (string? Verb, List<string> Arguments, string? Redirected) Subcommand(List<string> segment)
    {
        string? redirected = null;
        for (var i = 1; i < segment.Count; i++)
        {
            var token = segment[i];
            if (GitOptionsWithArgument.Contains(token))
            {
                if (string.Equals(token, "-C", StringComparison.Ordinal) && i + 1 < segment.Count)
                {
                    redirected = segment[i + 1];
                }

                i++;
                continue;
            }

            if (!token.StartsWith('-'))
            {
                return (token, segment.Skip(i + 1).ToList(), redirected);
            }
        }

        return (null, [], redirected);
    }

    private static bool HasAttribution(string text) =>
        CoAuthoredBy().IsMatch(text) || GeneratedWith().IsMatch(text);

    [GeneratedRegex(@"co-authored-by\s*:[^\n]{0,120}\b(claude|anthropic)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CoAuthoredBy();

    [GeneratedRegex(@"generated\s+with\b.{0,80}\b(claude|anthropic)\b", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GeneratedWith();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*=")]
    private static partial Regex EnvironmentAssignment();
}
