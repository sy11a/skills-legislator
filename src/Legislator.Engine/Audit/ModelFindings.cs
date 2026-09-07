using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Legislator.Engine.Audit;

/// <summary>One judgement the model made: the section it belongs in, the check it answers, the line a reader acts on, and optionally the engine finding it supersedes.</summary>
public sealed record ModelFinding(Severity Severity, string Slug, string Text, string? Escalates);

/// <summary>
/// The channel the model's half of the audit arrives through - checks 11 and 12, the check-9
/// escalations and the constitution candidates. It is validated strictly and refuses loudly,
/// because a channel that read as empty when malformed would make the model's silence and the
/// model's failure produce the same report.
/// </summary>
public sealed record ModelFindings(
    IReadOnlyList<ModelFinding> Findings, IReadOnlyList<string> Candidates, string? Verification)
{
    private const string FindingsKey = "findings";

    private const string CandidatesKey = "candidates";

    private const string VerificationKey = "verification";

    private const string CheckField = "check";

    private const string SeverityField = "severity";

    private const string LineField = "line";

    private const string EscalatesField = "escalates";

    public static ModelFindings Load(IFileSystem fs, string path)
    {
        ArgumentNullException.ThrowIfNull(fs);

        JsonNode? data;
        try
        {
            data = JsonNode.Parse(fs.File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"model-findings file unreadable or malformed ({path}): {ex.Message}", ex);
        }

        if (data is not JsonObject document || Array(document, FindingsKey) is null || Array(document, CandidatesKey) is null)
        {
            throw new InvalidDataException(
                $"model-findings file has the wrong shape ({path}): expected {{findings: [...], candidates: [...]}}");
        }

        var findings = new List<ModelFinding>();
        foreach (var entry in Array(document, FindingsKey)!)
        {
            findings.Add(One(entry, path));
        }

        var candidates = Array(document, CandidatesKey)!
            .Select(c => c?.ToString() ?? "").ToList();

        return new ModelFindings(findings, candidates, Text(document, VerificationKey));
    }

    private static ModelFinding One(JsonNode? entry, string path)
    {
        var slug = entry is JsonObject o ? Text(o, CheckField) : null;
        var severity = entry is JsonObject s ? Text(s, SeverityField) : null;
        var line = entry is JsonObject l ? Text(l, LineField) : null;
        if (slug is null or "" || line is null || !Enum.TryParse<Severity>(severity, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new InvalidDataException($"model-findings entry malformed ({path}): {entry?.ToJsonString() ?? "null"}");
        }

        return new ModelFinding(parsed, slug, Strip(line, slug), Text((JsonObject)entry!, EscalatesField));
    }

    /// <summary>The line as the report will print it: the model may hand over a finished bullet, and neither the list marker nor the slug is printed twice.</summary>
    private static string Strip(string line, string slug)
    {
        var text = line.Trim();
        if (text.StartsWith("- ", StringComparison.Ordinal))
        {
            text = text[2..];
        }

        var prefix = $"[{slug}] ";
        return text.StartsWith(prefix, StringComparison.Ordinal) ? text[prefix.Length..] : text;
    }

    private static JsonArray? Array(JsonObject document, string key) =>
        !document.TryGetPropertyValue(key, out var value) ? [] : value as JsonArray;

    private static string? Text(JsonObject document, string key) =>
        document.TryGetPropertyValue(key, out var value) && value is JsonValue v
            && v.GetValueKind() == JsonValueKind.String
            ? v.ToString()
            : null;
}
