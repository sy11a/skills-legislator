using Legislator.Engine.Anchors;
using Xunit;

namespace Legislator.Engine.Tests.Anchors;

/// <summary>
/// The closed definition of an anchor, transliterated from `core/okf.md` and the Python
/// `classify` it already executes. Every row here is a sentence of that law: what the
/// backticked token must look like before the engine is allowed to ask whether it resolves.
/// </summary>
public sealed class AnchorClassifierTests
{
    private static readonly HashSet<string> Top = ["docs", "evals", "src"];

    [Theory]
    // A path-anchor: contains '/', first segment is a top-level directory of the repository.
    [InlineData("src/App/WidgetStore.cs", AnchorKind.Path)]
    [InlineData("docs/okf/index.md", AnchorKind.Path)]
    [InlineData("src", AnchorKind.None)]                        // no '/', and lowercase
    [InlineData("nope/x.cs", AnchorKind.None)]                  // first segment is not top-level
    // A symbol-anchor: PascalCase, four characters or more, optionally dotted.
    [InlineData("WidgetStore", AnchorKind.Symbol)]
    [InlineData("WidgetStore.Flush", AnchorKind.Symbol)]
    [InlineData("Api", AnchorKind.None)]                        // three characters
    [InlineData("Apis", AnchorKind.Symbol)]                     // four is the floor
    [InlineData("contenthash", AnchorKind.None)]                // lowercase is not a symbol
    [InlineData("WidgetStore.flush", AnchorKind.None)]          // a dotted segment is PascalCase too
    [InlineData("Widget_Store", AnchorKind.None)]               // an underscore is not PascalCase
    // Excluded outright by the forbidden set and the two prefixes.
    [InlineData("schemas/<type>/<version>.json", AnchorKind.None)]
    [InlineData("src/**/*.cs", AnchorKind.None)]
    [InlineData("src/App?.cs", AnchorKind.None)]
    [InlineData("dotnet build", AnchorKind.None)]               // a space: a command, not an anchor
    [InlineData("~/.config/app/settings.yaml", AnchorKind.None)]
    [InlineData("/etc/hosts", AnchorKind.None)]
    public void Classify_follows_the_closed_definition(string token, AnchorKind expected) =>
        Assert.Equal(expected, AnchorClassifier.Classify(token, Top));

    /// <summary>
    /// A path-anchor may end in prose punctuation; the file test is made against the token
    /// without it. This is the Python's `token.rstrip(":,.")`, nothing more.
    /// </summary>
    [Theory]
    [InlineData("src/App/WidgetStore.cs", "src/App/WidgetStore.cs")]
    [InlineData("src/App/WidgetStore.cs.", "src/App/WidgetStore.cs")]
    [InlineData("src/App/WidgetStore.cs,", "src/App/WidgetStore.cs")]
    [InlineData("src/App/WidgetStore.Flush()", "src/App/WidgetStore.Flush()")]
    public void PathTarget_drops_trailing_prose_punctuation(string token, string expected) =>
        Assert.Equal(expected, AnchorClassifier.PathTarget(token));

    /// <summary>
    /// `Type.Member()` names a file plus a member: the stem is what may exist on disk, and
    /// which extension it wears is a question only the file system can answer — so the
    /// classifier hands back the stem and the job does the probing (C-07, amended at T-07).
    /// </summary>
    [Theory]
    [InlineData("src/App/WidgetStore.Flush()", "src/App/WidgetStore")]
    [InlineData("src/A.cs.Run()", "src/A.cs")]
    [InlineData("src/App/WidgetStore.cs", null)]
    [InlineData("WidgetStore.Flush()", "WidgetStore")]
    public void MemberStem_is_the_part_before_the_member(string token, string? expected) =>
        Assert.Equal(expected, AnchorClassifier.MemberStem(token));
}
