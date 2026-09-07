using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the anchors job answers (R-8206).
/// Each one materialises the ruler's own fixture and drives the CLI host over it, so what is
/// compared is what the ruler compares: the process's exit code and its stdout, not a helper's
/// return value. The three labels of that region the host owns rather than the job — an
/// unknown job exits 2, no job exits 2, usage error is 2 — stay with the CLI task.
/// </summary>
public sealed class AnchorsTwins
{
    private const string Source = "public class WidgetStore { }\n";

    /// <summary>The ruler's `make_repo`: documents under `docs/okf/`, sources at their own paths.</summary>
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

    /// <summary>The ruler's `run(root, "anchors")`, in process: the same argv the binary is given.</summary>
    private static (int Exit, string Out, string Err) Anchors(IFileSystem fs)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["anchors", "--root", "/r"], JobRegistry.Jobs, HookRegistry.Hooks, fs, TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    private static (int Exit, string Out, string Err) Anchors(
        Dictionary<string, string> docs, Dictionary<string, string> sources) => Anchors(Repo(docs, sources));

    [Fact]
    [Parity("engine", "a healthy document produces no findings")]
    public void A_healthy_document_produces_no_findings()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\nSee `src/App/WidgetStore.cs` and `WidgetStore`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "missing path reported with document, line and token")]
    public void Missing_path_reported_with_document_line_and_token()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\nSee `src/App/Gone.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, exit);
        Assert.Contains("widgets.md:3: path-anchor: src/App/Gone.cs → no such file", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "missing symbol reported")]
    public void Missing_symbol_reported()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\nHandled by `LegacyProcessor`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, exit);
        Assert.Contains("widgets.md:3: symbol-anchor: LegacyProcessor", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "templates, globs, ~ and / paths, commands, lowercase and 3-char tokens are not anchors")]
    public void The_closed_definition_excludes_non_anchors()
    {
        var (exit, output, _) = Anchors(
            new()
            {
                ["widgets.md"] = "# Widgets\n\nTemplate `schemas/<type>/<version>.json`, glob `src/**/*.cs`, "
                    + "home `~/.config/app/settings.yaml`, absolute `/etc/hosts`, command `dotnet build`, "
                    + "field `contenthash`, short `Api`.\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "a trailing .Member() is stripped before the file test")]
    public void A_trailing_member_is_stripped_before_the_file_test()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\n`src/App/WidgetStore.Flush()` writes.\n" },
            new() { ["src/App/WidgetStore.cs"] = "public class WidgetStore { void Flush() {} }\n" });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "log.md and glossary.md are never anchored")]
    public void Log_and_glossary_are_never_anchored()
    {
        var (exit, output, _) = Anchors(
            new()
            {
                ["log.md"] = "# Log\n\n2026-01: removed `RetiredJob` and `src/App/Retired.cs`.\n",
                ["glossary.md"] = "# Glossary\n\n| `RetiredJob` | gone |\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "front matter and fenced code are skipped")]
    public void Front_matter_and_fenced_code_are_skipped()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "---\ntitle: uses `src/App/Gone.cs` in its description\n---\n\n# Widgets\n\n```\n`src/App/AlsoGone.cs`\n```\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "a symbol that exists only in prose does not resolve")]
    public void A_symbol_that_exists_only_in_prose_does_not_resolve()
    {
        var (exit, output, _) = Anchors(
            new()
            {
                ["widgets.md"] = "# Widgets\n\nHandled by `OnlyInDocs`.\n",
                ["other.md"] = "# Other\n\nThe class `OnlyInDocs` is described here.\n",
            },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, exit);
        Assert.Contains("symbol-anchor: OnlyInDocs", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "a repo with no docs/okf/ is clean, not an error")]
    public void A_repo_with_no_okf_bundle_is_clean_not_an_error()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/ai");

        var (exit, output, _) = Anchors(fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    private const string Removed =
        "---\ntype: Concept\nstatus: removed\n---\n\n# Payments\n\nThis concept was removed. It named `src/App/Gone.cs`.\n";

    private const string Live =
        "---\ntype: Concept\nstatus: implemented\n---\n\n# Billing\n\nStill names `src/App/Gone.cs`.\n";

    [Fact]
    [Parity("engine", "removed_doc_not_anchored: a status: removed document produces no anchor finding")]
    public void Removed_doc_not_anchored()
    {
        var (exit, output, _) = Anchors(
            new() { ["payments.md"] = Removed, ["billing.md"] = Live },
            new() { ["src/App/WidgetStore.cs"] = Source });

        // The ruler asserts only that `payments.md` is absent from the output, which an
        // implementation that produces no output at all would satisfy (H-007). The twin
        // therefore asserts the whole of stdout: the removed document is silent BECAUSE the
        // live one spoke, not because nothing ran.
        Assert.Equal(1, exit);
        Assert.Equal("docs/okf/billing.md:8: path-anchor: src/App/Gone.cs → no such file\n", output);
        Assert.DoesNotContain("payments.md", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "removed_doc_not_anchored (control): a live sibling with the same dead path still reports")]
    public void Removed_doc_not_anchored_control()
    {
        var (_, output, _) = Anchors(
            new() { ["payments.md"] = Removed, ["billing.md"] = Live },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Contains("billing.md:8: path-anchor: src/App/Gone.cs", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "nested_build_output_ignored: a symbol living only in nested build output stays unresolved")]
    public void Nested_build_output_ignored()
    {
        var (_, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\nHandled by `PaymentProcessor`.\n" },
            new()
            {
                ["src/App/obj/Debug/App.js"] = "var PaymentProcessor = 1;\n",
                ["src/App/WidgetStore.cs"] = Source,
            });

        Assert.Contains("symbol-anchor: PaymentProcessor", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "real_source_still_resolves: a symbol outside build output resolves as before")]
    public void Real_source_still_resolves()
    {
        var (exit, output, _) = Anchors(
            new() { ["widgets.md"] = "# Widgets\n\nHandled by `PaymentProcessor`.\n" },
            new() { ["src/App/PaymentProcessor.cs"] = "public class PaymentProcessor { }\n" });

        Assert.Equal(0, exit);
        Assert.Equal("", output);
    }

    /// <summary>
    /// The ruler makes the document unreadable with `chmod 0o000`. Two things rule that
    /// mechanism out here: `MockFileSystem.File.ReadAllText` returns empty text for a
    /// directory where `System.IO` throws, so the fake cannot express an unreadable document
    /// at all; and `chmod` says nothing on Windows, which is why the ruler skips the case
    /// there (R-704). A `*.md` entry that is a directory, on a real disk, throws on every host
    /// and is yielded by the bundle walk exactly as the Python's `rglob` yields it. The claim
    /// asserted is the ruler's; the mechanism is not.
    /// </summary>
    private static (int Exit, string Out, string Err) AnchorsOverUnreadableDocument()
    {
        using var temp = new TempDirectory()
            .With("docs/okf/widgets.md", "# Widgets\n\nSee `src/App/WidgetStore.cs`.\n")
            .With("src/App/WidgetStore.cs", Source)
            .WithUnreadableDocument("docs/okf/locked.md");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["anchors", "--root", temp.Path], JobRegistry.Jobs, HookRegistry.Hooks, new FileSystem(), TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    [Parity("engine", "crash_exits_distinctly: an unhandled exception exits with a code distinct from clean/findings/usage")]
    public void Crash_exits_distinctly()
    {
        var (exit, _, stderr) = AnchorsOverUnreadableDocument();

        Assert.DoesNotContain(exit, (int[])[0, 1, 2]);
        Assert.Contains("engine failed:", stderr, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "crash_exits_distinctly (control): a crash prints nothing to stdout, so a stdout-only reader sees no findings")]
    public void Crash_prints_nothing_to_stdout()
    {
        var (exit, output, _) = AnchorsOverUnreadableDocument();

        // Silence on stdout only means something once a crash has actually happened - without
        // the exit code this twin would pass against a job that never ran (H-007).
        Assert.Equal(3, exit);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "exit_codes_unchanged: clean is 0")]
    public void Exit_codes_unchanged_clean_is_0()
    {
        var (exit, _, _) = Anchors(
            new() { ["widgets.md"] = "# W\n\nSee `src/App/WidgetStore.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(0, exit);
    }

    [Fact]
    [Parity("engine", "exit_codes_unchanged: findings is 1")]
    public void Exit_codes_unchanged_findings_is_1()
    {
        var (exit, _, _) = Anchors(
            new() { ["widgets.md"] = "# W\n\nSee `src/App/Gone.cs`.\n" },
            new() { ["src/App/WidgetStore.cs"] = Source });

        Assert.Equal(1, exit);
    }
}
