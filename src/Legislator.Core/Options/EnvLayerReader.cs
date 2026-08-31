using Legislator.Core.Abstractions;

namespace Legislator.Core.Options;

/// <summary>Reads the environment layer: <c>LEGISLATOR_DOCS_DIR</c> → <c>docs_dir</c> for every known key that is set. Only known keys are asked for - <see cref="IEnvironment"/> cannot enumerate, so a stray <c>LEGISLATOR_*</c> variable is not seen (C-04).</summary>
public static class EnvLayerReader
{
    public const string Prefix = "LEGISLATOR_";

    public static IReadOnlyDictionary<string, string> Read(IEnvironment env, IEnumerable<string> knownKeys)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in knownKeys)
        {
            if (env.GetVariable(Prefix + key.ToUpperInvariant()) is { } value)
            {
                values[key] = value;
            }
        }

        return values;
    }
}
