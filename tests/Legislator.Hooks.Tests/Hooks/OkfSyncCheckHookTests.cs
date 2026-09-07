using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Hooks.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Hooks.Tests.Hooks;

/// <summary>
/// The Stop hook that reminds a session to sync the OKF when the sources moved without it
/// (C-11) - the enforcement arm of okf.md's sync law. Its scope is deliberately narrow:
/// uncommitted working-tree state only, in a legislated git repository, at most once per stop.
/// Every other shape - no git, no manifest, a clean tree, a second firing - is silence.
/// </summary>
public sealed class OkfSyncCheckHookTests
{
    private static readonly LegislatorOptions Options = new();

    /// <summary>Git at its interface: the toplevel it reports, and the porcelain lines it prints for it.</summary>
    private static FakeProcessRunner Git(string? toplevel, params string[] porcelain) => new()
    {
        OnRun = (_, args, _) =>
        {
            if (args.Contains("--show-toplevel"))
            {
                return toplevel is null ? new ProcessResult(128, "", "fatal: not a git repository\n") : new ProcessResult(0, toplevel + "\n", "");
            }

            return new ProcessResult(0, string.Join('\n', porcelain) + (porcelain.Length > 0 ? "\n" : ""), "");
        },
    };

    private static MockFileSystem Legislated()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/docs/ai/manifest.json", new MockFileData("{}"));
        return fs;
    }

    private static HookResult Judge(FakeProcessRunner git, MockFileSystem? fs = null, bool stopHookActive = false)
    {
        var raw = $$"""
            {"hook_event_name": "Stop", "stop_reason": "end_turn",
             "stop_hook_active": {{(stopHookActive ? "true" : "false")}}, "cwd": "/r"}
            """;

        return new OkfSyncCheckHook().Run(
            new HookContext(HookPayload.Parse(raw), fs ?? Legislated(), new FakeEnvironment(), git, Options));
    }

    [Fact]
    public void Sources_changed_without_the_okf_is_a_reminder()
    {
        var result = Judge(Git("/r", " M src/a.txt"));

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("docs/okf", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sources_changed_together_with_the_okf_is_silence()
    {
        Assert.Equal(0, Judge(Git("/r", " M src/a.txt", " M docs/okf/log.md")).ExitCode);
    }

    [Fact]
    public void A_clean_tree_is_silence()
    {
        Assert.Equal(0, Judge(Git("/r")).ExitCode);
    }

    /// <summary>The OKF moving on its own is not the case this hook exists for.</summary>
    [Fact]
    public void The_okf_changing_alone_is_silence()
    {
        Assert.Equal(0, Judge(Git("/r", " M docs/okf/log.md")).ExitCode);
    }

    /// <summary>Claude Code sets this when a Stop hook already fired for this stop; without the guard the reminder would fire on its own reminder.</summary>
    [Fact]
    public void A_second_firing_for_the_same_stop_is_silence()
    {
        Assert.Equal(0, Judge(Git("/r", " M src/a.txt"), stopHookActive: true).ExitCode);
    }

    [Fact]
    public void Outside_a_legislated_repo_it_is_silence()
    {
        Assert.Equal(0, Judge(Git("/r", " M src/a.txt"), fs: new MockFileSystem()).ExitCode);
    }

    [Fact]
    public void Outside_a_git_repo_it_is_silence()
    {
        Assert.Equal(0, Judge(Git(toplevel: null, porcelain: " M src/a.txt")).ExitCode);
    }

    /// <summary>Git absent is not git refusing, but for this hook both end in silence: it can report nothing it has not read.</summary>
    [Fact]
    public void Git_being_absent_is_silence()
    {
        var absent = new FakeProcessRunner { OnRun = (_, _, _) => throw new ProcessStartException("git", new IOException("no git")) };

        Assert.Equal(0, Judge(absent).ExitCode);
    }

    /// <summary>A rename's porcelain line names two paths; the one that changed is the destination.</summary>
    [Fact]
    public void A_rename_is_read_at_its_destination()
    {
        Assert.Equal(2, Judge(Git("/r", "R  docs/old.md -> src/moved.txt")).ExitCode);
        Assert.Equal(0, Judge(Git("/r", "R  src/old.txt -> docs/okf/moved.md")).ExitCode);
    }

    /// <summary>A path with a space is quoted by porcelain; the quotes are not part of the path.</summary>
    [Fact]
    public void A_quoted_path_is_read_without_its_quotes()
    {
        Assert.Equal(2, Judge(Git("/r", "?? \"src/a file.txt\"")).ExitCode);
    }

    /// <summary>A directory whose name merely starts with the sources directory's is a different directory.</summary>
    [Fact]
    public void A_path_that_only_starts_like_the_sources_dir_is_not_the_sources_dir()
    {
        Assert.Equal(0, Judge(Git("/r", " M srcold/a.txt")).ExitCode);
    }

    [Fact]
    public void Malformed_stdin_is_silence()
    {
        var result = new OkfSyncCheckHook().Run(
            new HookContext(HookPayload.Parse("not json"), Legislated(), new FakeEnvironment(), Git("/r", " M src/a.txt"), Options));

        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>The manifest is looked for at the git toplevel, not at the payload's cwd - the hook's own docstring says so.</summary>
    [Fact]
    public void The_manifest_is_looked_for_at_the_git_toplevel()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/top/docs/ai/manifest.json", new MockFileData("{}"));
        var raw = """{"hook_event_name": "Stop", "stop_hook_active": false, "cwd": "/top/deep/inside"}""";

        var result = new OkfSyncCheckHook().Run(
            new HookContext(HookPayload.Parse(raw), fs, new FakeEnvironment(), Git("/top", " M src/a.txt"), Options));

        Assert.Equal(2, result.ExitCode);
    }
}
