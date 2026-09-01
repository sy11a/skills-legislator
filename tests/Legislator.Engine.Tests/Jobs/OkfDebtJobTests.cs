using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The okf-debt job, branch by branch. The fixtures mirror `check_engine.py`'s `make_repo`
/// and the dated commits the ruler builds around it — here the dates are answered by the
/// scripted process runner instead of a real repository, so the branch under test is what the
/// job does with a date, never what git does with a tree.
/// </summary>
public sealed class OkfDebtJobTests
{
    private const string Source = "public class WidgetStore { }\n";

    /// <summary>Committer dates by repository-relative path; a path the map does not name is untracked.</summary>
    private static FakeProcessRunner Git(Dictionary<string, string> dates) => new()
    {
        OnRun = (_, args, _) => new ProcessResult(0, dates.GetValueOrDefault(args[^1], "") + "\n", ""),
    };

    private static FakeProcessRunner NoGit() => new()
    {
        OnRun = (file, _, _) => throw ProcessStartException.For(file, new InvalidOperationException("no such file")),
    };

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

    private static JobResult RunOn(
        IFileSystem fs, IProcessRunner git, LegislatorOptions? options = null) =>
        new OkfDebtJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), git, options ?? new LegislatorOptions(), "/r", []));

    private static JobResult Run(
        Dictionary<string, string> docs,
        Dictionary<string, string> sources,
        Dictionary<string, string> dates,
        LegislatorOptions? options = null) =>
        RunOn(Repo(docs, sources), Git(dates), options);

    private static Dictionary<string, string> OneDocument(string body) =>
        new() { ["widgets.md"] = $"# Widgets\n\n{body}\n" };

    [Fact]
    public void A_source_that_moved_on_past_the_threshold_is_debt()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
            });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/okf/widgets.md: okf-sync-debt: src/App/WidgetStore.cs changed 59 days after this document\n",
            result.Stdout);
    }

    [Fact]
    public void A_source_inside_the_threshold_is_not_debt()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-01-20T12:00:00+00:00",
            });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>The threshold is the options model's, never a literal in the job (R-8209): raise it and the same 59 days stop being debt.</summary>
    [Fact]
    public void The_threshold_comes_from_the_options_model()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
            },
            new LegislatorOptions { OkfDebtDays = new(90, OptionsLayer.Instance) });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void An_untracked_document_is_skipped_however_old_its_sources_are()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source },
            new() { ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void An_untracked_source_produces_no_debt()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source },
            new() { ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>A directory's history is the union of everything beneath it, so it can never say whether one document went stale — the job never even asks (core/okf.md).</summary>
    [Fact]
    public void A_directory_anchor_is_never_asked_about()
    {
        var git = Git(new()
        {
            ["docs/okf/dirdebt.md"] = "2026-01-01T12:00:00+00:00",
            ["src"] = "2026-03-01T12:00:00+00:00",
            ["src/"] = "2026-03-01T12:00:00+00:00",
        });

        var result = RunOn(
            Repo(new() { ["dirdebt.md"] = "# Dir debt\n\nCode lives under `src/`.\n" },
                 new() { ["src/App/Widget.cs"] = Source }),
            git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.DoesNotContain(git.Calls, c => c.Args[^1] is "src" or "src/");
    }

    /// <summary>A broken anchor is the anchors job's finding, not debt: the job asks git about nothing that is not there.</summary>
    [Fact]
    public void A_path_anchor_that_does_not_resolve_is_not_a_debt_source()
    {
        var git = Git(new() { ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00" });

        var result = RunOn(
            Repo(OneDocument("Implemented in `src/App/Gone.cs`."), new() { ["src/App/WidgetStore.cs"] = Source }),
            git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.DoesNotContain(git.Calls, c => c.Args[^1] == "src/App/Gone.cs");
    }

    [Fact]
    public void A_symbol_anchor_is_not_a_debt_source()
    {
        var git = Git(new()
        {
            ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
            ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
        });

        var result = RunOn(
            Repo(OneDocument("Handled by `WidgetStore`."), new() { ["src/App/WidgetStore.cs"] = Source }),
            git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void The_human_class_accrues_no_debt()
    {
        var result = Run(
            new()
            {
                ["glossary.md"] = "# Glossary\n\nNames `src/App/WidgetStore.cs`.\n",
                ["log.md"] = "# Log\n\nNames `src/App/WidgetStore.cs`.\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source },
            new()
            {
                ["docs/okf/glossary.md"] = "2026-01-01T12:00:00+00:00",
                ["docs/okf/log.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
            });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_removed_document_accrues_no_debt_while_its_live_sibling_does()
    {
        var result = Run(
            new()
            {
                ["payments.md"] = "---\ntype: Concept\nstatus: removed\n---\n\n# Payments\n\nIt named `src/App/Gone.cs`.\n",
                ["billing.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\n# Billing\n\nStill names `src/App/Gone.cs`.\n",
            },
            new() { ["src/App/Gone.cs"] = "public class Gone { }\n" },
            new()
            {
                ["docs/okf/payments.md"] = "2026-01-01T12:00:00+00:00",
                ["docs/okf/billing.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/Gone.cs"] = "2026-04-01T12:00:00+00:00",
            });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/okf/billing.md: okf-sync-debt: src/App/Gone.cs changed 90 days after this document\n",
            result.Stdout);
    }

    /// <summary>One finding per document, and it names the worst source — the others are quieter symptoms of the same staleness.</summary>
    [Fact]
    public void One_finding_per_document_names_the_source_that_moved_furthest()
    {
        var result = Run(
            OneDocument("Implemented in `src/App/WidgetStore.cs` and `src/App/Widget.cs`."),
            new() { ["src/App/WidgetStore.cs"] = Source, ["src/App/Widget.cs"] = Source },
            new()
            {
                ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
                ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
                ["src/App/Widget.cs"] = "2026-04-01T12:00:00+00:00",
            });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "docs/okf/widgets.md: okf-sync-debt: src/App/Widget.cs changed 90 days after this document\n",
            result.Stdout);
    }

    [Fact]
    public void Front_matter_and_fenced_code_carry_no_anchors()
    {
        var git = Git(new()
        {
            ["docs/okf/widgets.md"] = "2026-01-01T12:00:00+00:00",
            ["src/App/WidgetStore.cs"] = "2026-03-01T12:00:00+00:00",
        });

        var result = RunOn(
            Repo(
                new()
                {
                    ["widgets.md"] = "---\nsource: `src/App/WidgetStore.cs`\n---\n\n# Widgets\n\n"
                        + "```\nSee `src/App/WidgetStore.cs`.\n```\n",
                },
                new() { ["src/App/WidgetStore.cs"] = Source }),
            git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_repository_with_no_okf_bundle_is_clean_and_git_is_never_asked()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/src/mod.py", new MockFileData("x = 1\n"));
        var git = Git([]);

        var result = RunOn(fs, git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Empty(git.Calls);
    }

    /// <summary>R-665 (BL-069 F1): a verification job whose measuring instrument is absent fails loud — it never reports clean.</summary>
    [Fact]
    public void Git_unavailable_with_documents_to_measure_fails_loud()
    {
        var fs = Repo(OneDocument("Implemented in `src/App/WidgetStore.cs`."),
                      new() { ["src/App/WidgetStore.cs"] = Source });

        var thrown = Assert.ThrowsAny<Exception>(() => RunOn(fs, NoGit()));

        Assert.Contains("git", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The boundary of the same rule: with nothing to measure there is no instrument to miss.</summary>
    [Fact]
    public void Git_unavailable_with_nothing_to_measure_is_clean()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/src/mod.py", new MockFileData("x = 1\n"));

        var result = RunOn(fs, NoGit());

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }
}
