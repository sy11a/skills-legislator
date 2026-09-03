using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the baseline job answers (R-8206).
/// The baseline is the engine's ONE write (ADR-0003) and the generated class's first member:
/// deterministic over an unchanged repository, destroying any hand edit, and touching nothing
/// but its declared target.
/// </summary>
public sealed class BaselineTwins
{
    /// <summary>
    /// The traceability marker, assembled rather than written: `core/artifact-lifecycle.md`
    /// annotates a test by the literal `per R-NNN` it carries, and a fixture that spelled the
    /// marker whole would enter the baseline as an annotation for requirements it does not
    /// test - a false green in the register a human reads.
    /// </summary>
    private const string Per = "per ";

    private const string Target = "/r/docs/ai/baseline.md";

    /// <summary>The ruler's `make_case_repo`, as `SddLintTwins` builds it: R-001 covered by an annotated test, R-002 and R-003 not.</summary>
    private static MockFileSystem CaseTree()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/okf");
        fs.AddFile("/r/docs/cases/BL-001-widget-flow/spec.md", new MockFileData(
            "# BL-001 — widget flow\n\n"
            + "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
            + "### R-001 — widgets persist\n\nWHEN a widget is saved THEN it SHALL persist.\n\n"
            + "### R-002 — widgets list\n\nWHEN listed THEN widgets SHALL appear.\n\n"
            + "### R-003 — widgets delete\n\nWHEN deleted THEN widgets SHALL disappear.\n"));
        fs.AddFile("/r/docs/cases/BL-001-widget-flow/plan.md", new MockFileData(
            "# plan\n\n1. store layer, " + Per + "R-001\n2. list endpoint, " + Per + "R-002\n3. cleanup, " + Per + "R-999\n"));
        fs.AddFile("/r/docs/cases/BL-001-widget-flow/notes.md", new MockFileData("# notes\n\nLeft behind: {{PROJECT_OVERVIEW}}\n"));
        fs.AddFile("/r/src/App.Tests/WidgetStoreTests.cs", new MockFileData("// " + Per + "R-001\npublic class WidgetStoreTests { }\n"));
        return fs;
    }

    /// <summary>The ruler's `run(root, "baseline")`, in process: the same argv the binary is given.</summary>
    private static int Baseline(MockFileSystem fs)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        return Program.Run(
            ["baseline", "--root", "/r"], JobRegistry.Jobs, HookRegistry.Hooks, fs, TimeProvider.System,
            new FakeEnvironment(), new FakeProcessRunner(), TextReader.Null, stdout, stderr);
    }

    private static string Text(MockFileSystem fs) => fs.File.Exists(Target) ? fs.File.ReadAllText(Target) : "";

    [Fact]
    [Parity("engine", "baseline_writes_declared_target")]
    public void Baseline_writes_its_declared_target()
    {
        var fs = CaseTree();

        var exit = Baseline(fs);

        Assert.Equal(0, exit);
        Assert.True(fs.File.Exists(Target));
    }

    [Fact]
    [Parity("engine", "baseline_maps_requirement_to_test")]
    public void Baseline_maps_a_requirement_to_the_test_that_carries_its_marker()
    {
        var fs = CaseTree();
        Baseline(fs);

        var text = Text(fs);

        Assert.Contains("R-001", text, StringComparison.Ordinal);
        Assert.Contains("WidgetStoreTests.cs", text, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "baseline_lists_uncovered_requirements")]
    public void Baseline_lists_the_requirements_no_test_carries()
    {
        var fs = CaseTree();
        Baseline(fs);

        var text = Text(fs);

        Assert.Contains("R-002", text, StringComparison.Ordinal);
        Assert.Contains("R-003", text, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "baseline_declares_itself_generated")]
    public void Baseline_declares_itself_generated()
    {
        var fs = CaseTree();
        Baseline(fs);

        Assert.Contains("do not edit", Text(fs), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("engine", "baseline_bytes_deterministic")]
    public void Baseline_bytes_are_deterministic_over_an_unchanged_repository()
    {
        var fs = CaseTree();
        Baseline(fs);
        var first = Text(fs);

        Baseline(fs);

        Assert.NotEqual("", first);
        Assert.Equal(first, Text(fs));
    }

    [Fact]
    [Parity("engine", "baseline_destroys_hand_edits: regeneration is the class's defining property")]
    public void Regeneration_destroys_a_hand_edit()
    {
        var fs = CaseTree();
        Baseline(fs);
        var first = Text(fs);
        fs.File.WriteAllText(Target, "hand edit\n");

        Baseline(fs);

        Assert.NotEqual("", first);
        Assert.Equal(first, Text(fs));
    }

    [Fact]
    [Parity("engine", "baseline_writes_nothing_else")]
    public void Baseline_writes_exactly_its_declared_target()
    {
        var fs = CaseTree();
        var before = fs.AllFiles.ToHashSet(StringComparer.Ordinal);

        Baseline(fs);

        Assert.Equal(
            [Target],
            fs.AllFiles.Where(f => !before.Contains(f)).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Parity("engine", "baseline_definition_keeps_inline_code")]
    public void A_definition_reaches_the_baseline_with_its_inline_code()
    {
        var fs = SddLintTwins.DefinitionForms();
        Baseline(fs);

        var text = Text(fs);

        Assert.Contains("R-002", text, StringComparison.Ordinal);
        Assert.Contains("`docs/x.md`", text, StringComparison.Ordinal);
    }
}
