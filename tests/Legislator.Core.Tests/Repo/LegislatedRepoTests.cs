using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Xunit;

namespace Legislator.Core.Tests.Repo;

/// <summary>
/// The "is this a legislated repository?" test, in one place. Three hooks ask it - the owned-file
/// guard from the edited file, the conduct guard from the payload's cwd, the OKF reminder from
/// the git toplevel - so it is a seam rather than three walks up three trees (the second-consumer
/// rule). The manifest's name comes from the options model, never from a literal here.
/// </summary>
public sealed class LegislatedRepoTests
{
    private static readonly LegislatorOptions Options = new();

    private static MockFileSystem WithManifest(string at)
    {
        var fs = new MockFileSystem();
        fs.AddFile($"{at}/docs/ai/manifest.json", new MockFileData("{}"));
        return fs;
    }

    [Fact]
    public void The_directory_holding_the_manifest_is_the_root()
    {
        Assert.Equal("/r", LegislatedRepo.Find(WithManifest("/r"), Options, "/r"));
    }

    [Fact]
    public void A_directory_far_below_the_manifest_still_finds_it()
    {
        Assert.Equal("/r", LegislatedRepo.Find(WithManifest("/r"), Options, "/r/docs/ai/rules/stacks/dotnet"));
    }

    [Fact]
    public void No_manifest_anywhere_above_is_no_repository()
    {
        Assert.Null(LegislatedRepo.Find(new MockFileSystem(), Options, "/plain/deep"));
    }

    /// <summary>The nearest manifest wins: a legislated repository nested inside another is its own.</summary>
    [Fact]
    public void The_nearest_manifest_wins()
    {
        var fs = WithManifest("/outer");
        fs.AddFile("/outer/inner/docs/ai/manifest.json", new MockFileData("{}"));

        Assert.Equal("/outer/inner", LegislatedRepo.Find(fs, Options, "/outer/inner/src"));
    }

    /// <summary>A manifest that is a directory is not a manifest - the Python asks `is_file`.</summary>
    [Fact]
    public void A_directory_named_like_the_manifest_is_not_one()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/ai/manifest.json");

        Assert.Null(LegislatedRepo.Find(fs, Options, "/r"));
    }
}
