using Legislator.Core.Options;
using Legislator.Core.Repo;
using Xunit;

namespace Legislator.Core.Tests.Repo;

/// <summary>
/// The layout is the only place a job learns where anything lives (C-07): every directory
/// comes from the options model, and the segments are joined with '/' because the paths are
/// compared against document text and printed into findings, not walked on a real disk.
/// </summary>
public sealed class RepoLayoutTests
{
    [Fact]
    public void Defaults_compose_the_constitutional_shape()
    {
        var layout = new RepoLayout(new LegislatorOptions(), "/r");

        Assert.Equal("/r/docs", layout.Docs);
        Assert.Equal("/r/docs/ai", layout.Ai);
        Assert.Equal("/r/docs/ai/rules", layout.Rules);
        Assert.Equal("/r/docs/ai/manifest.json", layout.Manifest);
        Assert.Equal("/r/docs/okf", layout.Okf);
        Assert.Equal("/r/docs/cases", layout.Cases);
    }

    [Fact]
    public void A_renamed_docs_directory_moves_everything_under_it()
    {
        var options = new LegislatorOptions
        {
            DocsDir = new("knowledge", OptionsLayer.Instance),
        };

        var layout = new RepoLayout(options, "/r");

        Assert.Equal("/r/knowledge", layout.Docs);
        Assert.Equal("/r/knowledge/okf", layout.Okf);
        Assert.Equal("/r/knowledge/ai/rules", layout.Rules);
    }

    [Fact]
    public void Segments_are_joined_with_a_forward_slash_on_every_host()
    {
        var layout = new RepoLayout(new LegislatorOptions(), "/r");

        Assert.DoesNotContain('\\', layout.Rules);
    }
}
