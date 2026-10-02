using Legislator.Core.Options;
using Legislator.Engine.Jobs;
using Legislator.Engine.Runs;
using Legislator.TestSupport;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// `ReviewLines`' tier-aware proposals (BL-484 R-001, R-007, R-008): the split is read straight
/// off a package's own `AGENTS.md.tpl`, and an on-demand rule's remove/add halves follow R-007's
/// guard - the owner's own pointer wording is never second-guessed against the template's. Every
/// fixture applies first (report needs a run record) and then overwrites the entry document, the
/// manifest and the template to the exact shape the scenario needs.
/// </summary>
public sealed class ReportJobTierTests
{
    private const string OkfOwned = "docs/ai/rules/core/okf.md";

    private const string OkfManifest =
        "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": [\"" + OkfOwned + "\"]}";

    private const string OkfPointer =
        "- Before changing code that implements a concept, read `docs/ai/rules/core/okf.md` — it is law, not a reference.";

    /// <summary>The "Needs your review" slice alone - `Created` legitimately lists every owned file, review-relevant or not.</summary>
    private static string Review(string report)
    {
        var after = report.Split("## Needs your review\n", 2)[1];
        return after.Split("\n\n", 2)[0];
    }

    private static void Applied(
        System.IO.Abstractions.TestingHelpers.MockFileSystem fs, string entry, string manifest, string template)
    {
        RunApply(fs, null, "--stacks", string.Empty);
        fs.File.WriteAllText($"{Root}/AGENTS.md", entry);
        fs.File.WriteAllText($"{Root}/docs/ai/manifest.json", manifest);
        fs.File.WriteAllText($"{SkillPath}/assets/templates/AGENTS.md.tpl", template);
    }

    [Fact]
    public void Given_an_always_tier_rule_missing_its_import_When_reported_Then_it_proposes_add()
    {
        var fs = Repo();
        Applied(fs, "# A\n", OkfManifest, "@docs/ai/rules/core/okf.md\n");

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- add to `AGENTS.md`: `@{OkfOwned}`", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_an_on_demand_rule_present_as_an_import_When_reported_Then_it_proposes_remove_and_add_pointer()
    {
        var fs = Repo();
        Applied(fs, $"# A\n\n@{OkfOwned}\n", OkfManifest, $"{OkfPointer}\n");

        var review = RunReport(fs).Stdout;

        Assert.Contains(
            $"- remove from `AGENTS.md`: `@{OkfOwned}` (on-demand: read on trigger, not imported)",
            review, StringComparison.Ordinal);
        Assert.Contains($"- add to `AGENTS.md`: {OkfPointer}", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_an_on_demand_rule_present_as_neither_import_nor_pointer_When_reported_Then_it_proposes_add_pointer_only()
    {
        var fs = Repo();
        Applied(fs, "# A\n", OkfManifest, $"{OkfPointer}\n");

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- add to `AGENTS.md`: {OkfPointer}", review, StringComparison.Ordinal);
        Assert.DoesNotContain("remove", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_an_on_demand_rule_already_pointer_wired_When_reported_Then_it_proposes_nothing()
    {
        var fs = Repo();
        Applied(fs, $"# A\n\n{OkfPointer}\n", OkfManifest, $"{OkfPointer}\n");

        var review = Review(RunReport(fs).Stdout);

        Assert.DoesNotContain(OkfOwned, review, StringComparison.Ordinal);
    }

    /// <summary>R-007: an owner's own wording, differing from the template's, is never re-proposed merely because it does not match byte for byte.</summary>
    [Fact]
    public void Given_an_on_demand_rule_under_a_reworded_pointer_When_reported_Then_the_owners_wording_is_left_alone()
    {
        var fs = Repo();
        Applied(
            fs, $"# A\n\n- Check `{OkfOwned}` before doing anything risky.\n", OkfManifest, $"{OkfPointer}\n");

        var review = Review(RunReport(fs).Stdout);

        Assert.DoesNotContain(OkfOwned, review, StringComparison.Ordinal);
    }

    private const string CodeBaseMapPointer =
        "- When finding where something lives, read `docs/okf/codebase-map.md` — it is law, not a reference.\n";

    [Fact]
    public void Given_the_codebase_map_present_on_disk_and_as_an_import_When_reported_Then_remove_and_add_fire()
    {
        var fs = Repo();
        Applied(
            fs,
            "# A\n\n@docs/okf/codebase-map.md\n",
            "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
            CodeBaseMapPointer);
        fs.AddFile($"{Root}/docs/okf/codebase-map.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# map\n"));

        // The codebase map's wiring is never gated on `scaffolded` when the file already exists
        // on disk - this fixture never ran a scaffold for it at all.
        var review = RunReport(fs).Stdout;

        Assert.Contains(
            "- remove from `AGENTS.md`: `@docs/okf/codebase-map.md` (on-demand: read on trigger, not imported)",
            review, StringComparison.Ordinal);
        Assert.Contains(
            "- add to `AGENTS.md`: - When finding where something lives, read `docs/okf/codebase-map.md` — "
            + "it is law, not a reference.",
            review, StringComparison.Ordinal);
    }

    /// <summary>f12: with no map file on disk and none scaffolded this run, neither half is proposed - even though the entry document imports it and the template carries its pointer.</summary>
    [Fact]
    public void Given_the_codebase_map_absent_from_disk_When_reported_Then_neither_remove_nor_add_is_proposed()
    {
        var fs = Repo();
        Applied(
            fs,
            "# A\n\n@docs/okf/codebase-map.md\n",
            "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
            CodeBaseMapPointer);

        var review = RunReport(fs).Stdout;

        Assert.DoesNotContain("docs/okf/codebase-map.md", Review(review), StringComparison.Ordinal);
    }

    /// <summary>f12: with no entry document at all, the map gets neither half proposed - the map "needs an entry document anyway" (review-1.md finding 12), the same guard the old code applied via its own `entry is not null` wrapper.</summary>
    [Fact]
    public void Given_no_entry_document_When_reported_Then_no_codebase_map_proposals_are_made()
    {
        var fs = Repo();
        RunApply(fs, null, "--stacks", string.Empty);
        fs.File.Delete($"{Root}/AGENTS.md");
        fs.File.WriteAllText($"{Root}/docs/ai/manifest.json", OkfManifest);
        fs.File.WriteAllText($"{SkillPath}/assets/templates/AGENTS.md.tpl", $"{OkfPointer}\n{CodeBaseMapPointer}");
        fs.AddFile($"{Root}/docs/okf/codebase-map.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# map\n"));

        var review = Review(RunReport(fs).Stdout);

        Assert.DoesNotContain("docs/okf/codebase-map.md", review, StringComparison.Ordinal);
    }

    /// <summary>f1: the map's tier comes only from the template's own pointer line - dropping the pointer means no on-demand proposal for the map, even though the file exists on disk and is imported.</summary>
    [Fact]
    public void Given_a_template_that_drops_the_codebase_map_pointer_When_reported_Then_no_on_demand_proposal_for_the_map()
    {
        var fs = Repo();
        Applied(
            fs,
            "# A\n\n@docs/okf/codebase-map.md\n",
            "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": []}",
            "no map pointer here\n");
        fs.AddFile($"{Root}/docs/okf/codebase-map.md", new System.IO.Abstractions.TestingHelpers.MockFileData("# map\n"));

        var review = Review(RunReport(fs).Stdout);

        Assert.DoesNotContain("docs/okf/codebase-map.md", review, StringComparison.Ordinal);
    }

    /// <summary>f6: the always-tier add branch keeps the manifest's own `ownedFiles` order, not a stack-then-core regrouping.</summary>
    [Fact]
    public void Given_owned_files_in_manifest_order_When_reported_Then_always_tier_adds_keep_that_order()
    {
        const string stackOwned = "docs/ai/rules/stacks/dotnet/a.md";
        var fs = Repo();
        Applied(
            fs,
            "# A\n",
            "{\"legislatorVersion\": 25, \"stacks\": [\"dotnet\"], \"keep\": [], \"ownedFiles\": [\""
                + stackOwned + "\", \"" + OkfOwned + "\"]}",
            "@docs/ai/rules/core/okf.md\n");

        var review = Review(RunReport(fs).Stdout);

        var stackIndex = review.IndexOf($"@{stackOwned}", StringComparison.Ordinal);
        var okfIndex = review.IndexOf($"@{OkfOwned}", StringComparison.Ordinal);
        Assert.True(stackIndex >= 0 && okfIndex >= 0 && stackIndex < okfIndex, review);
    }

    [Fact]
    public void Given_a_stack_rule_missing_its_import_When_reported_Then_it_still_proposes_add_regardless_of_the_template()
    {
        const string stackOwned = "docs/ai/rules/stacks/dotnet/a.md";
        var fs = Repo();
        // The template never names a stack rule at all - T-08's fallback ("today's add-`@import`
        // behavior") is what a path absent from the template entirely gets.
        Applied(
            fs,
            "# A\n",
            "{\"legislatorVersion\": 25, \"stacks\": [\"dotnet\"], \"keep\": [], \"ownedFiles\": [\""
                + stackOwned + "\"]}",
            "no rule paths here\n");

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- add to `AGENTS.md`: `@{stackOwned}`", review, StringComparison.Ordinal);
    }

    private const string VerificationOwned = "docs/ai/rules/core/verification.md";

    private const string VerificationManifest =
        "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": [\"" + VerificationOwned + "\"]}";

    private const string VerificationTemplate = $"@{VerificationOwned}\n";

    private const string VerificationPointer =
        "- Before writing tests or implementation code, and before reporting done, read "
        + "`docs/ai/rules/core/verification.md` — it is law, not a reference.";

    /// <summary>R-005: a rule now always-tier whose entry still carries its old on-demand pointer line proposes both the add-`@import` and the stale-pointer remove, in either order.</summary>
    [Fact]
    public void Given_an_edition_28_shaped_entry_for_a_now_always_tier_rule_When_reported_Then_it_proposes_add_import_and_remove_pointer()
    {
        var fs = Repo();
        Applied(fs, $"# A\n\n{VerificationPointer}\n", VerificationManifest, VerificationTemplate);

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- add to `AGENTS.md`: `@{VerificationOwned}`", review, StringComparison.Ordinal);
        Assert.Contains(
            $"- remove from `AGENTS.md`: the pointer line naming `{VerificationOwned}` "
            + "(now always-tier: imported, not read on trigger)",
            review, StringComparison.Ordinal);
    }

    /// <summary>R-005: an entry already at the edition-29 shape (`@import`, no pointer line) proposes neither half for that rule.</summary>
    [Fact]
    public void Given_an_edition_29_shaped_entry_for_an_always_tier_rule_When_reported_Then_it_proposes_neither_add_nor_remove()
    {
        var fs = Repo();
        Applied(fs, $"# A\n\n@{VerificationOwned}\n", VerificationManifest, VerificationTemplate);

        var review = Review(RunReport(fs).Stdout);

        Assert.DoesNotContain(VerificationOwned, review, StringComparison.Ordinal);
    }

    /// <summary>R-005 negative case: owner prose merely naming the rule's path outside the pointer grammar (this repository's own `AGENTS.md:26` shape) is never proposed for removal.</summary>
    [Fact]
    public void Given_the_rule_named_only_in_owner_prose_When_reported_Then_no_removal_is_proposed()
    {
        var fs = Repo();
        Applied(
            fs,
            $"# A\n\nThis is the backlog-ticket convention `{VerificationOwned}` defers to.\n",
            VerificationManifest,
            VerificationTemplate);

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- add to `AGENTS.md`: `@{VerificationOwned}`", review, StringComparison.Ordinal);
        Assert.DoesNotContain(
            $"- remove from `AGENTS.md`: the pointer line naming `{VerificationOwned}`",
            review, StringComparison.Ordinal);
    }

    private const string ChangelogOwned = "docs/ai/rules/core/changelog.md";

    private const string DevJournalOwned = "docs/ai/rules/core/dev-journal.md";

    private const string MergedManifest =
        "{\"legislatorVersion\": 25, \"stacks\": [], \"keep\": [], \"ownedFiles\": [\""
        + ChangelogOwned + "\", \"" + DevJournalOwned + "\"]}";

    private const string MergedTemplateLine =
        "- Before the first commit on a task branch, touching `CHANGELOG.md`/`docs/changes/`, or writing a "
        + "fragment's `## journal` section or editing `docs/journal/` directly, read "
        + "`docs/ai/rules/core/changelog.md` and `docs/ai/rules/core/dev-journal.md` — they are law, not a reference.";

    private const string OldChangelogLine =
        "- Before the first commit on a task branch, or touching `CHANGELOG.md`/`docs/changes/`, read "
        + "`docs/ai/rules/core/changelog.md` — it is law, not a reference.";

    private const string OldDevJournalLine =
        "- Before writing a fragment's `## journal` section, or editing `docs/journal/` directly, read "
        + "`docs/ai/rules/core/dev-journal.md` — it is law, not a reference.";

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        var at = 0;
        while ((at = haystack.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
        {
            count++;
            at += needle.Length;
        }

        return count;
    }

    /// <summary>R-010, R-014: two standalone old pointer bullets, both differing from the merged template line, propose two removes (one per old line) and a single merged add - not two.</summary>
    [Fact]
    public void Given_two_standalone_old_pointer_lines_merged_into_one_template_line_When_reported_Then_it_proposes_two_removes_and_one_add()
    {
        var fs = Repo();
        Applied(
            fs, $"# A\n\n{OldChangelogLine}\n{OldDevJournalLine}\n", MergedManifest, $"{MergedTemplateLine}\n");

        var review = RunReport(fs).Stdout;

        Assert.Contains($"- remove from `AGENTS.md`: the pointer line naming `{ChangelogOwned}`", review, StringComparison.Ordinal);
        Assert.Contains($"- remove from `AGENTS.md`: the pointer line naming `{DevJournalOwned}`", review, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(review, $"- add to `AGENTS.md`: {MergedTemplateLine}"));
    }

    /// <summary>R-014: an entry that already imports both of the merged bullet's rules with `@` lines proposes the merged add exactly once.</summary>
    [Fact]
    public void Given_an_entry_importing_both_merged_rules_with_at_lines_When_reported_Then_the_merged_add_is_proposed_exactly_once()
    {
        var fs = Repo();
        Applied(fs, $"# A\n\n@{ChangelogOwned}\n@{DevJournalOwned}\n", MergedManifest, $"{MergedTemplateLine}\n");

        var review = RunReport(fs).Stdout;

        Assert.Equal(1, Occurrences(review, $"- add to `AGENTS.md`: {MergedTemplateLine}"));
    }

    [Fact]
    public void Given_a_skill_package_with_no_AGENTS_md_tpl_When_reported_Then_it_throws_the_named_error_instead_of_a_silent_fallback()
    {
        var fs = Repo();
        Applied(fs, "# A\n", OkfManifest, "@docs/ai/rules/core/okf.md\n");
        fs.File.Delete($"{SkillPath}/assets/templates/AGENTS.md.tpl");

        Assert.Throws<AgentsTemplateMissingException>(() => RunReport(fs));
    }

    [Fact]
    public void Given_a_misconfigured_TemplateCorePrefix_When_reported_Then_the_layout_error_surfaces()
    {
        var fs = Repo();
        Applied(fs, "# A\n", OkfManifest, "@docs/ai/rules/core/okf.md\n");
        var options = Options with
        {
            TemplateCorePrefix = new("docs/ai/rules/core", OptionsLayer.Defaults),
        };
        var ctx = new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(),
            new FakeProcessRunner(), options, Root, ["--skill", SkillPath]);

        var ex = Assert.Throws<TemplateLayoutInvalidException>(() => new ReportJob().Run(ctx));

        Assert.Equal("template_core_prefix", ex.Key);
    }
}
