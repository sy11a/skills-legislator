using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>
/// The named twins of the `hooks.json` well-formedness assertions (R-8206). These read the
/// shipped file rather than a fixture, because the claim IS about the delivered registration:
/// a matcher naming a tool Claude Code does not have registers a hook that never fires. The
/// assertions bound to the file naming a Python script - the two launcher checks and the
/// `.py` command shape - were re-cut in T-13 together with the line they described, and their
/// twins are the three below: the command IS the binary and its hook name, the name is one the
/// binary answers to, and the Bash matcher registers the git-conduct guard. The launcher pair
/// became one assertion running the real command line through a PATH, and its twin drives the
/// hook NAME the file carries rather than one written here - PATH resolution stays the ruler's
/// half, being a claim about this machine, while the judgement is this side's.
/// </summary>
public sealed class HooksJsonTwins
{
    /// <summary>The tools a Claude Code matcher may name - the ruler's `KNOWN_TOOLS`, which is the same closed set on both arms.</summary>
    private static readonly string[] KnownTools =
    [
        "Edit", "Write", "MultiEdit", "NotebookEdit", "Bash", "Read",
        "Glob", "Grep", "WebFetch", "WebSearch", "Task", "NotebookRead",
    ];

    /// <summary>The four names the binary answers to - the ruler's `KNOWN_HOOKS`, the same closed set on both arms.</summary>
    private static readonly string[] KnownHooks =
    [
        "guard_owned_files", "guard_git_conduct", "format_on_edit", "okf_sync_check",
    ];

    private static JsonNode Read()
    {
        var path = Path.Combine(Labels.RepoRoot(), "plugin", "hooks", "hooks.json");

        return JsonNode.Parse(File.ReadAllText(path))
            ?? throw new JsonException($"{path} parsed to nothing");
    }

    [Fact]
    [Parity("hooks", "hooks.json parses as JSON")]
    public void Hooks_json_parses_as_json()
    {
        Assert.NotNull(Read());
    }

    [Fact]
    [Parity("hooks", "hooks.json has at least one event")]
    public void Hooks_json_has_at_least_one_event()
    {
        var events = Read()["hooks"]?.AsObject();

        Assert.NotNull(events);
        Assert.NotEmpty(events);
    }

    [Fact]
    [Parity("hooks", "{} entries form a list")]
    public void Every_events_entries_form_a_list()
    {
        foreach (var (name, entries) in Read()["hooks"]!.AsObject())
        {
            Assert.True(entries is JsonArray, $"{name} entries are not a list");
        }
    }

    [Fact]
    [Parity("hooks", "{} matcher names real tools ({})")]
    public void Every_matcher_names_real_tools()
    {
        foreach (var (name, entries) in Read()["hooks"]!.AsObject())
        {
            foreach (var entry in entries!.AsArray())
            {
                var matcher = entry?["matcher"]?.GetValue<string>();
                if (matcher is null)
                {
                    continue;
                }

                var unknown = matcher.Split('|').Where(t => !KnownTools.Contains(t, StringComparer.Ordinal)).ToList();
                Assert.True(unknown.Count == 0, $"{name} matcher '{matcher}' names {string.Join(", ", unknown)}");
            }
        }
    }

    /// <summary>Every `command` string the registration carries, with the event that carries it.</summary>
    private static IEnumerable<(string Event, string Command)> Commands()
    {
        foreach (var (name, entries) in Read()["hooks"]!.AsObject())
        {
            foreach (var entry in entries!.AsArray())
            {
                foreach (var hook in entry?["hooks"]?.AsArray() ?? [])
                {
                    yield return (name, hook?["command"]?.GetValue<string>() ?? "");
                }
            }
        }
    }

