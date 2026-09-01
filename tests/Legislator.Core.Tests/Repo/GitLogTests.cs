using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Repo;

/// <summary>
/// The git seam (C-08): the one place the system asks history when a document went stale.
/// The Python's `git_iso` runs `git log -1 --format=%cI -- <rel>` and treats every failure
/// as "no date"; the one failure it must NOT treat that way is git being absent altogether,
/// which is why the answer carries availability beside the date.
/// </summary>
public sealed class GitLogTests
{
    private static readonly LegislatorOptions Options = new();

    [Fact]
    public void Newest_commit_asks_git_for_one_committer_date_of_one_path()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, "2026-01-01T12:00:00+00:00\n", "") };

        GitLog.NewestCommit(runner, Options, "/r", "docs/okf/widgets.md");

        var call = Assert.Single(runner.Calls);
        Assert.Equal("git", call.FileName);
        Assert.Equal(["log", "-1", "--format=%cI", "--", "docs/okf/widgets.md"], call.Args);
        Assert.Equal("/r", call.WorkingDirectory);
    }

    [Fact]
    public void Newest_commit_returns_the_trimmed_iso_date_git_printed()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, "2026-03-01T12:00:00+00:00\n", "") };

        var (iso, available) = GitLog.NewestCommit(runner, Options, "/r", "src/App/WidgetStore.cs");

        Assert.Equal("2026-03-01T12:00:00+00:00", iso);
        Assert.True(available);
    }

    [Fact]
    public void An_untracked_path_has_no_date_and_git_is_still_available()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, "\n", "") };

        var (iso, available) = GitLog.NewestCommit(runner, Options, "/r", "src/new.cs");

        Assert.Null(iso);
        Assert.True(available);
    }

    /// <summary>A tree that is no repository at all: git runs and fails. That is "no history", not "no git" — the Python's `git_iso` returns None here and the job stays clean.</summary>
    [Fact]
    public void A_tree_that_is_not_a_repository_has_no_date_and_git_is_still_available()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(128, "", "fatal: not a git repository") };

        var (iso, available) = GitLog.NewestCommit(runner, Options, "/r", "docs/okf/widgets.md");

        Assert.Null(iso);
        Assert.True(available);
    }

    /// <summary>The one failure that is not "no history": the executable cannot be started. The Python asks `shutil.which("git")`; here the attempt itself is the probe, and it answers for a git that exists but cannot run too.</summary>
    [Fact]
    public void Git_that_cannot_be_started_is_reported_unavailable()
    {
        var runner = new FakeProcessRunner
        {
            OnRun = (file, _, _) => throw ProcessStartException.For(file, new InvalidOperationException("no such file")),
        };

        var (iso, available) = GitLog.NewestCommit(runner, Options, "/r", "docs/okf/widgets.md");

        Assert.Null(iso);
        Assert.False(available);
    }

    [Fact]
    public void A_run_killed_on_timeout_has_no_date_and_git_stays_available()
    {
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(-1, "", "timeout") };

        var (iso, available) = GitLog.NewestCommit(runner, Options, "/r", "docs/okf/widgets.md");

        Assert.Null(iso);
        Assert.True(available);
    }

    [Fact]
    public void The_executable_and_the_timeout_come_from_the_options_model()
    {
        TimeSpan seen = default;
        var runner = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, "", "") };
        var options = Options with
        {
            GitExecutable = new("git.exe", OptionsLayer.Instance),
            GitTimeoutSeconds = new(3, OptionsLayer.Instance),
        };
        var recorder = new RecordingProcessRunner(runner, t => seen = t);

        GitLog.NewestCommit(recorder, options, "/r", "docs/okf/widgets.md");

        Assert.Equal("git.exe", Assert.Single(runner.Calls).FileName);
        Assert.Equal(TimeSpan.FromSeconds(3), seen);
    }

    /// <summary>The fake records what it was called with but not the timeout; this thin wrapper is the only way to see the value the seam passed.</summary>
    private sealed class RecordingProcessRunner(IProcessRunner inner, Action<TimeSpan> onTimeout) : IProcessRunner
    {
        public ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
        {
            onTimeout(timeout);
            return inner.Run(fileName, args, workingDirectory, timeout);
        }
    }
}
