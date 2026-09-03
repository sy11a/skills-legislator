using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>The named twins of every `check_hooks.py` assertion the OKF-sync reminder answers (R-8206).</summary>
public sealed class OkfSyncCheckTwins
{
    private const string Hook = "okf_sync_check";

    /// <summary>The ruler's legislated git repository, whose working tree carries the porcelain lines the case declares.</summary>
    private static (int Exit, string Out, string Err) Okf(string[] dirty, bool stopHookActive = false)
    {
        var fs = new MockFileSystem();
        fs.AddFile($"{RunHooks.Root}/docs/ai/manifest.json", new MockFileData("{}"));

        return RunHooks.Run(
            Hook, RunHooks.StopPayload(RunHooks.Root, stopHookActive), fs,
            RunHooks.OkfGit(RunHooks.Root, dirty));
    }

    [Fact]
    [Parity("hooks", "dirty src/ only: exit 2")]
    [Parity("hooks", "reminder mentions docs/okf")]
    public void Dirty_src_only_exits_2_naming_the_okf()
    {
        var (exit, _, err) = Okf([" M src/a.txt"]);

        Assert.Equal(2, exit);
        Assert.Contains("docs/okf", err, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("hooks", "dirty src/ + docs/okf/: exit 0")]
    public void Dirty_src_and_okf_exits_0()
    {
        Assert.Equal(0, Okf([" M src/a.txt", "?? docs/okf/log.md"]).Exit);
    }

    [Fact]
    [Parity("hooks", "stop_hook_active true: exit 0 (loop-safe)")]
    public void Stop_hook_active_exits_0()
    {
        Assert.Equal(0, Okf([" M src/a.txt"], stopHookActive: true).Exit);
    }

    [Fact]
    [Parity("hooks", "clean tree: exit 0")]
    public void Clean_tree_exits_0()
    {
        Assert.Equal(0, Okf([]).Exit);
    }

    [Fact]
    [Parity("hooks", "non-legislated git repo: exit 0")]
    public void Non_legislated_git_repo_exits_0()
    {
        var (exit, _, _) = RunHooks.Run(
            Hook, RunHooks.StopPayload(RunHooks.Root), new MockFileSystem(),
            RunHooks.OkfGit(RunHooks.Root, " M src/a.txt"));

        Assert.Equal(0, exit);
    }

    [Fact]
    [Parity("hooks", "outside any git repo: exit 0")]
    public void Outside_any_git_repo_exits_0()
    {
        var (exit, _, _) = RunHooks.Run(
            Hook, RunHooks.StopPayload("/not-a-repo"), new MockFileSystem(),
            RunHooks.OkfGit(toplevel: null));

        Assert.Equal(0, exit);
    }

    [Fact]
    [Parity("hooks", "okf_sync_check: malformed stdin allowed (exit 0)")]
    public void Malformed_stdin_allowed()
    {
        Assert.Equal(0, RunHooks.Run(Hook, "not json", new MockFileSystem(), RunHooks.OkfGit(RunHooks.Root, " M src/a.txt")).Exit);
    }
}
