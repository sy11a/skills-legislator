namespace Legislator.Engine.Runs;

/// <summary>
/// The named failure when a package's <c>TemplateCorePrefix</c> or <c>TemplateCodebaseMapPath</c>
/// option is misconfigured for the helper that folds a template line's path back to an owned
/// path (BL-484 N2). An empty or trailing-slash-missing core prefix would let the loop walk off
/// the end of the line; a prefix without the trailing <c>/</c> would build owned paths with a
/// doubled <c>//</c> that no longer match. The report cannot derive a core rule's tier from a
/// template whose own defaults miss the project rule, so it says so loudly rather than silently
/// proposing the wrong wiring.
/// </summary>
public sealed class TemplateLayoutInvalidException : Exception
{
    public TemplateLayoutInvalidException(string key, string value, string reason)
        : base($"the skill package's template layout option `{key}` (currently `{value}`) is invalid: "
            + $"{reason} - restore the option to its default or point --skill at a package that ships one")
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(reason);
        Key = key;
        Value = value;
        Reason = reason;
    }

    public string Key { get; }

    public string Value { get; }

    public string Reason { get; }
}