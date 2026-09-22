namespace Legislator.Engine.Apply;

/// <summary>
/// Files an earlier edition delivered and the constitution no longer does.
/// </summary>
/// <remarks>
/// The list exists for exactly one reader: the reconstruction that runs when a repository has no
/// manifest to read its old owned set from. Everywhere else the set of retiring paths is the
/// difference between two manifests and needs no literal at all — the sweep that rewrites
/// declarations keys on that difference, so it covers a future retirement without being told.
/// Here there is no old manifest, and a reconstruction that listed only what the current edition
/// still delivers would call a retired file unowned: nothing to delete, nothing to rewrite, and a
/// repository left carrying an engine from an edition whose law it no longer has.
/// </remarks>
public static class RetiredDeliveries
{
    /// <summary>The Python engine, delivered up to edition 25 and retired in 26; path relative to the AI directory.</summary>
    public const string Engine = "engine.py";
}
