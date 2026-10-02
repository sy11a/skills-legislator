using Legislator.Engine.Runs;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Runs;

/// <summary>
/// `Step4Targets.ScaffoldOnlyPaths` (BL-487 R-012, R-025): the one Step-4 row restricted to
/// fresh-scaffold mode, identified by its own path - not by matching the "Fresh-scaffold mode
/// only" phrasing that also appears, coincidentally, in the `AGENTS.md` row's note. `Of` and
/// `Snapshot` stay exactly as they were: unfiltered for every caller.
/// </summary>
public sealed class Step4TargetsTests
{
    private const string JournalTarget = "docs/journal/<today>.md";

    /// <summary>Both rows this case's table carries a "fresh-scaffold mode only" note for - the `AGENTS.md` row (D3: its note reads the identical phrase) and the journal row. A notes-text match would over-include the former.</summary>
    private const string Step4Table =
        "## Step 4\n\n| Target | Template | Notes |\n|---|---|---|\n"
        + "| `AGENTS.md` | AGENTS.md.tpl | Only in fresh-scaffold mode (authority: entry document × scaffold) - legacy migration mode handles this file per Step 5 instead. |\n"
        + "| `docs/cases/README.md` | cases.md | |\n"
        + "| `" + JournalTarget + "` | journal.md | Fresh-scaffold mode only - an upgrade writes no entry. |\n"
        + "\n## Step 5\n";

    [Fact]
    public void Given_a_table_with_both_fresh_scaffold_only_rows_When_scaffold_only_paths_is_read_Then_it_returns_exactly_the_journal_row()
    {
        var fs = Repo();
        fs.File.WriteAllText($"{SkillPath}/SKILL.md", Step4Table);

        var scaffoldOnly = Step4Targets.ScaffoldOnlyPaths(fs, Package(fs), Options);

        Assert.Equal([JournalTarget], scaffoldOnly);
    }

    [Fact]
    public void Given_a_table_with_no_journal_row_When_scaffold_only_paths_is_read_Then_it_returns_empty()
    {
        var fs = Repo();
        fs.File.WriteAllText(
            $"{SkillPath}/SKILL.md",
            "## Step 4\n\n| Target | Template |\n|---|---|\n| `docs/cases/README.md` | cases.md |\n\n## Step 5\n");

        var scaffoldOnly = Step4Targets.ScaffoldOnlyPaths(fs, Package(fs), Options);

        Assert.Empty(scaffoldOnly);
    }

    /// <summary>Regression: `Of`/`Snapshot` are unfiltered regardless of `ScaffoldOnlyPaths` - both rows this case's table carries a "fresh-scaffold mode only" note for still appear in both.</summary>
    [Fact]
    public void Given_the_same_table_When_Of_and_Snapshot_are_read_Then_both_fresh_scaffold_only_rows_still_appear_unfiltered()
    {
        var fs = Repo();
        fs.File.WriteAllText($"{SkillPath}/SKILL.md", Step4Table);

        var targets = Step4Targets.Of(fs, Package(fs), Options);
        var snapshot = Step4Targets.Snapshot(fs, Package(fs), Options, Layout);

        Assert.Contains("AGENTS.md", targets);
        Assert.Contains(JournalTarget, targets);
        Assert.True(snapshot.ContainsKey("AGENTS.md"));
        Assert.True(snapshot.ContainsKey(JournalTarget));
    }
}