    [Fact]
    [Parity("hooks", "{} command runs the binary behind a PATH guard per R-8208")]
    public void Every_command_runs_the_binary_behind_a_path_guard()
    {
        // R-8208, amended 2026-09-08 (ADR-0011): the binary carries the hook and no
        // interpreter does, but the command line is a shell guard first - a machine
        // without the arm exits 0 in silence instead of printing `command not found`
        // on every tool call (R-8215).
        foreach (var (name, command) in Commands())
        {
            Assert.DoesNotContain(".py", command, StringComparison.Ordinal);
            Assert.Contains("command -v legislator", command, StringComparison.Ordinal);
            Assert.Contains("exec legislator hook ", command, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Parity("hooks", "{} command names a known hook per C-11")]
    public void Every_command_names_a_known_hook()
    {
        foreach (var (name, command) in Commands())
        {
            var at = command.IndexOf("exec legislator hook ", StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }

            var hook = command[(at + "exec legislator hook ".Length)..].Split(' ')[0];
            Assert.True(KnownHooks.Contains(hook, StringComparer.Ordinal),
                        $"{name} names '{hook}', which the binary does not answer to");
        }
    }

    [Fact]
    [Parity("hooks", "PreToolUse has a Bash entry running the git-conduct guard per R-641")]
    public void PreToolUse_has_a_bash_entry_running_the_git_conduct_guard()
    {
        var registered = Read()["hooks"]?["PreToolUse"]?.AsArray()
            .Where(e => e?["matcher"]?.GetValue<string>() == "Bash")
            .SelectMany(e => e?["hooks"]?.AsArray() ?? [])
            .Any(h => (h?["command"]?.GetValue<string>() ?? "").Contains(
                "hook guard_git_conduct", StringComparison.Ordinal));

        Assert.True(registered, "no Bash matcher entry registers `legislator hook guard_git_conduct`");
    }

    /// <summary>
    /// The registered guard command, driven by the name `hooks.json` carries rather than one
    /// spelled here: a registration pointing at the wrong hook would otherwise pass a twin that
    /// hardcodes the right one. Block and allow are ONE assertion for the ruler's reason - the
    /// allow half alone is satisfied by any command that exits 0, a guard that was never wired
    /// included, so only the pair catches removal (H-007).
    /// </summary>
    [Fact]
    [Parity("hooks", "hooks.json's PreToolUse guard blocks owned and allows ordinary per R-8208")]
    public void The_registered_guard_blocks_owned_and_allows_ordinary()
    {
        var registered = Commands()
            .FirstOrDefault(c => c.Command.Contains("guard_owned_files", StringComparison.Ordinal));
        Assert.False(registered.Command is null or "", "no PreToolUse command registers the owned-file guard");

        var hook = registered.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];

        var blocked = RunHooks.Run(
            hook, RunHooks.EditPayload($"{RunHooks.Root}/docs/ai/rules/core/x.md"), RunHooks.LegislatedRepo());
        var allowed = RunHooks.Run(
            hook, RunHooks.EditPayload($"{RunHooks.Root}/src/a.cs"), RunHooks.LegislatedRepo());

        Assert.True(blocked.Exit == 2 && allowed.Exit == 0,
                    $"owned edit exited {blocked.Exit} (want 2), ordinary edit exited {allowed.Exit} (want 0)");
    }

    /// <summary>Where a POSIX shell is looked for, in order.</summary>
    private static readonly string[] PosixShells = ["/bin/sh", "/usr/bin/sh"];

    /// <summary>
    /// R-8215 (ADR-0011): on a machine that has no arm the hook gives up quietly - exit 0 and
    /// nothing on stderr - so a legislated repository the edition reaches before the install
    /// loses neither its turn nor its output. Driven as a real process through a PATH that holds
    /// `sh` and nothing else, because the property belongs to the command line hooks.json
    /// carries and not to any hook this suite can call in-process.
    /// </summary>
    [Fact]
    [Parity("hooks", "hooks.json's guard fails open and silent with no arm on PATH per R-8215")]
    public void The_registered_guard_fails_open_and_silent_with_no_arm()
    {
        var registered = Commands()
            .FirstOrDefault(c => c.Command.Contains("guard_owned_files", StringComparison.Ordinal));
        Assert.False(registered.Command is null or "", "no PreToolUse command registers the owned-file guard");

        var shell = PosixShells.FirstOrDefault(File.Exists);
        Assert.False(shell is null, "no POSIX shell on this machine to drive the command line with");

        var dir = Directory.CreateTempSubdirectory("legislator-noarm-");
        try
        {
            File.CreateSymbolicLink(Path.Combine(dir.FullName, "sh"), shell!);

            var psi = new ProcessStartInfo(shell!)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(registered.Command);
            psi.Environment.Clear();
            psi.Environment["PATH"] = dir.FullName;

            using var proc = Process.Start(psi)!;
            try
            {
                proc.StandardInput.Write("{\"tool_name\":\"Edit\",\"tool_input\":{\"file_path\":\"/tmp/x.md\"}}");
                proc.StandardInput.Close();
            }
            catch (IOException)
            {
                // The guard gave up before reading stdin, which is the property under test:
                // a broken pipe here is the fail-open path working, not a failure of it.
            }
            var err = proc.StandardError.ReadToEnd();
            proc.WaitForExit(15_000);

            Assert.True(proc.ExitCode == 0 && err.Length == 0,
                        $"exit={proc.ExitCode}, stderr={err}");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
