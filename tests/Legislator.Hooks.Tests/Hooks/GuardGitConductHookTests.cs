using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Hooks.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Hooks.Tests.Hooks;

/// <summary>
/// The git conduct guard (BL-064), branch by documented branch (C-11). It enforces the
/// pair-development law where the decision is taken - on the agent's command line - and every
/// undecidable case allows: no git, detached HEAD, an unknown default branch, a parser
/// surprise. Git is asked through the injected runner here, so what the tests pin is the
/// judgement, not a repository.
/// </summary>
public sealed class GuardGitConductHookTests
{
    private static readonly LegislatorOptions Options = new();

    /// <summary>A legislated repository: the guard is a silent no-op without a manifest up the tree.</summary>
    private static MockFileSystem Legislated()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/docs/ai/manifest.json", new MockFileData("{}"));
        return fs;
    }

    /// <summary>
    /// Git at its interface: the three questions the guard asks, answered from what the test
    /// declares. <paramref name="current"/> null is a detached HEAD (symbolic-ref refuses),
    /// <paramref name="originHead"/> null makes the default fall back to the branch list.
    /// </summary>
    private static FakeProcessRunner Git(
        string? current = "master", string? originHead = "origin/master", params string[] branches) => new()
    {
        OnRun = (_, args, _) =>
        {
            var refused = new ProcessResult(1, "", "");
            if (args.Contains("HEAD") && !args.Contains("refs/remotes/origin/HEAD"))
            {
                return current is null ? refused : new ProcessResult(0, current + "\n", "");
            }

            if (args.Contains("refs/remotes/origin/HEAD"))
            {
                return originHead is null ? refused : new ProcessResult(0, originHead + "\n", "");
            }

            return args.Contains("refs/heads")
                ? new ProcessResult(0, string.Join('\n', branches) + "\n", "")
                : refused;
        },
    };

    private static HookResult Judge(string command, IProcessRunner? git = null, MockFileSystem? fs = null)
    {
        var raw = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["hook_event_name"] = "PreToolUse",
            ["tool_name"] = "Bash",
            ["tool_input"] = new Dictionary<string, string> { ["command"] = command },
            ["cwd"] = "/r",
        });

        return new GuardGitConductHook().Run(
            new HookContext(
                HookPayload.Parse(raw), fs ?? Legislated(), new FakeEnvironment(), git ?? Git(), Options));
    }

    // --- merging into the default branch (R-641) ----------------------------

    [Fact]
    public void Merging_while_on_the_default_branch_is_blocked()
    {
        var result = Judge("git merge bl/064-x");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("pair-development", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Merging_into_a_feature_branch_is_allowed()
    {
        Assert.Equal(0, Judge("git merge master", Git(current: "bl/064-x")).ExitCode);
    }

    /// <summary>A compound command hides no merge: the guard judges every segment, not the head of the line.</summary>
    [Fact]
    public void A_merge_inside_a_compound_command_is_blocked()
    {
        Assert.Equal(2, Judge("git fetch && git merge origin/master").ExitCode);
    }

    [Fact]
    public void Aborting_a_merge_is_cleanup_not_merging()
    {
        Assert.Equal(0, Judge("git merge --abort").ExitCode);
        Assert.Equal(0, Judge("git merge --quit").ExitCode);
    }

    /// <summary>A detached HEAD cannot be compared with the default branch, so the guard cannot tell - and allows.</summary>
    [Fact]
    public void A_detached_head_merge_is_allowed()
    {
        Assert.Equal(0, Judge("git merge bl/064-x", Git(current: null)).ExitCode);
    }

    /// <summary>No origin/HEAD and both conventional names present: the default is ambiguous, so the guard allows rather than guesses.</summary>
    [Fact]
    public void An_ambiguous_default_branch_is_allowed()
    {
        Assert.Equal(0, Judge("git merge topic", Git(originHead: null, branches: ["main", "master"])).ExitCode);
    }

    /// <summary>No origin/HEAD and exactly one conventional name: the default is decidable from the branch list.</summary>
    [Fact]
    public void The_default_branch_falls_back_to_the_single_conventional_name()
    {
        Assert.Equal(2, Judge("git merge topic", Git(originHead: null, branches: ["master", "topic"])).ExitCode);
    }

    // --- pushing the default branch (R-642) ---------------------------------

    [Fact]
    public void Pushing_the_default_branch_by_name_is_blocked()
    {
        Assert.Equal(2, Judge("git push origin master").ExitCode);
    }

    [Fact]
    public void Pushing_a_refspec_whose_destination_is_the_default_branch_is_blocked()
    {
        Assert.Equal(2, Judge("git push origin HEAD:master").ExitCode);
        Assert.Equal(2, Judge("git push origin +HEAD:refs/heads/master").ExitCode);
    }

    [Fact]
    public void Pushing_a_task_branch_is_allowed()
    {
        Assert.Equal(0, Judge("git push -u origin bl/064-x", Git(current: "bl/064-x")).ExitCode);
    }

    /// <summary>A bare push takes its destination from the branch you are on: on the default branch it is the default branch.</summary>
    [Fact]
    public void A_bare_push_while_on_the_default_branch_is_blocked()
    {
        Assert.Equal(2, Judge("git push").ExitCode);
    }

    [Fact]
    public void A_bare_push_from_a_task_branch_is_allowed()
    {
        Assert.Equal(0, Judge("git push", Git(current: "bl/064-x")).ExitCode);
    }

    /// <summary>A push of everything pushes the default branch too, whichever branch it is run from.</summary>
    [Fact]
    public void Pushing_all_branches_is_blocked()
    {
        Assert.Equal(2, Judge("git push --all", Git(current: "bl/064-x")).ExitCode);
        Assert.Equal(2, Judge("git push --mirror", Git(current: "bl/064-x")).ExitCode);
    }

    /// <summary>An option that consumes the next token must not have that token read as a refspec.</summary>
    [Fact]
    public void An_option_argument_is_not_mistaken_for_a_refspec()
    {
        Assert.Equal(0, Judge("git push --repo master origin bl/064-x", Git(current: "bl/064-x")).ExitCode);
    }

    // --- AI attribution in the VCS record (R-643, R-644) --------------------

    [Fact]
    public void A_claude_co_author_trailer_is_blocked()
    {
        var result = Judge("""git commit -m "fix: x\n\nCo-Authored-By: Claude <noreply@anthropic.com>" """);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("attribution", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_generated_with_footer_is_blocked()
    {
        Assert.Equal(2, Judge("""git commit -m "docs: y" -m "Generated with [Claude Code](https://claude.com/claude-code)" """).ExitCode);
    }

    [Fact]
    public void A_human_co_author_trailer_is_allowed()
    {
        Assert.Equal(0, Judge("""git commit -m "fix: x\n\nCo-Authored-By: Jane Doe <jane@example.com>" """).ExitCode);
    }

    [Fact]
    public void An_ordinary_commit_message_is_allowed()
    {
        Assert.Equal(0, Judge("""git commit -m "fix: ordinary message" """).ExitCode);
    }

    /// <summary>Attribution outside a commit or a PR command is not the guard's business - reading the pattern is not writing it.</summary>
    [Fact]
    public void Grepping_for_the_pattern_is_allowed()
    {
        Assert.Equal(0, Judge("""grep "Co-Authored-By: Claude" README.md""").ExitCode);
    }

    [Fact]
    public void Attribution_in_a_pr_body_is_blocked()
    {
        Assert.Equal(2, Judge("""gh pr create --title "x" --body "Generated with [Claude Code](https://claude.com/claude-code)" """).ExitCode);
        Assert.Equal(2, Judge("""gh pr edit 3 --body "Co-Authored-By: Claude <noreply@anthropic.com>" """).ExitCode);
    }

    [Fact]
    public void A_clean_pr_body_is_allowed()
    {
        Assert.Equal(0, Judge("""gh pr create --title "x" --body "plain delivery notes" """).ExitCode);
    }

    // --- merging the PR is the user's act (R-645) ---------------------------

    [Fact]
    public void Merging_the_pr_is_blocked_whatever_the_branch()
    {
        Assert.Equal(2, Judge("gh pr merge 23 --squash", Git(current: "bl/064-x")).ExitCode);
    }

    // --- undecidable and out of scope (R-646, R-647) ------------------------

    [Fact]
    public void A_non_git_command_is_allowed()
    {
        Assert.Equal(0, Judge("ls -la && echo done").ExitCode);
    }

    [Fact]
    public void Malformed_stdin_allows()
    {
        var result = new GuardGitConductHook().Run(
            new HookContext(HookPayload.Parse("not json"), Legislated(), new FakeEnvironment(), Git(), Options));

        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>The guard is the Bash matcher's: a payload from another tool is not its business.</summary>
    [Fact]
    public void A_payload_from_another_tool_allows()
    {
        var raw = """{"tool_name": "Edit", "tool_input": {"command": "git merge topic"}, "cwd": "/r"}""";
        var result = new GuardGitConductHook().Run(
            new HookContext(HookPayload.Parse(raw), Legislated(), new FakeEnvironment(), Git(), Options));

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Outside_a_legislated_repo_the_guard_is_a_no_op()
    {
        Assert.Equal(0, Judge("git merge topic", fs: new MockFileSystem()).ExitCode);
    }

    /// <summary>Git absent is not git refusing: the guard cannot tell what branch you are on, so it allows.</summary>
    [Fact]
    public void Git_being_absent_allows()
    {
        var absent = new FakeProcessRunner { OnRun = (_, _, _) => throw new ProcessStartException("git", new IOException("no git")) };

        Assert.Equal(0, Judge("git merge topic", absent).ExitCode);
    }

    /// <summary>An unbalanced quote makes the command line unparseable; an unparseable line is judged by nobody.</summary>
    [Fact]
    public void An_unparseable_command_line_allows()
    {
        Assert.Equal(0, Judge("git merge 'unbalanced").ExitCode);
    }

    // --- the command-head parser (R-701) ------------------------------------

    [Theory]
    [InlineData("git.exe merge topic")]
    [InlineData("GIT.EXE merge topic")]
    [InlineData("/usr/bin/git merge topic")]
    [InlineData("\"C:\\Program Files\\Git\\bin\\git.exe\" merge topic")]
    public void Every_windows_shaped_git_head_is_git(string command)
    {
        Assert.Equal(2, Judge(command).ExitCode);
    }

    /// <summary>The control: a binary whose name merely contains "git" is not git.</summary>
    [Fact]
    public void A_github_named_binary_is_not_git()
    {
        Assert.Equal(0, Judge("github merge topic").ExitCode);
    }

    /// <summary>An environment prefix is not the command - the guard steps over it to find the head.</summary>
    [Fact]
    public void An_environment_prefix_does_not_hide_the_command()
    {
        Assert.Equal(2, Judge("GIT_EDITOR=true git merge topic").ExitCode);
    }

    /// <summary>`-C` moves the repository the command acts on, so it moves the repository the guard asks about.</summary>
    [Fact]
    public void A_dash_c_path_redirects_the_repository_asked_about()
    {
        var asked = new List<string>();
        var git = new FakeProcessRunner
        {
            OnRun = (_, args, cwd) =>
            {
                asked.Add(cwd);
                return args.Contains("refs/remotes/origin/HEAD")
                    ? new ProcessResult(0, "origin/master\n", "")
                    : new ProcessResult(0, "master\n", "");
            },
        };

        Judge("git -C /other merge topic", git);

        Assert.Contains("/other", asked, StringComparer.Ordinal);
    }
}
