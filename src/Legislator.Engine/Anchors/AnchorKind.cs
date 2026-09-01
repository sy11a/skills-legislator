namespace Legislator.Engine.Anchors;

/// <summary>What a backticked token is, under the closed definition in <c>core/okf.md</c> (C-07).</summary>
public enum AnchorKind
{
    /// <summary>Prose - a command, a field name, a template. The engine asks nothing of it.</summary>
    None,

    /// <summary>A repository path: it must exist.</summary>
    Path,

    /// <summary>An identifier: its leading segment must occur under the source roots.</summary>
    Symbol,
}
