using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Engine.Anchors;
using Xunit;

namespace Legislator.Engine.Tests.Anchors;

/// <summary>
/// A symbol-anchor resolves when its leading segment occurs literally under the repository's
/// source roots. Which directories those are — and which are excluded, at any depth — is the
/// half of `core/okf.md` that decides whether the check finds real rot or is fooled by a
/// stale build artifact.
/// </summary>
public sealed class SymbolIndexTests
{
    private static MockFileSystem Fs(params (string Path, string Text)[] files)
    {
        var fs = new MockFileSystem();
        foreach (var (path, text) in files)
        {
            fs.AddFile(path, new MockFileData(text));
        }

        return fs;
    }

    private static SymbolIndex Build(IFileSystem fs, LegislatorOptions? options = null) =>
        SymbolIndex.Build(fs, "/r", options ?? new LegislatorOptions());

    [Fact]
    public void A_symbol_written_in_source_resolves()
    {
        var index = Build(Fs(("/r/src/App/WidgetStore.cs", "public class WidgetStore { }")));

        Assert.True(index.Contains("WidgetStore"));
        Assert.False(index.Contains("LegacyProcessor"));
    }

    /// <summary>The knowledge layer must not resolve its own anchors: `docs/` is never a source root.</summary>
    [Fact]
    public void Docs_is_not_a_source_root()
    {
        var index = Build(Fs(("/r/docs/okf/other.md", "The class `OnlyInDocs` is described here."),
                             ("/r/src/App/WidgetStore.cs", "public class WidgetStore { }")));

        Assert.False(index.Contains("OnlyInDocs"));
    }

    /// <summary>
    /// A stale `src/App/obj/Debug/App.js` still carrying a deleted symbol would resolve it and
    /// the check would silently miss the rot it exists to find — differently on a CI clone and
    /// a developer's. Build output is excluded at ANY depth, not just at the top.
    /// </summary>
    [Fact]
    public void Build_output_is_excluded_at_any_depth()
    {
        var index = Build(Fs(("/r/src/App/obj/Debug/App.js", "var PaymentProcessor = 1;"),
                             ("/r/src/App/WidgetStore.cs", "public class WidgetStore { }")));

        Assert.False(index.Contains("PaymentProcessor"));
    }

    [Fact]
    public void Hidden_directories_are_not_scanned()
    {
        var index = Build(Fs(("/r/.git/objects/blob", "class HiddenThing { }"),
                             ("/r/src/App/WidgetStore.cs", "public class WidgetStore { }")));

        Assert.False(index.Contains("HiddenThing"));
    }

    /// <summary>A file past the size ceiling is not prose or source; the ceiling is an option, never a literal (R-8209).</summary>
    [Fact]
    public void A_file_past_the_size_ceiling_is_skipped()
    {
        var options = new LegislatorOptions { MaxFileBytes = new(8, OptionsLayer.Defaults) };

        var index = Build(Fs(("/r/src/App/Big.cs", "class EnormousThing { }")), options);

        Assert.False(index.Contains("EnormousThing"));
    }

    /// <summary>The roots are named in the finding text, sorted, each with its trailing slash.</summary>
    [Fact]
    public void Source_roots_are_reported_sorted_for_the_finding_text()
    {
        var index = Build(Fs(("/r/src/App/A.cs", ""), ("/r/evals/y.py", ""), ("/r/docs/okf/a.md", "")));

        Assert.Equal(["evals", "src"], index.SourceRoots);
    }
}
