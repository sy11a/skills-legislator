using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Repo;
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
        var plan = Plan(("AGENTS.md", $"Run `python3 {Retired} anchors`, then `cp {Retired} /tmp/x`.\n"));

        Assert.Empty(plan.Rewrites);
        var refusal = Assert.Single(plan.Refusals);
        Assert.Contains("AGENTS.md:1", refusal, StringComparison.Ordinal);
        Assert.Contains("cannot rewrite the line", refusal, StringComparison.Ordinal);
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
    [InlineData("python3 docs/ai/engine.py --help")]
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
    [InlineData("./docs/ai/engine.py", true)]
    [InlineData("\"$ROOT/docs/ai/engine.py\"", true)]
    [InlineData("/srv/checkout/docs/ai/engine.py", true)]
    [InlineData("`docs/ai/engine.py`", true)]
    [InlineData("run docs/ai/engine.py anchors", true)]
    public void Given_a_line_When_asked_whether_it_names_the_retired_path_Then_a_longer_path_is_not_it(string line, bool named) =>
        Assert.Equal(named, RetiredCommands.Names(line, Retired));

    [Fact]
    public void Given_an_entry_document_importing_a_retired_rule_When_swept_Then_it_is_a_mention_not_a_refusal()
    {
        // **The regression this file exists to prevent.** Every entry document names every rule
        // it imports, by path. Refusing on a mention refused every upgrade that retires a rule
        // or drops a stack — two of the five scenarios in this edition's own e2e corpus. The
        // engine deletes; the skill's report proposes the import's removal.
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = "@docs/ai/rules/core/old.md\n\n# Entry\n",
        });

        var plan = RetiredCommands.Of(
            fs, ApplyFixture.Options, ApplyFixture.Layout, ["docs/ai/rules/core/old.md"]);

        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Rewrites);
        Assert.Equal("AGENTS.md:1", Assert.Single(plan.Mentions));
    }

    [Fact]
    public void Given_a_stack_import_in_the_entry_document_When_swept_Then_the_drop_is_not_refused()
    {
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = "@docs/ai/rules/stacks/aurelia/x.md\n",
        });

        var plan = RetiredCommands.Of(
            fs, ApplyFixture.Options, ApplyFixture.Layout, ["docs/ai/rules/stacks/aurelia/x.md"]);

        Assert.Empty(plan.Refusals);
    }

    [Theory]
    [InlineData("./docs/ai/engine.py")]
    [InlineData("\"$ROOT/docs/ai/engine.py\"")]
    [InlineData("/srv/checkout/docs/ai/engine.py")]
    public void Given_the_retired_path_written_under_a_root_When_swept_Then_the_command_still_moves(string written)
    {
        var plan = Plan(("tools/gate.sh", $"run anchors python3 {written} anchors\n"));

        var edit = Assert.Single(plan.Rewrites);
        Assert.Equal("run anchors legislator anchors", edit.After);
    }

    [Fact]
    public void Given_a_comment_in_a_script_When_swept_Then_it_is_not_refused()
    {
        // A fixture script that *describes* the old gate is not a gate. A comment is the one
        // shape in a declaration home that is not refused — stopping over one would offer a
        // remedy that rewrites a record.
        var plan = Plan(("tools/tests/fixtures/old-gate.sh", $"# the old gate was python3 {Retired} anchors\n"));

        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Rewrites);
        Assert.Equal("tools/tests/fixtures/old-gate.sh:1", Assert.Single(plan.Mentions));
    }

    [Theory]
    [InlineData("tools/tests/run.sh")]
    [InlineData("tools/fixtures/run.sh")]
    public void Given_a_live_script_under_a_tests_path_When_swept_Then_it_is_still_a_command(string where)
    {
        // Calling every line under `tests/` history left two scripts running a file the same run
        // had deleted. A script is a live command wherever it lives; only a *document* there is
        // frozen input.
        var plan = Plan((where, $"python3 {Retired} anchors\n"));

        Assert.Equal("legislator anchors", Assert.Single(plan.Rewrites).After);
    }

    [Theory]
    // Shapes the rewrite's one regex cannot read. Each is a live command; each must stop the run.
    [InlineData("python3 \"$ROOT\"/docs/ai/engine.py anchors")]
    [InlineData("python3 -u docs/ai/engine.py anchors")]
    [InlineData("exec python3 docs/ai/engine.py \"$@\"")]
    [InlineData("Run `python3 docs/ai/engine.py --help` to list the jobs.")]
    public void Given_an_invocation_the_rewrite_cannot_read_When_swept_Then_the_run_refuses(string line)
    {
        // The version before this one refused only where its own regex had matched — so a line
        // it could read was guarded and a line it could not was waved through, the file deleted
        // under a live command at exit 0. Refusal is the default now.
        var plan = Plan(("tools/gate.sh", line + "\n"));

        Assert.Empty(plan.Rewrites);
        Assert.Single(plan.Refusals);
    }

    [Fact]
    public void Given_a_retired_rule_file_When_it_is_named_anywhere_Then_it_is_never_a_refusal()
    {
        // Law is imported, not run: retiring a rule is the ordinary business of a constitution.
        var plan0 = Plan(("AGENTS.md", "@docs/ai/rules/core/old.md\n"));
        var fs = ApplyFixture.Repo(new Dictionary<string, string> { ["AGENTS.md"] = "@docs/ai/rules/core/old.md\n" });

        var plan = RetiredCommands.Of(
            fs, ApplyFixture.Options, ApplyFixture.Layout, ["docs/ai/rules/core/old.md"]);

        Assert.Empty(plan0.Refusals);
        Assert.Empty(plan.Refusals);
        Assert.Equal("AGENTS.md:1", Assert.Single(plan.Mentions));
    }

    [Theory]
    [InlineData("python3 docs/ai/engine.py --help")]
    [InlineData("python3 docs/ai/engine.py nosuchjob")]
    public void Given_a_job_the_binary_does_not_have_When_rewritten_Then_it_is_not_invented(string line) =>
        Assert.Null(RetiredCommands.Rewrite(line, Retired));

    [Theory]
    [InlineData("docs/okf/log.md")]
    [InlineData("docs/okf/glossary.md")]
    [InlineData("docs/backlog.md")]
    [InlineData("docs/superpowers/specs/old.md")]
    [InlineData("docs/changes/BL-001.md")]
    public void Given_a_home_this_repository_calls_history_When_swept_Then_it_is_counted(string home)
    {
        // `core/okf.md`: a glossary defines terms and a log records what was true at the time,
        // so naming something since removed is correct there. A generated mirror is never
        // hand-edited; `docs/superpowers/**` is legacy history by this repository's own law.
        var plan = Plan((home, $"the gate was `python3 {Retired} anchors`\n"));

        Assert.Empty(plan.Mentions);
        Assert.Equal(1, plan.Records);
    }

    [Fact]
    public void Given_a_repository_checked_out_under_a_tests_directory_When_swept_Then_the_cut_is_unchanged()
    {
        // The record test was keyed on the absolute path, so a repository at `/x/tests/r` had
        // every mention counted and none named.
        var options = ApplyFixture.Options;
        var fs = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        fs.AddDirectory("/x/tests/r");
        fs.AddFile("/x/tests/r/docs/okf/stand.md", new System.IO.Abstractions.TestingHelpers.MockFileData(
            $"| `anchors` | `python3 {Retired} anchors` |\n"));

        var plan = RetiredCommands.Of(fs, options, new RepoLayout(options, "/x/tests/r"), [Retired]);

        Assert.Equal("docs/okf/stand.md:1", Assert.Single(plan.Mentions));
        Assert.Equal(0, plan.Records);
    }

    [Theory]
    [InlineData("#!/bin/sh\rrun anchors python3 docs/ai/engine.py anchors\recho done\r",
                "#!/bin/sh\rrun anchors legislator anchors\recho done\r")]
    [InlineData("run anchors python3 docs/ai/engine.py anchors\r\r\necho done\n",
                "run anchors legislator anchors\r\r\necho done\n")]
    [InlineData("run anchors python3 docs/ai/engine.py anchors", "run anchors legislator anchors")]
    public void Given_a_file_with_unusual_endings_When_applied_Then_every_line_keeps_its_own(string before, string after)
    {
        // Walking only '\n' truncated a file whose lines end in a lone '\r' — the rewritten line
        // among the ones it dropped — and lost the trailing newline of a `\r\r\n` file.
        var fs = ApplyFixture.Repo(new Dictionary<string, string> { ["tools/gate.sh"] = before });
        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        RetiredCommands.Apply(fs, ApplyFixture.Layout, plan);

        Assert.Equal(after, fs.File.ReadAllText($"{ApplyFixture.Root}/tools/gate.sh"));
    }

    [Fact]
    public void Given_a_real_alias_beside_the_entry_document_When_swept_Then_only_the_real_files_are_declarations()
    {
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["CLAUDE.md"] = $"- `python3 {Retired} anchors`\n",
        });

        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        var edit = Assert.Single(plan.Rewrites);
        Assert.Equal("CLAUDE.md", edit.Relative);
    }

    [Fact]
    public void Given_a_non_markdown_file_in_the_project_rules_When_swept_Then_it_is_not_a_declaration()
    {
        var plan = Plan((".claude/rules/data.json", $"{{\"gate\": \"python3 {Retired} anchors\"}}\n"));

        Assert.Empty(plan.Rewrites);
        Assert.Empty(plan.Refusals);
    }

    [Fact]
    public void Given_a_line_carrying_an_invocation_and_a_lookalike_path_When_swept_Then_it_is_rewritten_not_refused()
    {
        // The post-rewrite test must ask whether the path is still *named*, not whether the
        // string is still present: `engine.py.bak` is a different file and must not block.
        var plan = Plan(("tools/gate.sh", $"python3 {Retired} anchors  # see {Retired}.bak\n"));

        Assert.Empty(plan.Refusals);
        Assert.Equal("legislator anchors  # see docs/ai/engine.py.bak", Assert.Single(plan.Rewrites).After);
    }

    [Theory]
    // `#` is a comment in a script and a heading in Markdown. Applying shell grammar to an entry
    // document exempted a heading and an issue line, and the tool was deleted under them.
    [InlineData("AGENTS.md", "# Gates: `python3 docs/ai/engine.py anchors`")]
    [InlineData("AGENTS.md", "#142 replaced `python3 docs/ai/engine.py anchors` with the binary")]
    [InlineData(".claude/rules/verification.md", "# `python3 docs/ai/engine.py anchors`")]
    public void Given_a_hash_line_in_a_markdown_declaration_When_swept_Then_it_is_not_exempt(string home, string line)
    {
        var plan = Plan((home, line + "\n"));

        Assert.True(plan.Rewrites.Count + plan.Refusals.Count == 1, "a markdown '#' line is not a comment");
        Assert.Empty(plan.Mentions);
    }

    [Theory]
    [InlineData("    # python3 docs/ai/engine.py anchors was the gate")]
    [InlineData("#!/usr/bin/env -S python3 docs/ai/engine.py")]
    public void Given_a_script_line_When_asked_whether_it_is_a_comment_Then_indentation_and_a_shebang_are_read(string line)
    {
        // An indented comment is a comment; a shebang is not, and dropping either bound let a
        // comment be falsified or a live interpreter line be waved through.
        var plan = Plan(("tools/gate.sh", line + "\n"));

        if (line.StartsWith("#!", StringComparison.Ordinal))
        {
            Assert.Single(plan.Refusals);
        }
        else
        {
            Assert.Empty(plan.Rewrites);
            Assert.Empty(plan.Refusals);
        }
    }

    [Fact]
    public void Given_a_line_naming_both_a_retired_rule_and_a_retired_tool_When_swept_Then_the_tool_decides()
    {
        // Picking the first path a line names made the split depend on manifest sort order: a
        // tool sorting after the rules directory was deleted under a live command at exit 0.
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = "`docs/ai/rules/core/old.md` binds `python3 docs/ai/zz-tool.py check`.\n",
        });

        var plan = RetiredCommands.Of(
            fs, ApplyFixture.Options, ApplyFixture.Layout,
            ["docs/ai/rules/core/old.md", "docs/ai/zz-tool.py"]);

        Assert.Single(plan.Refusals);
        Assert.Empty(plan.Mentions);
    }

    [Fact]
    public void Given_a_real_alias_that_is_a_symlink_When_swept_Then_it_is_not_a_second_declaration()
    {
        // All four repositories have a real `CLAUDE.md → AGENTS.md`; taking the link as a home
        // would report and rewrite the same line twice.
        //
        // **This control does not pin the `LinkTarget` bound, and says so rather than pretending.**
        // The fake file system does not resolve a link on read, so with the bound removed the
        // alias is read as empty and nothing changes here — a mutation of that line survives this
        // test and every other in the suite. What pins it is the dry run over the four
        // repositories, each of which has a real symlinked alias and each of which reports its
        // one rewrite against `AGENTS.md` alone; that evidence is in the case record, not in this
        // file, and the gap is named because an unpinned bound a reader believes is pinned is
        // worse than one they know to check by hand.
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["AGENTS.md"] = $"- `python3 {Retired} anchors`\n",
        });
        fs.File.CreateSymbolicLink($"{ApplyFixture.Root}/CLAUDE.md", "AGENTS.md");

        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        Assert.Equal("AGENTS.md", Assert.Single(plan.Rewrites).Relative);
    }

    [Theory]
    [InlineData("tools/tests/snapshot.md")]
    [InlineData("tools/fixtures/snapshot.md")]
    public void Given_a_document_under_one_frozen_path_When_swept_Then_that_clause_alone_makes_it_a_record(string home)
    {
        // The two clauses had one control between them, on a path containing both segments.
        var plan = Plan((home, $"the gate was `python3 {Retired} anchors`\n"));

        Assert.Empty(plan.Mentions);
        Assert.Equal(1, plan.Records);
    }

    [Fact]
    public void Given_a_CRLF_file_When_a_line_is_rewritten_Then_the_reported_number_is_the_line_it_is()
    {
        // Splitting `\r\n` as two endings kept the bytes right and made every reported line
        // number on a CRLF file off by the lines above it.
        var fs = ApplyFixture.Repo(new Dictionary<string, string>
        {
            ["tools/gate.sh"] = $"#!/bin/sh\r\nrun anchors python3 {Retired} anchors\r\n",
        });
        var plan = RetiredCommands.Of(fs, ApplyFixture.Options, ApplyFixture.Layout, [Retired]);

        Assert.Equal(2, Assert.Single(plan.Rewrites).Line);
    }

    [Fact]
    public void Given_a_different_file_whose_name_extends_the_retired_one_with_a_hyphen_When_swept_Then_it_is_not_named()
    {
        // Under a default of refusal, dropping `-` from the name bound turns this into a false
        // refusal that blocks the upgrade.
        var plan = Plan(("tools/gate.sh", $"cp {Retired}-old /tmp/x\n"));

        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.Rewrites);
    }
}
