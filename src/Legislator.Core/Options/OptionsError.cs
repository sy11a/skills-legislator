namespace Legislator.Core.Options;

/// <summary>One configuration fault: the layer it was read from, the key (empty when the whole document is at fault) and the reason (R-8211, C-04).</summary>
public sealed record OptionsError(OptionsLayer Layer, string Key, string Reason);

/// <summary>Every configuration fault of a composition at once, first layer first - thrown before any work is done (R-8211, C-04).</summary>
public sealed class OptionsException(IReadOnlyList<OptionsError> errors) : Exception(Describe(errors))
{
    public IReadOnlyList<OptionsError> Errors { get; } = errors;

    private static string Describe(IReadOnlyList<OptionsError> errors) =>
        string.Join('\n', errors.Select(e => $"{e.Layer}: {e.Key} - {e.Reason}"));
}
