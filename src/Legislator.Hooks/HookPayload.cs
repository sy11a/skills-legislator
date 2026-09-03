using System.Text.Json;
using System.Text.Json.Nodes;

namespace Legislator.Hooks;

/// <summary>
/// Claude Code's hook payload, read as DATA rather than as a type (the `ManifestFile`
/// precedent). Every hook's contract is to decide on a surprising shape, not to trip over it:
/// a `tool_input` that arrives as a string is an allow, and a typed model would make it an
/// exception instead. So the JSON is a node, every accessor answers null on anything it did
/// not expect, and an unparseable payload is simply one that is not <see cref="Usable"/>.
/// </summary>
public sealed class HookPayload
{
    /// <summary>The field names Claude Code writes - consts because the payload is a contract with the editor, not a shape this system chose.</summary>
    private const string ToolNameField = "tool_name";
    private const string ToolInputField = "tool_input";
    private const string CwdField = "cwd";
    private const string StopHookActiveField = "stop_hook_active";
    private const string FilePathField = "file_path";
    private const string NotebookPathField = "notebook_path";
    private const string CommandField = "command";

    private readonly JsonObject? root;

    private HookPayload(JsonObject? root) => this.root = root;

    /// <summary>Whether the payload parsed AND is a JSON object; anything else - empty stdin, a fragment, an array - is not a payload at all.</summary>
    public bool Usable => root is not null;

    public string? ToolName => Text(root, ToolNameField);

    public string? Cwd => Text(root, CwdField);

    /// <summary>The Bash tool's command line, absent for every other tool.</summary>
    public string? Command => Text(ToolInput, CommandField);

    /// <summary>The edited file, under either key the editing tools use - a guard reading only one of them guards nothing the day the other tool fires.</summary>
    public string? FilePath => Text(ToolInput, FilePathField) ?? Text(ToolInput, NotebookPathField);

    /// <summary>Set by Claude Code when a Stop hook already fired for this stop; anything but a true boolean reads as false, so a surprising value cannot suppress a reminder.</summary>
    public bool StopHookActive =>
        root?[StopHookActiveField] is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    private JsonObject? ToolInput => root?[ToolInputField] as JsonObject;

    /// <summary>Reads whatever stdin carried; anything that is not a JSON object yields an unusable payload rather than a fault.</summary>
    public static HookPayload Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HookPayload(null);
        }

        try
        {
            return new HookPayload(JsonNode.Parse(raw) as JsonObject);
        }
        catch (JsonException)
        {
            return new HookPayload(null);
        }
    }

    /// <summary>A string field, or null where it is absent or of another type - the `isinstance` check the Python makes before it trusts a value.</summary>
    private static string? Text(JsonObject? node, string field) =>
        node?[field] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
