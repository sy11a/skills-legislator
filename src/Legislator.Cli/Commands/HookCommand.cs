using System.IO.Abstractions;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Hooks;

namespace Legislator.Cli.Commands;

/// <summary>
/// The host's side of the hook contract (C-11): `legislator hook &lt;name&gt;` reads one JSON
/// object on stdin, hands it to the named hook and renders its answer - exit 0 to allow, exit 2
/// with the message on stderr to block, and never anything else. The catch-all lives HERE
/// rather than in each hook because it is the contract that must hold, including for a bug no
/// hook's own code foresaw: a crashing guard must not stop the user's work.
/// </summary>
internal static class HookCommand
{
    /// <summary>The hook's exit code, or null when no hook was named or the name is not one this edition ships - a usage error the host renders, not a decision this command may make.</summary>
    public static int? Run(
        List<string> args,
        IReadOnlyDictionary<string, Func<IHook>> hooks,
        TextReader stdin,
        IFileSystem fs,
        IEnvironment env,
        IProcessRunner proc,
        Func<LegislatorOptions> compose,
        TextWriter stderr)
    {
        if (args.Count == 0 || !hooks.TryGetValue(args[0], out var make))
        {
            return null;
        }

        try
        {
            // Composition is inside the guard on purpose: a malformed configuration file is a
            // loud exit 2 for a job, and exit 2 from a hook BLOCKS the edit that triggered it.
            var result = make().Run(new HookContext(HookPayload.Parse(stdin.ReadToEnd()), fs, env, proc, compose()));
            stderr.Write(result.Message);
            return result.ExitCode;
        }
#pragma warning disable CA1031 // The contract's hard edge (C-11): every failure below this line, including a bug in a hook, becomes an allow rather than a stopped session.
        catch (Exception)
        {
            return 0;
        }
#pragma warning restore CA1031
    }
}
