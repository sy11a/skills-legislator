namespace Legislator.Core.Options;

/// <summary>The configuration layer an option value came from; later layers override earlier ones (C-04).</summary>
public enum OptionsLayer
{
    Defaults,
    Machine,
    Instance,
    Environment,
}

/// <summary>An option value together with the layer that produced it - provenance travels with the value (C-03).</summary>
public sealed record OptionValue<T>(T Value, OptionsLayer Source);

/// <summary>The single birthplace of a layer's written form: every error line, every provenance stamp and every JSON source field renders through <see cref="Keyword"/>, so the four words never drift apart (C-04, C-05).</summary>
public static class OptionsLayerExtensions
{
    /// <summary>The layer as it is written to a user - the enum's own name, lowercased; a layer added to the enum is spelled by that act alone.</summary>
    public static string Keyword(this OptionsLayer layer) => layer.ToString().ToLowerInvariant();
}
