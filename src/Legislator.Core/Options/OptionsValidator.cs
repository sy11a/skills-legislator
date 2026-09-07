using System.Globalization;

namespace Legislator.Core.Options;

/// <summary>Judges one raw layer against the options model: every key known, integer keys an integer of 1 or more, every other value non-empty and free of a <c>..</c> segment (R-8211, C-04).</summary>
public static class OptionsValidator
{
    private const string ParentSegment = "..";

    private static readonly char[] SegmentSeparators = ['/', '\\', LegislatorOptions.ListSeparator];

    public static IReadOnlyList<OptionsError> Validate(OptionsLayer layer, IReadOnlyDictionary<string, string> raw)
    {
        var errors = new List<OptionsError>();
        foreach (var (key, value) in raw)
        {
            if (Judge(key, value) is { } reason)
            {
                errors.Add(new(layer, key, reason));
            }
        }

        return errors;
    }

    private static string? Judge(string key, string value)
    {
        if (!LegislatorOptions.KeyMap.ContainsKey(key))
        {
            return "unknown key";
        }

        if (LegislatorOptions.IntegerKeys.Contains(key))
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= 1
                ? null
                : "must be an integer of 1 or more";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return "must not be empty";
        }

        return value.Split(SegmentSeparators).Contains(ParentSegment, StringComparer.Ordinal)
            ? $"a '{ParentSegment}' path segment is not allowed"
            : null;
    }
}
