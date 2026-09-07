using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the okf-debt job answers (R-8206).
/// The ruler builds a real repository and dates its commits with `GIT_COMMITTER_DATE`; the
/// twin scripts the same dates through the injected process runner, so what differs is where
/// the date comes from, never what the job concludes from it. Absent git is the ruler's
/// PATH-shim, here a runner that cannot start the executable.
/// </summary>
public sealed class OkfDebtTwins
{
    private const string Source = "public class WidgetStore { }\n";

    private const string Removed =
        "---\ntype: Concept\nstatus: removed\n---\n\n# Payments\n\nThis concept was removed. It named `src/App/Gone.cs`.\n";

    private const string Live =
        "---\ntype: Concept\nstatus: implemented\n---\n\n# Billing\n\nStill names `src/App/Gone.cs`.\n";

    /// <summary>The ruler's `make_repo`: documents under `docs/okf/`, sources at their own paths.</summary>
    private static MockFileSystem Repo(Dictionary<string, string> docs, Dictionary<string, string> sources)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/okf");
        foreach (var (name, text) in docs)
        {
            fs.AddFile($"/r/docs/okf/{name}", new MockFileData(text));
        }

        foreach (var (rel, text) in sources)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        return fs;
    }

    /// <summary>The ruler's dated commits: a committer date per repository-relative path, anything else untracked.</summary>
    private static FakeProcessRunner Git(Dictionary<string, string> dates) => new()
    {
        OnRun = (_, args, _) => new ProcessResult(0, dates.GetValueOrDefault(args[^1], "") + "\n", ""),
    };

    /// <summary>The ruler's PATH shim, which leaves no `git` to find.</summary>
    private static FakeProcessRunner NoGit() => new()
    {
        OnRun = (file, _, _) => throw ProcessStartException.For(file, new InvalidOperationException("no such file")),
    };

    /// <summary>The ruler's `run(root, "okf-debt")`, in process: the same argv the binary is given.</summary>
    private static (int Exit, string Out, string Err) OkfDebt(IFileSystem fs, IProcessRunner git)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["okf-debt", "--root", "/r"], JobRegistry.Jobs, HookRegistry.Hooks, fs, TimeProvider.System,
            new FakeEnvironment(), git, TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    private static (int Exit, string Out, string Err) OkfDebt(
        Dictionary<string, string> docs,
        Dictionary<string, string> sources,
        Dictionary<string, string> dates) => OkfDebt(Repo(docs, sources), Git(dates));

    [Fact]
    [Parity("engine", "a source 59 days newer than its document is debt")]
    public void A_source_59_days_newer_than_its_document_is_debt()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["widgets.md"] = "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
            });

        Assert.Equal(1, exit);
        Assert.Contains(
            "widgets.md: okf-sync-debt: src/App/WidgetStore.cs changed 59 days after this document",
            output,
            StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "19 days is inside the 30-day threshold")]
    public void Nineteen_days_is_inside_the_thirty_day_threshold()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["widgets.md"] = "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-01-20T12:00:00+00:00",
            });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "a document that moved with its source is clean")]
    public void A_document_that_moved_with_its_source_is_clean()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["widgets.md"] = "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`, now with Flush.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-03-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
            });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "an untracked tree yields no debt findings")]
    public void An_untracked_tree_yields_no_debt_findings()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["widgets.md"] = "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source },
            []);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "a directory anchor's history (the union of everything beneath it) never produces debt")]
    public void A_directory_anchors_history_never_produces_debt()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["dirdebt.md"] = "# Dir debt\n\nCode lives under `src/`.\n" },
            new() { ["src/App/Widget.cs"] = "public class Widget { }\n" },
            new()
            {
                ["docs/okf/dirdebt.md"] = "2026-01-01T12:00:00+00:00",
                ["src"] = "2026-03-01T12:00:00+00:00",
                ["src/"] = "2026-03-01T12:00:00+00:00",
            });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "okf_debt_git_absent (control): with git the 85-day debt is a finding")]
    public void With_git_the_85_day_debt_is_a_finding()
    {
        var (exit, output, _) = OkfDebt(
            new() { ["mod.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\nDescribes `src/mod.py`.\n" },
            new() { ["src/mod.py"] = "x = 1\n" },
            new()
            {
                ["docs/okf/mod.md"] = "2026-06-01T00:00:00+00:00",
                ["src/mod.py"] = "2026-08-25T00:00:00+00:00",
            });

        Assert.Equal(1, exit);
        Assert.Contains("okf-sync-debt", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "okf_debt_git_absent: without git the job exits as a check failure, never clean")]
    public void Without_git_the_job_exits_as_a_check_failure()
    {
        var (exit, _, error) = OkfDebt(
            Repo(new() { ["mod.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\nDescribes `src/mod.py`.\n" },
                 new() { ["src/mod.py"] = "x = 1\n" }),
            NoGit());

        Assert.DoesNotContain(exit, (int[])[0, 1, 2]);
        Assert.Contains("git", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The ruler asserts an absence — nothing on stdout — which an engine that does not know
    /// the job at all satisfies for the wrong reason. The twin therefore asserts more than the
    /// ruler does: the silence AND the loud exit that must accompany it (T-07's pattern).
    /// </summary>
    [Fact]
    [Parity("engine", "okf_debt_git_absent (stdout control): no findings text a stdout-reader could mistake")]
    public void Without_git_no_findings_text_reaches_stdout()
    {
        var (exit, output, error) = OkfDebt(
            Repo(new() { ["mod.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\nDescribes `src/mod.py`.\n" },
                 new() { ["src/mod.py"] = "x = 1\n" }),
            NoGit());

        Assert.Equal("", output);
        Assert.Equal(3, exit);
        Assert.Contains("git", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "okf_debt_git_absent (boundary): nothing to measure is clean even without git")]
    public void Nothing_to_measure_is_clean_even_without_git()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/src/a.py", new MockFileData("x\n"));

        var (exit, output, _) = OkfDebt(fs, NoGit());

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    /// <summary>
    /// The ruler asserts an absence again — `payments.md` nowhere in the output — so the twin
    /// pins the whole of stdout instead: the removed document is silent and the live sibling
    /// is the only line there is.
    /// </summary>
    [Fact]
    [Parity("engine", "removed_doc_no_debt: a status: removed document accrues no okf-sync-debt")]
    public void A_removed_document_accrues_no_okf_sync_debt()
    {
        var (exit, output, _) = RemovedAndLive();

        Assert.Equal(1, exit);
        Assert.Equal(
            "docs/okf/billing.md: okf-sync-debt: src/App/Gone.cs changed 90 days after this document\n",
            output);
    }

    [Fact]
    [Parity("engine", "removed_doc_no_debt (control): the live sibling still accrues debt")]
    public void The_live_sibling_still_accrues_debt()
    {
        var (exit, output, _) = RemovedAndLive();

        Assert.Equal(1, exit);
        Assert.Contains("billing.md", output, StringComparison.Ordinal);
    }

    private static (int Exit, string Out, string Err) RemovedAndLive() => OkfDebt(
        new() { ["payments.md"] = Removed, ["billing.md"] = Live },
        new() { ["src/App/Gone.cs"] = "public class Gone { }\n" },
        new()
        {
            ["docs/okf/payments.md"] = "2026-01-01T12:00:00+00:00",
            ["docs/okf/billing.md"] = "2026-01-01T12:00:00+00:00",
            ["src/App/Gone.cs"] = "2026-04-01T12:00:00+00:00",
        });
}
