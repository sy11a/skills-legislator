using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Sdd;
using Xunit;

namespace Legislator.Engine.Tests.Sdd;

/// <summary>
/// The change fragment's shape (core/changelog.md). L-3 shipped this lint with no test of its
/// own; these are its first, and they carry the composition law BL-347 added — one changelog
/// bullet, a journal section that is present, and an okf-log section that is optional because
/// inside a case the record is the case summary.
/// </summary>
public sealed class FragmentLintTests
{
    private const string Branch = "bl/347-retelling-layer";

    private sealed class BranchRunner(string branch) : IProcessRunner
    {
        public ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
            => new(0, branch + "\n", "");
    }

    private static string Fragment(string caseKey, string sections)
        => $"---\ncase: {caseKey}\nissue: 405\nkind: Added\ndate: 2026-09-19\n---\n\n{sections}";

    private static IReadOnlyList<string> Findings(string fragment, string name = "BL-347.md", string branch = Branch)
    {
        var fs = new MockFileSystem();
        fs.AddFile($"/r/docs/changes/{name}", new MockFileData(fragment));
        var options = new LegislatorOptions();
        var layout = new RepoLayout(options, "/r");
        return [.. FragmentLint.Findings(fs, layout, new BranchRunner(branch), "/r", options)];
    }

    private static IReadOnlyList<string> Findings((string Name, string Content)[] fragments, string branch)
    {
        var fs = new MockFileSystem();
        foreach (var (name, content) in fragments)
        {
            fs.AddFile($"/r/docs/changes/{name}", new MockFileData(content));
        }

        var options = new LegislatorOptions();
        var layout = new RepoLayout(options, "/r");
        return [.. FragmentLint.Findings(fs, layout, new BranchRunner(branch), "/r", options)];
    }

    private const string OneBullet = "## changelog\n\n- One line, pointing at the summary.\n\n## journal\n\nNo dead end; the decision is in ADR 0021.\n";

    [Fact]
    public void The_homes_own_readme_is_not_a_fragment()
    {
        // #53, found by the first delivery of edition 26 into a consuming repo:
        // Step 4 scaffolds docs/changes/README.md from a template, the enumeration
        // took every *.md, and the repository went red the moment it adopted the
        // mechanism correctly — with no way to clear it but deleting its own README.
        // `master` is used so the branch check (which always bounds to its own case)
        // does not answer for a fragment this test deliberately does not provide.
        Assert.Empty(Findings("# Change Fragments\n\nOne fragment per case.\n",
                              name: "README.md", branch: "master"));
    }

    [Fact]
    public void A_file_not_named_for_a_case_is_not_a_fragment()
    {
        // The gate is the NAME, not a by-name skip of README: anything else the home
        // grows — notes, a scratch file — is furniture too.
        Assert.Empty(Findings("no front matter here\n", name: "notes.md", branch: "master"));
    }

    [Fact]
    public void A_case_named_file_declaring_another_case_is_a_finding()
    {
        // The other half of naming being the gate. `render` inserts by case key, so a
        // fragment filed under someone else's name lands in the wrong place silently.
        // The branch check does not pile on: the file named BL-347 IS this branch's
        // fragment, so nothing is reported as absent — only the misdeclaration is.
        var all = Findings(Fragment("BL-999", OneBullet), name: "BL-347.md");
        Assert.Single(all);
        var f = Assert.Single(all, x => x.Contains("declares case 'BL-999'", StringComparison.Ordinal));
        Assert.Contains("named 'BL-347'", f);
    }

    [Fact]
    public void A_case_named_file_without_front_matter_is_still_a_finding()
    {
        // Naming as the gate must not become a way to smuggle a broken fragment past
        // the lint: a case-named file is held to the whole shape. `master` isolates the
        // shape finding — the malformed file IS the branch's fragment, so nothing is
        // reported as absent.
        var f = Assert.Single(Findings("## changelog\n\n- One line.\n", name: "BL-347.md", branch: "master"));
        Assert.Contains("no YAML front matter", f);
    }

    [Fact]
    public void A_lawful_fragment_is_silent()
    {
        Assert.Empty(Findings(Fragment("BL-347", OneBullet)));
    }

    [Fact]
    public void An_omitted_okf_log_section_is_lawful()
    {
        // the ordinary case since BL-347: inside a case the record is docs/cases/<case>/summary.md
        Assert.Empty(Findings(Fragment("BL-347", OneBullet)));
    }

