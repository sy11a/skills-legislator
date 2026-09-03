using Legislator.Engine.Apply;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Apply;

/// <summary>
/// Which files the package delivers into a repository, and the inverse question the audit's
/// owned-integrity check has been asking since T-09. The parity twins pin the set for one
/// subscription; these tests pin what a subscription selects, and the case-collision reading
/// that has no ruler label at all.
/// </summary>
public sealed class OwnedSetTests
{
    [Fact]
    public void Given_no_stack_subscribed_When_the_owned_set_is_derived_Then_it_is_the_core_rules_and_the_two_templated_files()
    {
        var fs = Repo();

        var owned = OwnedSet.Of(fs, Package(fs), Layout, Options, []);

        Assert.Equal(
            ["docs/ai/engine.py", "docs/ai/rules/core/okf.md", "docs/ai/rules/core/sdd.md", "opencode.json"],
            owned.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Given_one_stack_subscribed_When_the_owned_set_is_derived_Then_only_that_stack_is_in_it()
    {
        var fs = Repo();

        var owned = OwnedSet.Of(fs, Package(fs), Layout, Options, ["dotnet"]);

        Assert.Contains("docs/ai/rules/stacks/dotnet/a.md", owned.Keys);
        Assert.DoesNotContain("docs/ai/rules/stacks/aurelia/x.md", owned.Keys);
    }

    [Fact]
    public void Given_a_stack_the_package_does_not_ship_When_the_owned_set_is_derived_Then_it_contributes_nothing()
    {
        var fs = Repo();

        var owned = OwnedSet.Of(fs, Package(fs), Layout, Options, ["ghost"]);

        Assert.DoesNotContain(owned.Keys, k => k.Contains("ghost", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_an_owned_path_When_its_source_is_asked_for_Then_it_names_the_file_inside_the_package()
    {
        var fs = Repo();

        Assert.Equal(
            $"{SkillPath}/assets/rules/core/okf.md",
            OwnedSet.SourceOf("docs/ai/rules/core/okf.md", Package(fs), Layout, Options));
        Assert.Equal(
            $"{SkillPath}/assets/engine/engine.py",
            OwnedSet.SourceOf("docs/ai/engine.py", Package(fs), Layout, Options));
        Assert.Equal(
            $"{SkillPath}/assets/templates/opencode.json.tpl",
            OwnedSet.SourceOf("opencode.json", Package(fs), Layout, Options));
    }

    [Fact]
    public void Given_a_path_the_package_does_not_author_When_its_source_is_asked_for_Then_there_is_none()
    {
        var fs = Repo();

        Assert.Null(OwnedSet.SourceOf("README.md", Package(fs), Layout, Options));
    }

    /// <summary>
    /// The item carried in by the T-09 ruling. On a case-insensitive checkout the two paths are
    /// one file, so the delivered copy and the repository's own document overwrite each other
    /// silently; on this one they merely sit side by side, which is why nothing else notices.
    /// </summary>
    [Fact]
    public void Given_a_file_differing_from_an_owned_path_only_by_case_When_collisions_are_read_Then_it_is_named_with_the_path_it_collides_with()
    {
        var fs = Repo(new() { ["docs/ai/rules/core/OKF.md"] = "hand-written\n" });

        var collisions = OwnedSet.CaseCollisions(fs, Layout, ["docs/ai/rules/core/okf.md"]);

        var collision = Assert.Single(collisions);
        Assert.Contains("docs/ai/rules/core/OKF.md", collision, StringComparison.Ordinal);
        Assert.Contains("docs/ai/rules/core/okf.md", collision, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_the_owned_path_itself_on_disk_When_collisions_are_read_Then_a_file_is_not_a_collision_with_itself()
    {
        var fs = Repo(new() { ["docs/ai/rules/core/okf.md"] = "delivered\n" });

        Assert.Empty(OwnedSet.CaseCollisions(fs, Layout, ["docs/ai/rules/core/okf.md"]));
    }

    [Fact]
    public void Given_a_differently_named_neighbour_When_collisions_are_read_Then_it_is_not_a_collision()
    {
        var fs = Repo(new() { ["docs/ai/rules/core/okf-notes.md"] = "notes\n" });

        Assert.Empty(OwnedSet.CaseCollisions(fs, Layout, ["docs/ai/rules/core/okf.md"]));
    }
}
