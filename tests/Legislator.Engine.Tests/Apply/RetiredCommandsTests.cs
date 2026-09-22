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

    [Theory]
    [InlineData("docs/cases/BL-001-a/summary.md")]
    [InlineData("docs/journal/2026-09-01.md")]
    [InlineData("docs/adr/0001-a.md")]
    [InlineData("CHANGELOG.md")]
    public void Given_a_record_naming_the_retired_command_When_swept_Then_it_is_counted_not_listed(string home)
    {
        // A record of what was true when it was written. Rewriting it would falsify the record,
        // and its going out of date is the design (artifact-lifecycle). Listing them buries the
        // mentions a reader must act on, so they are counted.
        var plan = Plan((home, $"The gate we ran was `python3 {Retired} anchors`.\n"));

        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Mentions);
        Assert.Equal(1, plan.Records);
    }

    [Fact]
    public void Given_a_knowledge_document_naming_the_retired_command_When_swept_Then_it_is_named_for_the_reader()
    {
        // Not a record: `core/okf.md` says a concept document must be updated when the concept
        // changes, so this is the one line in the block its reader has an act in.
        var plan = Plan(("docs/okf/stand.md", $"| `anchors` | `python3 {Retired} anchors` | the rung |\n"));

        Assert.Equal("docs/okf/stand.md:1", Assert.Single(plan.Mentions));
        Assert.Equal(0, plan.Records);
    }

    [Fact]
    public void Given_a_frozen_fixture_under_tools_When_swept_Then_it_is_not_a_declaration()
    {
        // clerk keeps a 2026-08-31 backlog snapshot under `tools/tests/fixtures/`, pinned by its
        // own parser test. Taking `tools/**.md` as declaration made the upgrade refuse and offer
        // a remedy that would have rewritten a record and changed a test's subject.
        var plan = Plan(("tools/tests/fixtures/backlog-2026-08-31.md", $"the engine, `{Retired}`, was delivered then\n"));

        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Mentions);
        Assert.Equal(1, plan.Records);
    }

    [Fact]
    public void Given_a_path_that_merely_contains_the_retired_one_When_swept_Then_nothing_is_named()
    {
        var plan = Plan(("tools/gate.sh", $"cp tools/docs/ai/engine.py.bak /tmp/x\n"));

        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Mentions);
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
    // The four repositories this exists for all write both invocations on one line. A rewrite
    // that took the first and then saw the path still there refused every one of them.
    [InlineData(
        "- Gates: `python3 docs/ai/engine.py anchors`, `python3 docs/ai/engine.py sdd-lint`, and `python3 -m unittest`",
        "- Gates: `legislator anchors`, `legislator sdd-lint`, and `python3 -m unittest`")]
    [InlineData("python3 docs/ai/engine.py anchors && python3 docs/ai/engine.py sdd-lint",
                "legislator anchors && legislator sdd-lint")]
    public void Given_a_recognised_invocation_When_rewritten_Then_the_rest_of_the_line_survives(string before, string after) =>
        Assert.Equal(after, RetiredCommands.Rewrite(before, Retired));

    [Theory]
    [InlineData("see docs/ai/engine.py for the jobs")]
    [InlineData("python3 docs/ai/engine.py")]
    [InlineData("cp docs/ai/engine.py /tmp/x")]
    public void Given_a_shape_the_rewrite_does_not_know_When_rewritten_Then_it_returns_null(string line) =>
        Assert.Null(RetiredCommands.Rewrite(line, Retired));

    [Fact]
    public void Given_a_file_with_CRLF_endings_When_applied_Then_only_the_edited_line_changes()
    {
        // `ReadAllLines` + join on '\n' normalises the whole file, which is the whole-file diff
        // the rewrite exists not to make.
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["tools/gate.sh"] = $"#!/bin/sh\r\nrun anchors python3 {Retired} anchors\r\necho done\r\n",
        });
        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        RetiredCommands.Apply(fs, ApplyFixture.Layout, plan);

        Assert.Equal(
            "#!/bin/sh\r\nrun anchors legislator anchors\r\necho done\r\n",
            fs.File.ReadAllText($"{ApplyFixture.Root}/tools/gate.sh"));
    }

    [Theory]
    [InlineData("tools/docs/ai/engine.py.bak", false)]
    [InlineData("x/docs/ai/engine.py", false)]
    [InlineData("`docs/ai/engine.py`", true)]
    [InlineData("run docs/ai/engine.py anchors", true)]
    public void Given_a_line_When_asked_whether_it_names_the_retired_path_Then_a_longer_path_is_not_it(string line, bool named) =>
        Assert.Equal(named, RetiredCommands.Names(line, Retired));
}
