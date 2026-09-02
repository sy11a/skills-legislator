using System.Text;

namespace Legislator.Engine.Audit;

/// <summary>
/// The pinned report (R-663, R-669). It is a document, not a log: the model reads it back, the
/// user pastes it into a review, and two runs over an unchanged repository print the same bytes.
/// So nothing here is ordered by when it was found - findings sort by the check's pinned place
/// and then by their own text, and the clean-checks line names every check that stayed silent,
/// which is how a reader tells "checked and clean" from "never ran".
/// </summary>
public static class AuditReport
{
    private const string ModelNote =
        "Model checks (project-rules, stray-rulebooks, constitution candidates): not supplied - this print is the mechanical half only.";

    public static string Render(
        AuditResult result, string repository, DateOnly today, string repoVersion, string skillVersion,
        ModelFindings? model, string engineName)
    {
        ArgumentNullException.ThrowIfNull(result);

        var state = repoVersion == skillVersion ? "up to date" : "behind";
        var report = new StringBuilder();
        report.Append($"# AI-Layer Audit — {repository}, {today:yyyy-MM-dd}\n\n");
        report.Append($"Constitution: v{repoVersion} (skill source: v{skillVersion}) — {state}\n\n");

        if (result.Findings.Count == 0)
        {
            report.Append("No findings.\n\n");
        }
        else
        {
            foreach (var severity in Enum.GetValues<Severity>())
            {
                var section = result.Findings.Where(f => f.Severity == severity)
                    .OrderBy(f => AuditChecks.Place(f.Slug)).ThenBy(f => f.Text, StringComparer.Ordinal).ToList();
                if (section.Count == 0)
                {
                    continue;
                }

                report.Append($"## {severity}\n");
                foreach (var finding in section)
                {
                    report.Append($"- [{finding.Slug}] {finding.Text}\n");
                }

                report.Append('\n');
            }
        }

        if (model is { Candidates.Count: > 0 })
        {
            report.Append("## Constitution candidates\n");
            foreach (var candidate in model.Candidates)
            {
                report.Append($"{candidate}\n");
            }

            report.Append('\n');
        }

        report.Append($"Clean checks: {CleanLine(result, model)}\n");
        if (model is null)
        {
            report.Append($"{ModelNote}\n");
        }

        if (model?.Verification is { } verification)
        {
            report.Append($"\n{verification}\n");
        }

        report.Append($"\nEmitted by {engineName} audit — constitution v{skillVersion}.\n");
        return report.ToString();
    }

    /// <summary>The checks that stayed silent, in the pinned order. The model's two join the list only when the model actually reported - unsupplied is not clean.</summary>
    private static string CleanLine(AuditResult result, ModelFindings? model)
    {
        var clean = AuditChecks.Order
            .Where(slug => !result.Dirty.Contains(slug))
            .Where(slug => model is not null || !AuditChecks.ModelChecks.Contains(slug))
            .ToList();

        return clean.Count > 0 ? string.Join(", ", clean) : "none";
    }
}
