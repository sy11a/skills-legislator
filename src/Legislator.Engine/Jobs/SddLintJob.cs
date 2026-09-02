using System.Text.RegularExpressions;
using Legislator.Core.Repo;
using Legislator.Engine.Sdd;
using Legislator.Engine.Text;

namespace Legislator.Engine.Jobs;

/// <summary>
/// The analyze gate's mechanical passes (`core/sdd.md`), read-only: coverage of requirement to
/// task, references that resolve to no definition, placeholders nobody replaced, and the case,
/// ADR, journal, changelog and OKF shapes. Scope is the cases directory alone - retired history
/// never enters a lint pass, and a converged case is history too: its going out of date is the
/// design, so the gate serves work in flight, not the record.
/// </summary>
public sealed class SddLintJob : IJob
{
    public string Name => "sdd-lint";

    public string Usage => Name;

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var fs = ctx.Fs;
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var cases = CaseModel.Load(fs, layout);
        var findings = new List<string>();

        // Dangling is judged against EVERY case's definitions, not just the referencing case's
        // own: a case may lawfully trace a requirement of a sibling riding the same edition.
        // Coverage stays per-case, ids being unique within a case only.
        var defined = cases.SelectMany(c => c.Requirements.Keys).ToHashSet(StringComparer.Ordinal);
        var specHome = $"{layout.Cases[(ctx.Root.Length + 1)..]}/*/spec.md";

        foreach (var file in cases.Where(c => !c.Converged))
        {
            foreach (var document in CaseModel.Documents(fs, file))
            {
                var relative = document[(ctx.Root.Length + 1)..];
                var prose = Prose.ProseOnly(fs.File.ReadAllText(document));
                foreach (var id in Prose.PerRefs(prose).Order(StringComparer.Ordinal))
                {
                    if (!defined.Contains(id))
                    {
                        findings.Add($"{relative}: dangling: per {id} resolves to no EARS definition in any {specHome}");
                    }
                }

                foreach (var placeholder in Prose.Placeholder().Matches(prose).Cast<Match>())
                {
                    findings.Add($"{relative}: unresolved-placeholder: {placeholder.Value}");
                }
            }

            // Coverage applies only where the case declares a plan: tier 0/1 is lawful, so a
            // spec-only case yields no coverage noise.
            var plan = $"{file.Path}/plan.md";
            if (fs.File.Exists(plan))
            {
                var traced = Prose.PerRefs(Prose.ProseOnly(fs.File.ReadAllText(plan)));
                foreach (var id in file.Requirements.Keys.Where(id => !traced.Contains(id)).Order(StringComparer.Ordinal))
                {
                    findings.Add($"{file.Relative}/plan.md: uncovered: {id} has no per-{id} task");
                }
            }

            findings.AddRange(EarsLint.Findings(fs, file));
        }

        findings.AddRange(AdrLint.Findings(fs, layout));
        findings.AddRange(JournalLint.Findings(fs, layout));
        findings.AddRange(ChangelogLint.Findings(fs, layout));
        findings.AddRange(OkfFrontMatterLint.Findings(fs, layout, ctx.Options.HumanClassDocs.Value));

        return Findings.AsResult(findings);
    }
}
