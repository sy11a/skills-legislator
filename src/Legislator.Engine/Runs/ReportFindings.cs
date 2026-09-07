using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Legislator.Engine.Runs;

/// <summary>
/// The model's half of the Step-7 report: the constitution candidates it found and the review
/// lines it wants added. A narrower channel than the audit's - two lists of sentences, no
/// severities and no check slugs, because the report's sections are already chosen and the
/// model is filling them rather than raising findings. Malformed input refuses loudly for the
/// audit channel's reason: silence and failure must not produce the same report.
/// </summary>
public sealed record ReportFindings(IReadOnlyList<string> Candidates, IReadOnlyList<string> Review)
{
    private const string CandidatesKey = "candidates";

    private const string ReviewKey = "review";

    public static ReportFindings Empty { get; } = new([], []);

    public static ReportFindings Load(IFileSystem fs, string path)
    {
        ArgumentNullException.ThrowIfNull(fs);

        JsonNode? data;
        try
        {
            data = JsonNode.Parse(fs.File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"model-findings file unreadable or malformed ({path}): {ex.Message}", ex);
        }

        if (data is not JsonObject obj || !TryLines(obj, CandidatesKey, out var candidates) || !TryLines(obj, ReviewKey, out var review))
        {
            throw new InvalidOperationException(
                $"model-findings file has the wrong shape ({path}): expected {{candidates: [str], review: [str]}}");
        }

        return new ReportFindings(candidates, review);
    }

    /// <summary>An absent key is an empty list; a key of the wrong shape is a refusal, never an empty list - the two say different things about what the model did.</summary>
    private static bool TryLines(JsonObject obj, string key, out IReadOnlyList<string> lines)
    {
        lines = [];
        if (!obj.TryGetPropertyValue(key, out var value) || value is null)
        {
            return true;
        }

        if (value is not JsonArray array)
        {
            return false;
        }

        var read = new List<string>();
        foreach (var entry in array)
        {
            if (entry is not JsonValue item || item.GetValueKind() != JsonValueKind.String)
            {
                return false;
            }

            read.Add(item.GetValue<string>());
        }

        lines = read;
        return true;
    }
}
