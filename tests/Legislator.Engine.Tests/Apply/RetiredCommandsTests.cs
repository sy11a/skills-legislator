using System.IO.Abstractions.TestingHelpers;
using Legislator.Engine.Apply;
using Xunit;

namespace Legislator.Engine.Tests.Apply;

/// <summary>
/// The sweep that makes a retirement and a declaration one act (BL-397, `sy11a/skills-legislator#60`).
/// Each case separates one branch: the parity twin drives a whole upgrade and would report the
/// same manifest whether a declaration moved or not, which is how four governed repositories came
/// to declare a gate naming a file the next edition deletes.
/// </summary>
public sealed class RetiredCommandsTests
{
    private const string Retired = "docs/ai/engine.py";

    private static RetiredCommands.Plan Plan(params (string Path, string Text)[] files)
    {
        var fs = ApplyFixture.Repo(files.ToDictionary(f => f.Path, f => f.Text));
        return RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);
    }

    [Fact]
    public void Given_an_entry_document_declaring_the_retired_form_When_swept_Then_it_is_rewritten_to_the_binary()
    {
        var plan = Plan(("AGENTS.md", $"## Build & Test\n\n- `python3 {Retired} anchors`\n"));

        var edit = Assert.Single(plan.Rewrites);
        Assert.Equal("AGENTS.md", edit.Relative);
        Assert.Equal(3, edit.Line);
        Assert.Equal("- `legislator anchors`", edit.After);
        Assert.Empty(plan.Refusals);
    }

    [Fact]
    public void Given_a_gate_script_the_repository_owns_When_swept_Then_its_row_is_rewritten_too()
    {
        // foundry's shape: the stale form is not prose, it is the script the gate runs.
        var plan = Plan(("tools/gate.sh", $"run anchors python3 {Retired} anchors\n"));

        var edit = Assert.Single(plan.Rewrites);
        Assert.Equal("tools/gate.sh", edit.Relative);
        Assert.Equal("run anchors legislator anchors", edit.After);
    }

    [Fact]
    public void Given_a_project_rule_declaring_the_retired_form_When_swept_Then_it_is_rewritten()
    {
        var plan = Plan((".claude/rules/verification.md", $"| anchors | `python3 {Retired} anchors` | every path resolves |\n"));

        var edit = Assert.Single(plan.Rewrites);
        Assert.Equal("| anchors | `legislator anchors` | every path resolves |", edit.After);
    }

    [Fact]
    public void Given_a_declaration_naming_the_retired_path_in_an_unknown_shape_When_swept_Then_it_refuses()
    {
        var plan = Plan(("AGENTS.md", $"Verification: run {Retired} yourself.\n"));

        Assert.Empty(plan.Rewrites);
        var refusal = Assert.Single(plan.Refusals);
        Assert.Contains("AGENTS.md:1", refusal, StringComparison.Ordinal);
        Assert.Contains("does not know how to rewrite", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_case_summary_naming_the_retired_command_When_swept_Then_it_is_left_as_history()
    {
        // A record of what was true when it was written. Rewriting it would falsify the record,
        // and its going out of date is the design (artifact-lifecycle).
        var plan = Plan(("docs/cases/BL-001-a/summary.md", $"The gate we ran was `python3 {Retired} anchors`.\n"));

        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Refusals);
        Assert.Equal("docs/cases/BL-001-a/summary.md:1", Assert.Single(plan.Mentions));
    }

    [Fact]
    public void Given_an_owned_rule_file_naming_the_retired_path_When_swept_Then_it_is_neither_rewritten_nor_refused()
    {
        // Owned files are rewritten byte-for-byte by this same run and self-heal; that is the
        // distinction between kbl and clerk in the census this case was filed on.
        var plan = Plan(("docs/ai/rules/core/verification.md", $"`python3 {Retired} anchors` is the executing arm.\n"));

        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Mentions);
    }

    [Fact]
    public void Given_nothing_is_being_retired_When_swept_Then_the_plan_is_empty()
    {
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = $"- `python3 {Retired} anchors`\n",
        });

        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, []);

        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Mentions);
    }

    [Fact]
    public void Given_a_plan_When_applied_Then_only_the_named_lines_move_and_the_file_keeps_its_ending()
    {
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = $"# Entry\n\n- `python3 {Retired} anchors`\n- `python3 {Retired} sdd-lint`\n- untouched\n",
        });
        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        var done = RetiredCommands.Apply(fs, ApplyFixture.Layout, plan);

        Assert.Equal(["AGENTS.md:3", "AGENTS.md:4"], done);
        Assert.Equal(
            "# Entry\n\n- `legislator anchors`\n- `legislator sdd-lint`\n- untouched\n",
            fs.File.ReadAllText($"{ApplyFixture.Root}/AGENTS.md"));
    }

    [Theory]
    [InlineData("- `python3 docs/ai/engine.py anchors`", "- `legislator anchors`")]
    [InlineData("run sdd-lint python docs/ai/engine.py sdd-lint", "run sdd-lint legislator sdd-lint")]
    [InlineData("`python3 docs/ai/engine.py okf-debt` names stale documents", "`legislator okf-debt` names stale documents")]
    public void Given_a_recognised_invocation_When_rewritten_Then_the_rest_of_the_line_survives(string before, string after) =>
        Assert.Equal(after, RetiredCommands.Rewrite(before, Retired));

    [Theory]
    [InlineData("see docs/ai/engine.py for the jobs")]
    [InlineData("python3 docs/ai/engine.py")]
    [InlineData("cp docs/ai/engine.py /tmp/x")]
    [InlineData("python3 docs/ai/engine.py anchors && python3 docs/ai/engine.py sdd-lint")]
    public void Given_a_shape_the_rewrite_does_not_know_When_rewritten_Then_it_returns_null(string line) =>
        Assert.Null(RetiredCommands.Rewrite(line, Retired));
}