    [Fact]
    public void Two_changelog_bullets_are_a_finding()
    {
        var f = Assert.Single(Findings(Fragment("BL-347",
            "## changelog\n\n- First.\n- Second.\n\n## journal\n\nNothing owed.\n")));
        Assert.Contains("carries 2 bullets, not one", f, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_changelog_section_is_a_finding()
    {
        var f = Assert.Single(Findings(Fragment("BL-347", "## journal\n\nNothing owed.\n")));
        Assert.Contains("no '## changelog' section", f, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_journal_section_is_a_finding()
    {
        var f = Assert.Single(Findings(Fragment("BL-347", "## changelog\n\n- One line.\n")));
        Assert.Contains("no '## journal' section", f, StringComparison.Ordinal);
    }

    /// <summary>
    /// A fenced example inside a fragment is illustration. Counting its bullets would redden a
    /// lawful fragment that quotes another one — which is how this rule's own law file is written.
    /// </summary>
    [Fact]
    public void Bullets_inside_a_fence_do_not_count()
    {
        Assert.Empty(Findings(Fragment("BL-347",
            "## changelog\n\n- One line.\n\n```markdown\n## changelog\n\n- A quoted example.\n- And another.\n```\n\n## journal\n\nNothing owed.\n")));
    }

    /// <summary>A continuation line indented under a bullet is one bullet, not two.</summary>
    [Fact]
    public void A_wrapped_bullet_is_one_bullet()
    {
        Assert.Empty(Findings(Fragment("BL-347",
            "## changelog\n\n- One line that wraps\n  - and carries a nested point.\n\n## journal\n\nNothing owed.\n")));
    }

    /// <summary>
    /// The branch forms this fleet writes. Before BL-347 the check was a plain Contains, so
    /// `bl/347-retelling-layer` did not match `BL-347` and `l/3-change-fragments` did not
    /// match `L-3` — it fired on every correct fragment. After BL-441 the question is
    /// inverted: a branch answers only about the case its own name carries, and an own-case
    /// fragment in either form is silence.
    /// </summary>
    [Theory]
    [InlineData("bl/347-retelling-layer", "BL-347")]
    [InlineData("l/3-change-fragments", "L-3")]
    [InlineData("bl/BL-347-verbatim", "BL-347")]
    [InlineData("feature/bl-441-x", "BL-441")]
    [InlineData("feature/user/bl-441-x", "BL-441")]
    public void A_branch_names_its_own_case_in_either_form(string branch, string caseKey)
    {
        Assert.Empty(Findings(Fragment(caseKey, OneBullet), $"{caseKey}.md", branch));
    }

    /// <summary>
    /// The reproduction from #51: fragments accumulate until a release cut, so a branch that
    /// carries a case key and has written its own fragment must not redden on another case's
    /// fragment that sits beside it after a rebase onto the release branch.
    /// </summary>
    [Fact]
    public void A_second_cases_fragment_is_silent_on_this_branch()
    {
        var findings = Findings(
            [
                ("BL-1.md", Fragment("BL-1", OneBullet)),
                ("BL-2.md", Fragment("BL-2", OneBullet)),
            ],
            "bl/1-x");

        Assert.Empty(findings);
    }

    /// <summary>
    /// The one error the branch check exists to catch, and the only path that still reports
    /// by name: a branch that carries a case key and changed something but wrote no fragment
    /// for that case. A foreign fragment (L-3, another merged case) says nothing.
    /// </summary>
    [Fact]
    public void The_branchs_case_without_a_fragment_is_a_finding()
    {
        var findings = Findings([("L-3.md", Fragment("L-3", OneBullet))], Branch);

        var finding = Assert.Single(findings);
        Assert.Contains("case 'BL-347' has no change fragment", finding, StringComparison.Ordinal);
        Assert.DoesNotContain("case 'L-3'", finding, StringComparison.Ordinal);
    }

    /// <summary>
    /// The case a branch names is reported in the law's key form (`BL-NNN`, letters
    /// upper-cased) whatever the branch's own casing or shape: slashed (`bl/441-x`, `l/3-x`),
    /// verbatim (`bl/BL-441-x`, `feature/bl-441-x`), or a group-prefixed verbatim ticket
    /// (`feature/user/bl-441-x` — the ticket is the last segment, so the slice that read the
    /// first segment would silently lose the key). A foreign fragment says nothing; the finding
    /// names only the case whose fragment is missing.
    /// </summary>
    [Theory]
    [InlineData("bl/441-x", "BL-441")]
    [InlineData("bl/BL-441-x", "BL-441")]
    [InlineData("feature/bl-441-x", "BL-441")]
    [InlineData("feature/user/bl-441-x", "BL-441")]
    [InlineData("l/3-x", "L-3")]
    public void A_branch_naming_its_case_reports_its_absent_fragment(string branch, string caseKey)
    {
        var findings = Findings([("BL-999.md", Fragment("BL-999", OneBullet))], branch);

        var finding = Assert.Single(findings);
        Assert.Contains($"case '{caseKey}' has no change fragment", finding, StringComparison.Ordinal);
        Assert.DoesNotContain("case 'BL-999'", finding, StringComparison.Ordinal);
    }

    /// <summary>
    /// The number must end where the key's number ends (`BranchMatchesCase`'s boundary): a
    /// branch `bl/3470-other` carries `BL-3470`, not `BL-347`, so a `BL-347` fragment does
    /// not satisfy it and the branch is reported as having no fragment.
    /// </summary>
    [Fact]
    public void A_number_is_not_truncated_to_a_shorter_case()
    {
        var findings = Findings(Fragment("BL-347", OneBullet), "BL-347.md", "bl/3470-other");

        Assert.Contains(findings, x => x.Contains("case 'BL-3470' has no change fragment", StringComparison.Ordinal));
    }

    /// <summary>
    /// A branch that names no case — an integration branch, a release branch, a detached
    /// HEAD — reports no branch finding at all, whatever fragments sit in the tree. This is
    /// the release-branch half of the defect: `release/3` reddened on every accumulated
    /// fragment, and it must say nothing.
    /// </summary>
    [Theory]
    [InlineData("master")]
    [InlineData("main")]
    [InlineData("release/3")]
    [InlineData("rc/edition-27")]
    [InlineData("release/edition-3")]
    [InlineData("task/12-x")]
    [InlineData("wave/2")]
    [InlineData("v27/task-12")]
    [InlineData("feature/fix-404-page")]
    [InlineData("dependabot/npm_and_yarn/frontend/eslint-8.57.0")]
    [InlineData("(HEAD detached at deadbeef)")]
    public void A_branch_that_names_no_case_reports_no_branch_finding(string branch)
    {
        var findings = Findings(
            [
                ("BL-1.md", Fragment("BL-1", OneBullet)),
                ("BL-2.md", Fragment("BL-2", OneBullet)),
            ],
            branch);

        Assert.Empty(findings);
    }
}
