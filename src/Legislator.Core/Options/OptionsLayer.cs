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
