using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// `ReviewLines` end to end against a non-default `RulesDir`/`OkfDir` (BL-484 F-3/T-09): the
/// template is always written against the default layout, so the tier split and the proposals it
/// drives must fold every path through the repo's actual layout before they reach the report -
/// not only at `TemplateTiers.Read`'s own unit level (`TemplateTiersTests.cs`), but through the
/// full `ReportJob` run an owner actually sees.
/// </summary>
public sealed class ReportJobLayoutTests
{
    private static readonly LegislatorOptions LayoutOptions = Options with
    {
        RulesDir = new("law", OptionsLayer.Defaults),
        OkfDir = new("knowledge", OptionsLayer.Defaults),
    };

    private const string Template =
        "@docs/ai/rules/core/pair-development.md\n\n"
        + "- Before changing code, read `docs/ai/rules/core/okf.md` — it is law, not a reference.\n"
        + "- When finding where something lives, read `docs/okf/codebase-map.md` — it is law, not a reference.\n";

    private static JobResult RunApplyLayout(MockFileSystem fs) =>
        new ApplyJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), NoRepo(), LayoutOptions, Root,
            ["--skill", SkillPath, "--stacks", string.Empty]));

    private static JobResult RunReportLayout(MockFileSystem fs) =>
        new ReportJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), LayoutOptions, Root,
            ["--skill", SkillPath]));

    [Fact]
    public void Given_a_non_default_RulesDir_and_OkfDir_When_reported_Then_proposals_are_folded_through_the_repos_actual_layout()
    {
        var fs = Repo();
        fs.File.WriteAllText($"{SkillPath}/assets/templates/AGENTS.md.tpl", Template);
        RunApplyLayout(fs);
        fs.File.WriteAllText($"{Root}/AGENTS.md", "# A\n\n@docs/knowledge/codebase-map.md\n");
        fs.File.WriteAllText(
            $"{Root}/docs/ai/manifest.json",
            "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": "
            + "[\"docs/ai/law/core/pair-development.md\", \"docs/ai/law/core/okf.md\"]}");
        fs.AddFile($"{Root}/docs/knowledge/codebase-map.md", new MockFileData("# map\n"));

        var review = RunReportLayout(fs).Stdout;

        Assert.Contains(
            "- add to `AGENTS.md`: `@docs/ai/law/core/pair-development.md`", review, StringComparison.Ordinal);
        Assert.Contains(
            "- add to `AGENTS.md`: - Before changing code, read `docs/ai/law/core/okf.md` — "
            + "it is law, not a reference.",
            review, StringComparison.Ordinal);
        Assert.Contains(
            "- remove from `AGENTS.md`: `@docs/knowledge/codebase-map.md` (on-demand: read on trigger, not imported)",
            review, StringComparison.Ordinal);
        Assert.DoesNotContain("docs/ai/rules/core/pair-development.md", review, StringComparison.Ordinal);
    }
}
