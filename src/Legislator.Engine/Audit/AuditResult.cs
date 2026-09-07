namespace Legislator.Engine.Audit;

/// <summary>
/// What one audit found, and which checks it heard from. The dirty set is not derivable from
/// the findings alone once the model's half arrives: a check that raised nothing is clean, and
/// the report says so by name, which is how a reader tells "checked and clean" from "not run".
/// </summary>
public sealed class AuditResult
{
    private readonly List<AuditFinding> findings = [];

    private readonly HashSet<string> dirty = new(StringComparer.Ordinal);

    public IReadOnlyList<AuditFinding> Findings => findings;

    public IReadOnlySet<string> Dirty => dirty;

    public void Add(AuditFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        findings.Add(finding);
        dirty.Add(finding.Slug);
    }

    public void AddRange(IEnumerable<AuditFinding> found)
    {
        ArgumentNullException.ThrowIfNull(found);

        foreach (var finding in found)
        {
            Add(finding);
        }
    }

    /// <summary>Drops the engine's own finding a model finding supersedes: the model saw the same file and judged it harder, so keeping both would report one fact twice.</summary>
    public void Escalate(string slug, string needle)
    {
        findings.RemoveAll(f => f.Slug == slug && f.Text.Contains(needle, StringComparison.Ordinal));
    }
}
