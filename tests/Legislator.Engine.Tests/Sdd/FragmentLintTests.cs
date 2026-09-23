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

    private const string OneBullet = "## changelog\n\n- One line, pointing at the summary.\n\n## journal\n\nNo dead end; the decision is in ADR 0021.\n";

    [Fact]
    public void The_homes_own_readme_is_not_a_fragment()
    {
        // #53, found by the first delivery of edition 26 into a consuming repo:
        // Step 4 scaffolds docs/changes/README.md from a template, the enumeration
        // took every *.md, and the repository went red the moment it adopted the
        // mechanism correctly — with no way to clear it but deleting its own README.
        Assert.Empty(Findings("# Change Fragments\n\nOne fragment per case.\n",
                              name: "README.md"));
    }

    [Fact]
    public void A_file_not_named_for_a_case_is_not_a_fragment()
    {
        // The gate is the NAME, not a by-name skip of README: anything else the home
        // grows — notes, a scratch file — is furniture too.
        Assert.Empty(Findings("no front matter here\n", name: "notes.md"));
    }

    [Fact]
    public void A_case_named_file_declaring_another_case_is_a_finding()
    {
        // The other half of naming being the gate. `render` inserts by case key, so a
        // fragment filed under someone else's name lands in the wrong place silently.
        var f = Assert.Single(Findings(Fragment("BL-999", OneBullet), name: "BL-347.md"));
        Assert.Contains("declares case 'BL-999'", f);
        Assert.Contains("named 'BL-347'", f);
    }

    [Fact]
    public void A_case_named_file_without_front_matter_is_still_a_finding()
    {
        // Naming as the gate must not become a way to smuggle a broken fragment past
        // the lint: a case-named file is held to the whole shape.
        var f = Assert.Single(Findings("## changelog\n\n- One line.\n", name: "BL-347.md"));
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
    /// The branch form every real branch in this fleet uses. Before BL-347 the check was a
    /// plain Contains, so `bl/347-retelling-layer` did not match `BL-347` and the lint fired
    /// on every correct fragment — including this repository's own `L-3`.
    /// </summary>
    [Theory]
    [InlineData("bl/347-retelling-layer", "BL-347", true)]
    [InlineData("l/3-change-fragments", "L-3", true)]
    [InlineData("bl/BL-347-verbatim", "BL-347", true)]
    [InlineData("bl/3470-other", "BL-347", false)]
    [InlineData("bl/348-another-case", "BL-347", false)]
    [InlineData("master", "BL-347", false)]
    public void A_branch_names_its_case_in_either_form(string branch, string caseKey, bool matches)
    {
        var findings = Findings(
            $"---\ncase: {caseKey}\nissue: 405\nkind: Added\ndate: 2026-09-19\n---\n\n{OneBullet}",
            $"{caseKey}.md",
            branch);

        if (matches)
        {
            Assert.Empty(findings);
        }
        else
        {
            Assert.Contains(findings, f => f.Contains("does not match current branch", StringComparison.Ordinal));
        }
    }
}
