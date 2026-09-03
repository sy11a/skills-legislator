using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>
/// The fixture every `check_hooks.py` twin shares (R-8206). The ruler pipes a crafted Claude
/// Code payload into one hook and reads the process's exit code and stderr; the twin drives the
/// same argv through the CLI host in process, so what is compared is what the ruler compares.
/// The ruler's temporary trees become fake ones and its real repositories become git answered
/// at its interface - the `RunJobs.TrackingGit` precedent - because every assertion here is
/// about the JUDGEMENT, and none about whether git works.
/// </summary>
internal static class RunHooks
{
    public const string Root = "/r";

    /// <summary>The ruler's `run_hook(script, payload)` and `run_hook_raw(script, text)`, in process: the same argv the binary is given, the same bytes on stdin.</summary>
    public static (int Exit, string Out, string Err) Run(
        string hook, string stdin, IFileSystem fs, IProcessRunner? proc = null, IEnvironment? env = null)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["hook", hook], JobRegistry.Jobs, HookRegistry.Hooks, fs, TimeProvider.System,
            env ?? new FakeEnvironment(), proc ?? new FakeProcessRunner(),
            new StringReader(stdin), stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    /// <summary>The ruler's `edit_payload(file_path)` - a PreToolUse Edit naming the file and the directory it sits in.</summary>
    public static string EditPayload(string filePath)
    {
        var parent = filePath[..filePath.LastIndexOf('/')];
        return $$"""
            {"hook_event_name": "PreToolUse", "tool_name": "Edit",
             "tool_input": {"file_path": "{{filePath}}", "old_string": "a", "new_string": "b"},
             "cwd": "{{parent}}"}
            """;
    }

    /// <summary>The ruler's `bash_payload(command, cwd)`.</summary>
    public static string BashPayload(string command, string cwd) =>
        $$"""
        {"hook_event_name": "PreToolUse", "tool_name": "Bash",
         "tool_input": {"command": {{Quote(command)}}}, "cwd": "{{cwd}}"}
        """;

    /// <summary>The ruler's `stop_payload(cwd, stop_hook_active)`.</summary>
    public static string StopPayload(string cwd, bool stopHookActive = false) =>
        $$"""
        {"hook_event_name": "Stop", "stop_reason": "end_turn",
         "stop_hook_active": {{(stopHookActive ? "true" : "false")}}, "cwd": "{{cwd}}"}
        """;

    /// <summary>The ruler's `legislated-repo`: a manifest, one delivered rule, and whatever else the case adds.</summary>
    public static MockFileSystem LegislatedRepo()
    {
        var fs = new MockFileSystem();
        fs.AddFile($"{Root}/docs/ai/manifest.json", new MockFileData("{}"));
        fs.AddFile($"{Root}/docs/ai/rules/core/x.md", new MockFileData("## X\n"));
        return fs;
    }

    /// <summary>
    /// The ruler's `make_conduct_repo`, at git's interface: a repository on
    /// <paramref name="current"/> whose default branch is <paramref name="defaultBranch"/>.
    /// </summary>
    public static FakeProcessRunner ConductGit(string current = "master", string defaultBranch = "master") => new()
    {
        OnRun = (_, args, _) =>
        {
            if (args.Contains("refs/remotes/origin/HEAD"))
            {
                return new ProcessResult(0, $"origin/{defaultBranch}\n", "");
            }

            return args.Contains("HEAD")
                ? new ProcessResult(0, $"{current}\n", "")
                : new ProcessResult(1, "", "");
        },
    };

    /// <summary>The ruler's detached checkout: `symbolic-ref HEAD` refuses, and nothing else changes.</summary>
    public static FakeProcessRunner DetachedGit() => new()
    {
        OnRun = (_, args, _) => args.Contains("refs/remotes/origin/HEAD")
            ? new ProcessResult(0, "origin/master\n", "")
            : new ProcessResult(1, "", ""),
    };

    /// <summary>The ruler's `make_legislated_git_repo` plus its dirtying, at git's interface: the toplevel, and the porcelain lines the case declares.</summary>
    public static FakeProcessRunner OkfGit(string? toplevel, params string[] porcelain) => new()
    {
        OnRun = (_, args, _) =>
        {
            if (args.Contains("--show-toplevel"))
            {
                return toplevel is null
                    ? new ProcessResult(128, "", "fatal: not a git repository\n")
                    : new ProcessResult(0, $"{toplevel}\n", "");
            }

            return new ProcessResult(0, porcelain.Length == 0 ? "" : string.Join('\n', porcelain) + "\n", "");
        },
    };

    /// <summary>A JSON string literal for a command line that carries quotes and backslashes of its own.</summary>
    private static string Quote(string text) => System.Text.Json.JsonSerializer.Serialize(text);
}
