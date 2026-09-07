using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The anchors job, branch by branch. The fixtures mirror `check_engine.py`'s `make_repo`:
/// documents under `docs/okf/`, sources wherever the repository puts them — the shape the
/// parity ruler materialises on a real disk, here in memory.
/// </summary>
public sealed class AnchorsJobTests
{
    private static JobContext Ctx(IFileSystem fs) => new(
        fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
        new LegislatorOptions(), "/r", []);

    private static MockFileSystem Repo(Dictionary<string, string> docs, Dictionary<string, string> sources)
    {
        var fs = new MockFileSystem();
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

    private static JobResult Run(Dictionary<string, string> docs, Dictionary<string, string> sources) =>
        new AnchorsJob().Run(Ctx(Repo(docs, sources)));

    private const string Source = "public class WidgetStore { }\n";

    [Fact]
    public void A_resolving_document_produces_no_findings()
    {
        var result = Run(
            new() { ["widgets.md"] = "# Widgets\n\nSee `src/App/WidgetStore.cs` and `WidgetStore`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_path_that_does_not_exist_names_document_line_and_token()
    {
        var result = Run(
            new() { ["widgets.md"] = "# Widgets\n\nSee `src/App/Gone.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("docs/okf/widgets.md:3: path-anchor: src/App/Gone.cs → no such file\n", result.Stdout);
    }

    [Fact]
    public void A_symbol_absent_from_the_source_names_the_roots_it_looked_in()
    {
        var result = Run(
            new() { ["widgets.md"] = "# Widgets\n\nHandled by `LegacyProcessor`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("docs/okf/widgets.md:3: symbol-anchor: LegacyProcessor → not found in src/\n", result.Stdout);
    }

    [Fact]
    public void A_member_suffix_resolves_to_its_file()
    {
        var result = Run(
            new() { ["widgets.md"] = "# Widgets\n\n`src/App/WidgetStore.Flush()` writes.\n" },
            new() { ["src/App/WidgetStore.cs"] = "public class WidgetStore { void Flush() {} }\n" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>A glossary defines terms and a log records what was true; naming something since removed is correct there.</summary>
    [Fact]
    public void Human_class_documents_are_never_anchored()
    {
        var result = Run(
            new()
            {
                ["log.md"] = "# Log\n\n2026-01: removed `RetiredJob` and `src/App/Retired.cs`.\n",
                ["glossary.md"] = "# Glossary\n\n| `RetiredJob` | gone |\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>
    /// The human class is matched by name, ordinally, on every operating system: a
    /// case-insensitive comparison would silently exempt a `Glossary.md` that is an ordinary
    /// anchored document, and on a case-insensitive checkout (ADR-0005) that is exactly the
    /// file a Windows or macOS fleet member would produce. One document per repository, so
    /// the fake's own case policy never enters the answer.
    /// </summary>
    [Fact]
    public void A_document_whose_name_differs_only_in_case_from_the_human_class_is_anchored()
    {
        var result = Run(
            new() { ["Glossary.md"] = "# Glossary\n\nNames `src/App/Gone.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("docs/okf/Glossary.md:3: path-anchor: src/App/Gone.cs → no such file\n", result.Stdout);
    }

    [Fact]
    public void A_status_removed_document_leaves_the_anchored_class_but_its_live_sibling_does_not()
    {
        var result = Run(
            new()
            {
                ["payments.md"] = "---\ntype: Concept\nstatus: removed\n---\n\n# Payments\n\nThis concept was removed. It named `src/App/Gone.cs`.\n",
                ["billing.md"] = "---\ntype: Concept\nstatus: implemented\n---\n\n# Billing\n\nStill names `src/App/Gone.cs`.\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("docs/okf/billing.md:8: path-anchor: src/App/Gone.cs → no such file\n", result.Stdout);
    }

    [Fact]
    public void Front_matter_and_fenced_code_are_not_scanned()
    {
        var result = Run(
            new() { ["widgets.md"] = "---\ntitle: uses `src/App/Gone.cs` in its description\n---\n\n# Widgets\n\n```\n`src/App/AlsoGone.cs`\n```\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void Templates_globs_commands_and_short_tokens_are_not_anchors()
    {
        var result = Run(
            new()
            {
                ["widgets.md"] = "# Widgets\n\nTemplate `schemas/<type>/<version>.json`, glob `src/**/*.cs`, "
                    + "home `~/.config/app/settings.yaml`, absolute `/etc/hosts`, command `dotnet build`, "
                    + "field `contenthash`, short `Api`.\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void A_repository_with_no_okf_bundle_is_clean_not_an_error()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/src/App/WidgetStore.cs", new MockFileData(Source));

        var result = new AnchorsJob().Run(Ctx(fs));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>The bundle is walked recursively, as the Python's `rglob` walks it.</summary>
    [Fact]
    public void Documents_in_subdirectories_are_anchored_too()
    {
        var result = Run(
            new() { ["concepts/widget.md"] = "# Widget\n\nSee `src/App/Gone.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("docs/okf/concepts/widget.md:3: path-anchor: src/App/Gone.cs → no such file\n", result.Stdout);
    }

    /// <summary>Two documents, two findings, ordinal order — the ruler compares stdout byte for byte.</summary>
    [Fact]
    public void Findings_are_sorted_ordinally()
    {
        var result = Run(
            new()
            {
                ["zebra.md"] = "# Z\n\n`src/App/Gone.cs`\n",
                ["Alpha.md"] = "# A\n\n`src/App/Gone.cs`\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(
            "docs/okf/Alpha.md:3: path-anchor: src/App/Gone.cs → no such file\n"
            + "docs/okf/zebra.md:3: path-anchor: src/App/Gone.cs → no such file\n",
            result.Stdout);
    }

    /// <summary>
    /// Every site of an unresolved symbol reports, not just the first: the document and line
    /// are what makes a finding repairable.
    /// </summary>
    [Fact]
    public void Every_site_of_an_unresolved_symbol_reports()
    {
        var result = Run(
            new() { ["widgets.md"] = "# Widgets\n\n`LegacyProcessor` here.\n\nAnd `LegacyProcessor` again.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(
            "docs/okf/widgets.md:3: symbol-anchor: LegacyProcessor → not found in src/\n"
            + "docs/okf/widgets.md:5: symbol-anchor: LegacyProcessor → not found in src/\n",
            result.Stdout);
    }

    /// <summary>
    /// The job never swallows a read failure. `core/verification.md` reads stdout for findings,
    /// so a crash that printed nothing and exited 0 would read as a clean repository — the
    /// host turns the escaping exception into exit 3 (C-05).
    /// </summary>
    [Fact]
    public void An_unreadable_document_lets_the_exception_escape()
    {
        // On a real disk, because `MockFileSystem` returns empty text for a directory where
        // `System.IO` throws: the fake cannot express a document the process may not read.
        using var temp = new TempDirectory()
            .With("src/App/WidgetStore.cs", Source)
            .WithUnreadableDocument("docs/okf/locked.md");

        var ctx = new JobContext(
            new FileSystem(), TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
            new LegislatorOptions(), temp.Path, []);

        Assert.ThrowsAny<UnauthorizedAccessException>(() => new AnchorsJob().Run(ctx));
    }

    [Fact]
    public void The_job_is_named_anchors() => Assert.Equal("anchors", new AnchorsJob().Name);
}
