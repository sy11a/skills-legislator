using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;
using Legislator.Engine.Runs;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Runs;

/// <summary>
/// The tier split <c>TemplateTiers.Read</c> derives from a package's own <c>AGENTS.md.tpl</c>
/// (BL-484 R-001): an `@import` line is always-tier, any other line naming a core rule path (or
/// <c>docs/okf/codebase-map.md</c>) is on-demand, and the matched line is the proposal's pointer
/// text, verbatim. No fixed grammar, no engine-side closed list.
/// </summary>
public sealed class TemplateTiersTests
{
    /// <summary>The repository root - the directory holding `skill/SKILL.md` - walked up from the test binary, the same way <c>Legislator.Parity.Tests.Labels.RepoRoot</c> finds `evals/parity_labels.py`.</summary>
    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "skill", "SKILL.md")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"no directory above {AppContext.BaseDirectory} holds skill/SKILL.md");
    }

    [Fact]
    public void Given_the_real_AGENTS_md_tpl_When_tiers_are_read_Then_every_core_rule_and_the_codebase_map_lands_in_exactly_one_tier()
    {
        var fs = new FileSystem();
        var options = new LegislatorOptions();
        var skillRoot = Path.Combine(RepoRoot(), "skill").Replace('\\', '/');
        var skill = new SkillPackage(fs, options, skillRoot);
        var layout = new RepoLayout(options, "/r");

        var tiers = TemplateTiers.Read(fs, skill, layout, options);

        var coreDir = Path.Combine(RepoRoot(), "skill", "assets", "rules", "core");
        var names = Directory.GetFiles(coreDir, "*.md").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal);

        foreach (var name in names)
        {
            var owned = $"docs/ai/rules/core/{name}";
            var always = tiers.Always.Contains(owned);
            var onDemand = tiers.OnDemandLine.ContainsKey(owned);
            Assert.True(always ^ onDemand, $"{owned}: always={always} onDemand={onDemand}");
        }

        // project-rules.md classifies on-demand from its existing prose line, with no
        // rule-specific text in C# - the same lookup every other on-demand rule uses.
        Assert.False(tiers.Always.Contains("docs/ai/rules/core/project-rules.md"));
        Assert.Contains("docs/ai/rules/core/project-rules.md", tiers.OnDemandLine.Keys);
        Assert.Contains("project-rules.md", tiers.OnDemandLine["docs/ai/rules/core/project-rules.md"]);

        Assert.True(tiers.Always.Contains("docs/ai/rules/core/pair-development.md"));
        Assert.True(tiers.Always.Contains("docs/ai/rules/core/decision-gate.md"));
        Assert.Contains("docs/okf/codebase-map.md", tiers.OnDemandLine.Keys);
    }

    [Fact]
    public void Given_a_skill_package_with_no_AGENTS_md_tpl_When_tiers_are_read_Then_it_throws_the_named_error_instead_of_a_silent_fallback()
    {
        var fs = Repo();
        fs.File.Delete($"{SkillPath}/assets/templates/AGENTS.md.tpl");
        var skill = Package(fs);

        var ex = Assert.Throws<AgentsTemplateMissingException>(
            () => TemplateTiers.Read(fs, skill, Layout, Options));

        Assert.Contains($"{SkillPath}/assets/templates/AGENTS.md.tpl", ex.Path, StringComparison.Ordinal);
    }

    /// <summary>f3: two core-rule paths named on the same template line both land on-demand - neither falls through to always-tier by only recording the line's first occurrence.</summary>
    [Fact]
    public void Given_a_template_line_naming_two_core_paths_When_tiers_are_read_Then_both_land_on_demand()
    {
        var fs = Repo();
        fs.File.WriteAllText(
            $"{SkillPath}/assets/templates/AGENTS.md.tpl",
            "- See `docs/ai/rules/core/okf.md` and `docs/ai/rules/core/sdd.md` together.\n");
        var skill = Package(fs);

        var tiers = TemplateTiers.Read(fs, skill, Layout, Options);

        Assert.Contains("docs/ai/rules/core/okf.md", tiers.OnDemandLine.Keys);
        Assert.Contains("docs/ai/rules/core/sdd.md", tiers.OnDemandLine.Keys);
    }

    /// <summary>f3: a core-rule path and the codebase map named on the same line both get an on-demand entry - the map is not lost to the `continue` the once-per-line cap used to take.</summary>
    [Fact]
    public void Given_a_template_line_naming_a_core_path_and_the_codebase_map_When_tiers_are_read_Then_both_land_on_demand()
    {
        var fs = Repo();
        fs.File.WriteAllText(
            $"{SkillPath}/assets/templates/AGENTS.md.tpl",
            "- See `docs/ai/rules/core/okf.md` and `docs/okf/codebase-map.md` together.\n");
        var skill = Package(fs);

        var tiers = TemplateTiers.Read(fs, skill, Layout, Options);

        Assert.Contains("docs/ai/rules/core/okf.md", tiers.OnDemandLine.Keys);
        Assert.Contains("docs/okf/codebase-map.md", tiers.OnDemandLine.Keys);
    }

    /// <summary>f9: a path immediately followed by sentence punctuation does not absorb the punctuation into the matched path.</summary>
    [Fact]
    public void Given_a_path_followed_by_trailing_punctuation_When_tiers_are_read_Then_the_punctuation_is_not_part_of_the_path()
    {
        var fs = Repo();
        fs.File.WriteAllText(
            $"{SkillPath}/assets/templates/AGENTS.md.tpl",
            "- Read docs/ai/rules/core/okf.md. Then read docs/ai/rules/core/sdd.md, and (docs/ai/rules/core/okf.md).\n");
        var skill = Package(fs);

        var tiers = TemplateTiers.Read(fs, skill, Layout, Options);

        Assert.Contains("docs/ai/rules/core/okf.md", tiers.OnDemandLine.Keys);
        Assert.Contains("docs/ai/rules/core/sdd.md", tiers.OnDemandLine.Keys);
        Assert.DoesNotContain("docs/ai/rules/core/okf.md.", tiers.OnDemandLine.Keys);
        Assert.DoesNotContain("docs/ai/rules/core/okf.md)", tiers.OnDemandLine.Keys);
    }

    /// <summary>f7: a CRLF template still classifies an `@import` line as always-tier - the regex anchors against `\n`, not a bare `$` that a trailing `\r` would desynchronise.</summary>
    [Fact]
    public void Given_a_CRLF_template_When_tiers_are_read_Then_the_always_tier_import_is_still_recognised()
    {
        var fs = Repo();
        fs.File.WriteAllText(
            $"{SkillPath}/assets/templates/AGENTS.md.tpl",
            "@docs/ai/rules/core/okf.md\r\n\r\n- Read `docs/ai/rules/core/sdd.md` too.\r\n");
        var skill = Package(fs);

        var tiers = TemplateTiers.Read(fs, skill, Layout, Options);

        Assert.Contains("docs/ai/rules/core/okf.md", tiers.Always);
        Assert.Contains("docs/ai/rules/core/sdd.md", tiers.OnDemandLine.Keys);
    }

    [Fact]
    public void Given_a_non_default_RulesDir_and_OkfDir_When_tiers_are_read_Then_the_template_still_classifies_against_the_repos_actual_layout()
    {
        var options = new LegislatorOptions
        {
            RulesDir = new("law", OptionsLayer.Defaults),
            OkfDir = new("knowledge", OptionsLayer.Defaults),
        };
        var layout = new RepoLayout(options, "/r");

        var fs = new MockFileSystem();
        fs.AddDirectory(SkillPath);
        fs.AddFile($"{SkillPath}/VERSION", new MockFileData("25\n"));
        fs.AddFile(
            $"{SkillPath}/assets/templates/AGENTS.md.tpl",
            new MockFileData(
                "@docs/ai/rules/core/pair-development.md\n\n"
                + "- Before changing code, read `docs/ai/rules/core/okf.md` — it is law, not a reference.\n"
                + "- When finding where something lives, read `docs/okf/codebase-map.md` — it is law, not a reference.\n"));
        var skill = new SkillPackage(fs, options, SkillPath);

        var tiers = TemplateTiers.Read(fs, skill, layout, options);

        Assert.Contains("docs/ai/law/core/pair-development.md", tiers.Always);
        Assert.Contains("docs/ai/law/core/okf.md", tiers.OnDemandLine.Keys);
        Assert.Contains("docs/ai/law/core/okf.md", tiers.OnDemandLine["docs/ai/law/core/okf.md"]);
        Assert.Contains("docs/knowledge/codebase-map.md", tiers.OnDemandLine.Keys);
        Assert.Contains("docs/knowledge/codebase-map.md", tiers.OnDemandLine["docs/knowledge/codebase-map.md"]);
    }
}
