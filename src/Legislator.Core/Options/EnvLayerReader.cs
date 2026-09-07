using Legislator.Core.Abstractions;

namespace Legislator.Core.Options;

/// <summary>Reads the environment layer: every variable named <c>LEGISLATOR_*</c> becomes the key that is its lowercased remainder - <c>LEGISLATOR_DOCS_DIR</c> → <c>docs_dir</c>. A stray name is admitted as the key it spells so the validator can refuse it by name, which is what makes a typo loud rather than invisible (R-8211, C-04).</summary>
public static class EnvLayerReader
{
    public const string Prefix = "LEGISLATOR_";

    public static IReadOnlyDictionary<string, string> Read(IEnvironment env)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in env.VariableNames)
        {
            if (name.StartsWith(Prefix, StringComparison.Ordinal) && env.GetVariable(name) is { } value)
            {
                values[name[Prefix.Length..].ToLowerInvariant()] = value;
            }
        }

        return values;
    }
}
