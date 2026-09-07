using Legislator.Engine.Apply;
using Xunit;

namespace Legislator.Engine.Tests.Apply;

/// <summary>
/// The keep rules, one branch at a time. The parity twin drives all of them at once through one
/// manifest; these tests separate the three refusals, because a run that refuses for the wrong
/// reason still produces the twin's manifest and the twin would not notice.
/// </summary>
public sealed class KeepListTests
{
    private static readonly HashSet<string> Owned = new(StringComparer.Ordinal) { "docs/ai/rules/core/okf.md" };

    private static readonly HashSet<string> Generated = new(StringComparer.Ordinal) { "docs/ai/baseline.md" };

    private static KeepOutcome Resolve(
        IEnumerable<KeepEntry>? carried = null,
        IEnumerable<(string Path, string Reason)>? add = null,
        IEnumerable<string>? remove = null,
        params string[] onDisk) =>
        KeepList.Resolve(
            carried ?? [], add ?? [], remove ?? [], Owned, Generated,
            path => onDisk.Contains(path, StringComparer.Ordinal));

    [Fact]
    public void Given_an_owned_path_When_it_is_added_Then_it_is_refused_as_machine_managed()
    {
        var outcome = Resolve(add: [("docs/ai/rules/core/okf.md", "mine")], onDisk: "docs/ai/rules/core/okf.md");

        Assert.Empty(outcome.Final);
        var refused = Assert.Single(outcome.Refused);
        Assert.Equal("docs/ai/rules/core/okf.md", refused.Path);
        Assert.Contains("owned", refused.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_generated_artifact_When_it_is_added_Then_it_is_refused_as_unkeepable()
    {
        var outcome = Resolve(add: [("docs/ai/baseline.md", "mine")], onDisk: "docs/ai/baseline.md");

        var refused = Assert.Single(outcome.Refused);
        Assert.Equal("docs/ai/baseline.md", refused.Path);
        Assert.Contains("generated", refused.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_path_that_is_not_on_disk_When_it_is_added_Then_it_is_refused_for_not_existing()
    {
        var outcome = Resolve(add: [("docs/nope.md", "mine")]);

        var refused = Assert.Single(outcome.Refused);
        Assert.Contains("exist", refused.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_path_already_kept_When_it_is_added_with_a_new_reason_Then_the_new_reason_replaces_the_old_one()
    {
        var outcome = Resolve(
            carried: [new KeepEntry("docs/notes/b.md", "old")],
            add: [("docs/notes/b.md", "new reason")],
            onDisk: "docs/notes/b.md");

        var entry = Assert.Single(outcome.Final);
        Assert.Equal("docs/notes/b.md", entry.Path);
        Assert.Equal("new reason", entry.Reason);
    }

    [Fact]
    public void Given_entries_out_of_order_When_the_list_is_resolved_Then_it_comes_back_sorted_by_path()
    {
        var outcome = Resolve(
            carried: [new KeepEntry("docs/notes/z.md", "z")],
            add: [("docs/notes/a.md", "a")],
            onDisk: ["docs/notes/a.md", "docs/notes/z.md"]);

        Assert.Equal(["docs/notes/a.md", "docs/notes/z.md"], outcome.Final.Select(k => k.Path));
    }

    [Fact]
    public void Given_a_path_that_is_not_kept_When_it_is_removed_Then_nothing_is_recorded_as_removed()
    {
        var outcome = Resolve(carried: [new KeepEntry("docs/notes/a.md", "a")], remove: ["docs/notes/b.md"]);

        Assert.Empty(outcome.Removed);
        Assert.Single(outcome.Final);
    }

    [Fact]
    public void Given_a_carried_entry_and_no_instruction_When_the_list_is_resolved_Then_it_is_carried_unchanged()
    {
        var outcome = Resolve(carried: [new KeepEntry("docs/notes/a.md", "a")]);

        var entry = Assert.Single(outcome.Final);
        Assert.Equal(("docs/notes/a.md", "a"), (entry.Path, entry.Reason));
        Assert.Empty(outcome.Added);
        Assert.Empty(outcome.Refused);
    }
}
