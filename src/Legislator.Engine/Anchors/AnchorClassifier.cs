using System.Text.RegularExpressions;

namespace Legislator.Engine.Anchors;

/// <summary>
/// The closed definition of an anchor (C-07), transliterated from <c>core/okf.md</c> and the
/// Python <c>classify</c> that already executes it. The rules here are law, not configuration
/// (R-8213): what a PascalCase identifier looks like is a sentence of the constitution, and an
/// installation that could reconfigure it would be reading a different constitution.
/// </summary>
public static partial class AnchorClassifier
{
    /// <summary>A token carrying any of these is a template, a glob or a phrase - never an anchor.</summary>
    private static readonly char[] Forbidden = [' ', '<', '>', '*', '?'];

    /// <summary>Trailing prose punctuation: a sentence may end on the anchor.</summary>
    private static readonly char[] ProsePunctuation = [':', ',', '.'];

    /// <summary>
    /// PascalCase of at least four characters, optionally dotted - and every dotted segment is
    /// PascalCase too. Source-generated because a <c>new Regex(...)</c> falls back to the
    /// interpreter under NativeAOT (plan amendment, stage 4 audit).
    /// </summary>
    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]{3,}(\.[A-Z][A-Za-z0-9]+)*$")]
    private static partial Regex Pascal();

    public static AnchorKind Classify(string token, IReadOnlySet<string> topLevelDirs)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(topLevelDirs);

        if (token.IndexOfAny(Forbidden) >= 0 || token.StartsWith('~') || token.StartsWith('/'))
        {
            return AnchorKind.None;
        }

        var slash = token.IndexOf('/', StringComparison.Ordinal);
        if (slash >= 0)
        {
            return topLevelDirs.Contains(token[..slash]) ? AnchorKind.Path : AnchorKind.None;
        }

        return Pascal().IsMatch(token) ? AnchorKind.Symbol : AnchorKind.None;
    }

    /// <summary>The path a path-anchor names, with the sentence's punctuation dropped.</summary>
    public static string PathTarget(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return token.TrimEnd(ProsePunctuation);
    }

    /// <summary>
    /// For <c>Type.Member()</c>, the part that may be a file on disk - null when the token
    /// names no member. Which extension that stem wears is a question only a file system can
    /// answer, so the probing belongs to the job and not here (C-07, amended at T-07).
    /// </summary>
    public static string? MemberStem(string token)
    {
        var target = PathTarget(token);
        if (!target.EndsWith("()", StringComparison.Ordinal))
        {
            return null;
        }

        var withoutCall = target[..^2];
        var dot = withoutCall.LastIndexOf('.');
        return dot < 0 ? withoutCall : withoutCall[..dot];
    }
}
