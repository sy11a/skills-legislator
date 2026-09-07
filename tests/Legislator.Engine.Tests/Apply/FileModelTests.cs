using Legislator.Engine.Apply;
using Legislator.TestSupport;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Apply;

/// <summary>
/// The v14 file model's branches. The parity twins cover the two the ruler builds a repository
/// for - a tracked rename and a bare canonical document; these are the three it does not: an
/// alias pointing at the wrong target, a rename git will not perform, and a repository already
/// in the model, where the right answer is to do nothing at all.
/// </summary>
public sealed class FileModelTests
{
    [Fact]
    public void Given_an_alias_pointing_somewhere_else_When_the_model_is_wired_Then_it_is_relinked_to_the_canonical_document()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n", ["elsewhere.md"] = "# E\n" });
        fs.File.CreateSymbolicLink($"{Root}/CLAUDE.md", $"{Root}/elsewhere.md");

        var events = FileModel.Wire(fs, NoRepo(), Options, Layout);

        Assert.Contains(events, e => e.StartsWith("relinked", StringComparison.Ordinal));
        Assert.Equal("AGENTS.md", fs.FileInfo.New($"{Root}/CLAUDE.md").LinkTarget);
    }

    [Fact]
    public void Given_git_refuses_the_move_When_the_model_is_wired_Then_the_file_is_renamed_anyway_and_the_event_still_says_renamed()
    {
        var fs = Repo(new() { ["CLAUDE.md"] = "# Real\n" });

        var events = FileModel.Wire(fs, NoRepo(), Options, Layout);

        Assert.Contains("renamed CLAUDE.md -> AGENTS.md", events);
        Assert.Equal("# Real\n", fs.File.ReadAllText($"{Root}/AGENTS.md"));
        Assert.Equal("AGENTS.md", fs.FileInfo.New($"{Root}/CLAUDE.md").LinkTarget);
    }

    [Fact]
    public void Given_git_is_absent_altogether_When_the_model_is_wired_Then_the_rename_still_happens()
    {
        var fs = Repo(new() { ["CLAUDE.md"] = "# Real\n" });
        var noGit = new FakeProcessRunner
        {
            OnRun = (file, _, _) => throw Legislator.Core.Abstractions.ProcessStartException.For(
                file, new InvalidOperationException("no such file")),
        };

        var events = FileModel.Wire(fs, noGit, Options, Layout);

        Assert.Contains("renamed CLAUDE.md -> AGENTS.md", events);
        Assert.True(fs.File.Exists($"{Root}/AGENTS.md"));
    }

    [Fact]
    public void Given_a_repository_already_in_the_model_When_it_is_wired_Then_nothing_is_reported_and_nothing_moves()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });
        // Relative, as the model writes it: an absolute link is a link that breaks when the
        // repository moves, and the model does not recognise it as its own.
        fs.File.CreateSymbolicLink($"{Root}/CLAUDE.md", "AGENTS.md");

        Assert.Empty(FileModel.Wire(fs, NoRepo(), Options, Layout));
    }

    [Fact]
    public void Given_neither_document_exists_When_the_model_is_wired_Then_nothing_is_created()
    {
        var fs = Repo();

        Assert.Empty(FileModel.Wire(fs, NoRepo(), Options, Layout));
        Assert.False(fs.File.Exists($"{Root}/CLAUDE.md"));
        Assert.False(fs.File.Exists($"{Root}/AGENTS.md"));
    }
}
