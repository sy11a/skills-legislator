using System.Buffers;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Cli.Commands;
using Legislator.Engine;
using Legislator.Hooks;

namespace Legislator.Cli;

/// <summary>
/// The CLI host: it parses the command line, composes the options, dispatches to a job and renders
/// what comes back. It carries no engine logic of its own (ADR-0008) - everything below the parse
/// is a job. Exit codes are the v24 engine's: 0 clean, 1 findings, 2 usage or configuration,
/// 3 engine failure, 4 apply decision-gate stop (C-05).
/// </summary>
public static class Program
{
    private const int Clean = 0;
    private const int UsageError = 2;
    private const int EngineFailure = 3;

    public static int Main(string[] args) => Run(
        args,
        JobRegistry.Jobs,
        HookRegistry.Hooks,
        new FileSystem(),
        TimeProvider.System,
        new SystemEnvironment(),
        new SystemProcessRunner(),
        Console.In,
        Console.Out,
        Console.Error);

    /// <summary>The host with every service and stream injected - what <see cref="Main"/> wires to the real world and a test wires to fakes. The job map is a parameter, so a test dispatches its own jobs without touching the shipped registry.</summary>
    public static int Run(
        string[] args,
        IReadOnlyDictionary<string, Func<IJob>> jobs,
        IReadOnlyDictionary<string, Func<IHook>> hooks,
        IFileSystem fs,
        TimeProvider clock,
        IEnvironment env,
        IProcessRunner proc,
        TextReader stdin,
        TextWriter stdout,
        TextWriter stderr)
    {
        if (args.Length == 0)
        {
            return Usage(stderr, jobs, hooks, null);
        }

        var command = args[0];
        var rest = new List<string>(args[1..]);

        try
        {
            return command switch
            {
                "version" => VersionCommand.Run(rest, stdout) ?? Usage(stderr, jobs, hooks, null),
                "config" => Config(rest, fs, env, stdout, stderr, jobs, hooks),
                "hook" => HookCommand.Run(rest, hooks, stdin, fs, env, proc, () => Compose(fs, env, env.CurrentDirectory), stderr)
                    ?? Usage(stderr, jobs, hooks, rest.Count == 0 ? null : $"unknown hook: {rest[0]}"),
                _ => Job(command, rest, jobs, hooks, fs, clock, env, proc, stdout, stderr),
            };
        }
        catch (OptionsException ex)
        {
            return Fault(stderr, ex.Message, UsageError);
        }
#pragma warning disable CA1031 // The host's last line of defence: a job's escaping exception is exit 3 with its reason, never a stack trace on the user's terminal (C-05).
        catch (Exception ex)
        {
            return Fault(stderr, $"engine failed: {ex.GetType().Name}: {ex.Message}", EngineFailure);
        }
#pragma warning restore CA1031
    }

    private static int Config(
        List<string> rest,
        IFileSystem fs,
        IEnvironment env,
        TextWriter stdout,
        TextWriter stderr,
        IReadOnlyDictionary<string, Func<IJob>> jobs,
        IReadOnlyDictionary<string, Func<IHook>> hooks)
    {
        if (rest.Count == 0 || rest[0] != "show")
        {
            return Usage(stderr, jobs, hooks, null);
        }

        rest.RemoveAt(0);
        var json = rest.Remove("--json");
        var root = env.CurrentDirectory;
        if (!TryTakeRoot(rest, ref root) || rest.Count > 0)
        {
            return Usage(stderr, jobs, hooks, null);
        }

        var options = Compose(fs, env, root);
        stdout.Write(json ? Json(options) : Table(options));
        return Clean;
    }

    private static int Job(
        string command,
        List<string> rest,
        IReadOnlyDictionary<string, Func<IJob>> jobs,
        IReadOnlyDictionary<string, Func<IHook>> hooks,
        IFileSystem fs,
        TimeProvider clock,
        IEnvironment env,
        IProcessRunner proc,
        TextWriter stdout,
        TextWriter stderr)
    {
        if (!jobs.TryGetValue(command, out var job))
        {
            return Usage(stderr, jobs, hooks, $"unknown job: {command}");
        }

        var root = env.CurrentDirectory;
        if (!TryTakeRoot(rest, ref root))
        {
            return Usage(stderr, jobs, hooks, null);
        }

        // Composition comes before the job is even constructed: a faulty layer stops the run
        // naming the layer, the key and the reason, before any work is done (R-8211).
        var options = Compose(fs, env, root);
        var result = job().Run(new JobContext(fs, clock, env, proc, options, root, rest));
        stdout.Write(result.Stdout);
        stderr.Write(result.Stderr);
        return result.ExitCode;
    }

    /// <summary>The two configuration files by their declared shapes: the machine file under the home directory, the instance file under the root being worked on (R-8210, C-04).</summary>
    private static LegislatorOptions Compose(IFileSystem fs, IEnvironment env, string root)
    {
        var shape = new LegislatorOptions();
        return OptionsComposer.Compose(
            fs,
            env,
            fs.Path.Combine(env.HomeDirectory, shape.MachineConfigFile.Value),
            fs.Path.Combine(root, shape.InstanceConfigFile.Value));
    }

    private static string Table(LegislatorOptions options)
    {
        var text = new StringBuilder();
        foreach (var (key, value, source) in options.Enumerate())
        {
            text.Append($"{key} = {value}  [{source.Keyword()}]\n");
        }

        return text.ToString();
    }

    /// <summary>The same table as one JSON object, written by hand so the shape stays AOT-trivial and the order stays the table's.</summary>
    private static string Json(LegislatorOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            foreach (var (key, value, source) in options.Enumerate())
            {
                json.WriteStartObject(key);
                json.WriteString("value", value);
                json.WriteString("source", source.Keyword());
                json.WriteEndObject();
            }

            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan) + "\n";
    }

    /// <summary>Takes <c>--root &lt;dir&gt;</c> out of the argument list wherever it stands; what remains belongs to the job. A <c>--root</c> without a directory is a usage error, not a silently ignored flag.</summary>
    private static bool TryTakeRoot(List<string> args, ref string root)
    {
        var at = args.IndexOf("--root");
        if (at < 0)
        {
            return true;
        }

        if (at + 1 >= args.Count)
        {
            return false;
        }

        root = args[at + 1];
        args.RemoveRange(at, 2);
        return true;
    }

    private static int Usage(
        TextWriter stderr,
        IReadOnlyDictionary<string, Func<IJob>> jobs,
        IReadOnlyDictionary<string, Func<IHook>> hooks,
        string? problem)
    {
        var text = new StringBuilder();
        text.Append("usage: legislator <job> [--root <dir>] [job args...]\n");
        text.Append("       legislator hook <name>   (reads the hook payload on stdin)\n");
        text.Append("       legislator config show [--json] [--root <dir>]\n");
        text.Append("       legislator version\n");
        if (jobs.Count > 0)
        {
            text.Append($"jobs: {string.Join(", ", jobs.Keys.Order(StringComparer.Ordinal))}\n");
        }

        if (hooks.Count > 0)
        {
            text.Append($"hooks: {string.Join(", ", hooks.Keys.Order(StringComparer.Ordinal))}\n");
        }

        if (problem is not null)
        {
            text.Append($"{problem}\n");
        }

        stderr.Write(text.ToString());
        return UsageError;
    }

    private static int Fault(TextWriter stderr, string reason, int code)
    {
        stderr.Write($"{reason}\n");
        return code;
    }
}
