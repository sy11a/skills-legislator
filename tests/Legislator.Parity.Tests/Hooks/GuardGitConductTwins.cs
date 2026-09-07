using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>
/// The named twins of every `check_hooks.py` assertion the git-conduct guard answers (R-8206).
/// The ruler builds a real repository per case; here git answers at its interface, so the
/// branch the guard sees is the branch the case declares and nothing depends on a checkout.
/// </summary>
public sealed class GuardGitConductTwins
{
    private const string Hook = "guard_git_conduct";

    /// <summary>The ruler's `conduct(command, repo)` over its legislated `fleet-repo`.</summary>
    private static (int Exit, string Out, string Err) Conduct(
        string command, string current = "master", MockFileSystem? fs = null, IProcessRunner? git = null) =>
        RunHooks.Run(
            Hook, RunHooks.BashPayload(command, RunHooks.Root),
            fs ?? RunHooks.LegislatedRepo(), git ?? RunHooks.ConductGit(current));

    // --- merging (R-641) ----------------------------------------------------

    [Fact]
    [Parity("hooks", "merge into default branch blocked (exit 2) per R-641")]
    [Parity("hooks", "merge block message names the rule per R-641")]
    public void Merge_into_default_branch_blocked()
    {
        var (exit, _, err) = Conduct("git merge bl/064-x");

        Assert.Equal(2, exit);
        Assert.Contains("pair-development", err, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("hooks", "merge INTO a feature branch allowed per R-641")]
    public void Merge_into_a_feature_branch_allowed()
    {
        Assert.Equal(0, Conduct("git merge master", current: "bl/064-x").Exit);
    }

    [Fact]
    [Parity("hooks", "merge inside a compound command blocked per R-641")]
    public void Merge_inside_a_compound_command_blocked()
    {
        Assert.Equal(2, Conduct("git fetch && git merge origin/master").Exit);
    }

    [Fact]
    [Parity("hooks", "merge --abort allowed per R-641")]
    public void Merge_abort_allowed()
    {
        Assert.Equal(0, Conduct("git merge --abort").Exit);
    }

    // --- pushing (R-642) ----------------------------------------------------

    [Fact]
    [Parity("hooks", "push origin master blocked per R-642")]
    public void Push_origin_master_blocked()
    {
        Assert.Equal(2, Conduct("git push origin master").Exit);
    }

    [Fact]
    [Parity("hooks", "push HEAD:master blocked per R-642")]
    public void Push_head_colon_master_blocked()
    {
        Assert.Equal(2, Conduct("git push origin HEAD:master").Exit);
    }

    [Fact]
    [Parity("hooks", "push of a task branch allowed per R-642")]
    public void Push_of_a_task_branch_allowed()
    {
        Assert.Equal(0, Conduct("git push -u origin bl/064-x", current: "bl/064-x").Exit);
    }

    [Fact]
    [Parity("hooks", "bare push on default branch blocked per R-642")]
    public void Bare_push_on_default_branch_blocked()
    {
        Assert.Equal(2, Conduct("git push").Exit);
    }

    // --- AI attribution (R-643, R-644) --------------------------------------

    [Fact]
    [Parity("hooks", "Claude co-author trailer blocked per R-643")]
    [Parity("hooks", "attribution block message says why per R-643")]
    public void Claude_co_author_trailer_blocked()
    {
        var (exit, _, err) = Conduct("git commit -m \"fix: x\n\nCo-Authored-By: Claude <noreply@anthropic.com>\"");

        Assert.Equal(2, exit);
        Assert.Contains("attribution", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Parity("hooks", "Generated-with footer blocked per R-643")]
    public void Generated_with_footer_blocked()
    {
        Assert.Equal(
            2,
            Conduct("git commit -m \"docs: y\" -m \"Generated with [Claude Code](https://claude.com/claude-code)\"").Exit);
    }

    [Fact]
    [Parity("hooks", "human co-author trailer allowed per R-643")]
    public void Human_co_author_trailer_allowed()
    {
        Assert.Equal(0, Conduct("git commit -m \"fix: x\n\nCo-Authored-By: Jane Doe <jane@example.com>\"").Exit);
    }

    [Fact]
    [Parity("hooks", "ordinary commit message allowed per R-643")]
    public void Ordinary_commit_message_allowed()
    {
        Assert.Equal(0, Conduct("git commit -m \"fix: ordinary message\"").Exit);
    }

    [Fact]
    [Parity("hooks", "grep for the pattern allowed per R-643")]
    public void Grep_for_the_pattern_allowed()
    {
        Assert.Equal(0, Conduct("grep \"Co-Authored-By: Claude\" README.md").Exit);
    }

    [Fact]
    [Parity("hooks", "attribution in gh pr body blocked per R-644")]
    public void Attribution_in_gh_pr_body_blocked()
    {
        Assert.Equal(
            2,
            Conduct("gh pr create --title \"x\" --body \"\U0001F916 Generated with [Claude Code](https://claude.com/claude-code)\"").Exit);
    }

    [Fact]
    [Parity("hooks", "clean gh pr body allowed per R-644")]
    public void Clean_gh_pr_body_allowed()
    {
        Assert.Equal(0, Conduct("gh pr create --title \"x\" --body \"plain delivery notes\"").Exit);
    }

    // --- merging the PR (R-645) ---------------------------------------------

    [Fact]
    [Parity("hooks", "gh pr merge blocked per R-645")]
    public void Gh_pr_merge_blocked()
    {
        Assert.Equal(2, Conduct("gh pr merge 23 --squash").Exit);
    }

    // --- undecidable and out of scope (R-646, R-647) ------------------------

    [Fact]
    [Parity("hooks", "detached HEAD merge allowed (can't tell) per R-646")]
    public void Detached_head_merge_allowed()
    {
        Assert.Equal(0, Conduct("git merge bl/064-x", git: RunHooks.DetachedGit()).Exit);
    }

    [Fact]
    [Parity("hooks", "malformed stdin allowed (exit 0) per R-646")]
    public void Malformed_stdin_allowed()
    {
        Assert.Equal(0, RunHooks.Run(Hook, "not json", RunHooks.LegislatedRepo(), RunHooks.ConductGit()).Exit);
    }

    [Fact]
    [Parity("hooks", "non-git command allowed per R-646")]
    public void Non_git_command_allowed()
    {
        Assert.Equal(0, Conduct("ls -la && echo done").Exit);
    }

    [Fact]
    [Parity("hooks", "non-legislated repo: merge on default allowed per R-647")]
    public void Non_legislated_repo_merge_on_default_allowed()
    {
        Assert.Equal(0, Conduct("git merge topic", fs: new MockFileSystem()).Exit);
    }

    // --- Windows-shaped command heads (R-701) -------------------------------

    [Fact]
    [Parity("hooks", "git.exe merge on default branch blocked per R-701")]
    public void Git_exe_merge_blocked()
    {
        Assert.Equal(2, Conduct("git.exe merge topic").Exit);
    }

    [Fact]
    [Parity("hooks", "backslashed git.exe path push blocked per R-701")]
    public void Backslashed_git_exe_path_push_blocked()
    {
        Assert.Equal(2, Conduct("\"C:\\Program Files\\Git\\bin\\git.exe\" push origin master").Exit);
    }

    [Fact]
    [Parity("hooks", "GIT.EXE merge blocked per R-701")]
    public void Upper_case_git_exe_merge_blocked()
    {
        Assert.Equal(2, Conduct("GIT.EXE merge topic").Exit);
    }

    [Fact]
    [Parity("hooks", "github-named binary not treated as git per R-701")]
    public void Github_named_binary_not_treated_as_git()
    {
        Assert.Equal(0, Conduct("github merge topic").Exit);
    }
}
