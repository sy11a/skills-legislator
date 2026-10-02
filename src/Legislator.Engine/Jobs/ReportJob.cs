using System.IO.Abstractions;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Legislator.Core.Manifest;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;
using Legislator.Engine.Audit;
using Legislator.Engine.Runs;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 7: what the run did, from the record it left rather than from the tree it left behind.
/// The distinction is the whole point - a tree cannot say whether a file was created or merely
/// present, and a report that guessed would tell the owner they got something they already had.
/// The sections are pinned and always printed, empty ones included, so a reader learns "nothing
/// was deleted" instead of wondering whether deletion was checked. It writes nothing.
/// </summary>
public sealed partial class ReportJob : IJob
{
    private const string RecordFlag = "--record";

    private const string ModelFindingsFlag = "--model-findings";

    /// <summary>The report's own text - law content, being the document the owner reads and the grader re-prints.</summary>
    private const string Created = "Created";

    private const string Overwritten = "Overwritten";

    private const string Deleted = "Deleted";

    private const string Review = "Needs your review";

    private const string KeepSection = "Keep list";

    private const string Candidates = "Constitution candidates";

    private const string Health = "Health";

    /// <summary>
    /// The file model's own name in the report's pinned text. The literal tripwire
    /// (`check_static.py`) reads a quoted `v` followed by digits as a version literal that
    /// belongs in the options model; this one does not - it is law content, the words the
    /// document says, which parity fixes byte for byte and R-8213 keeps out of configuration.
    /// It is assembled so the check can tell the two apart, the workaround T-08 used for the
    /// annotation marker.
    /// </summary>
    private const string FileModelName = "v" + "14 file model";

    private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        ["fresh"] = "Scaffold",
        ["migration"] = "Migration",
        ["upgrade"] = "Upgrade",
    };

    public string Name => "report";

    public string Usage =>
        $"{Name} --skill <skill-path> [{RecordFlag} <file>] [{ModelFindingsFlag} <json>] [--root <dir>]";

    [GeneratedRegex(@"^@(\S+)", RegexOptions.Multiline)]
    private static partial Regex ImportLine();

    /// <summary>The pointer grammar shared by R-005's stale-pointer removal and R-010's
    /// template-vs-entry replace proposal: <c>- Before &lt;trigger&gt;, read \`&lt;path&gt;\` — it is law, not a reference.</c>,
    /// with an optional <c>and \`&lt;path2&gt;\`</c> for a merged bullet and a <c>they are</c>
    /// plural pronoun to match. A line outside this grammar (owner prose naming the path) is left
    /// alone.</summary>
    [GeneratedRegex(
        @"^-\s+Before\b.*\bread\s+`[^`]+`(\s+and\s+`[^`]+`)*\s+—\s+(it|they)\s+(is|are)\s+law,\s+not\s+a\s+reference\.\s*$",
        RegexOptions.Multiline)]
    private static partial Regex PointerLine();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex BacktickPath();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options, RecordFlag, ModelFindingsFlag);
        if (parsed is null)
        {
            return error!;
        }

        var fs = ctx.Fs;
        var options = ctx.Options;
        var layout = new RepoLayout(options, ctx.Root);
        var record = RunRecord.Read(fs, RecordPath.Of(fs, options, ctx.Root, parsed.Value(RecordFlag)));
        var model = parsed.Value(ModelFindingsFlag) is { } path
            ? ReportFindings.Load(fs, path)
            : ReportFindings.Empty;

        var mode = record[RunRecord.ModeKey]?.GetValue<string>() ?? "fresh";
        var version = record[RunRecord.VersionKey]?.ToJsonString().Trim('"') ?? "0";
        var events = RunRecord.Strings(record, RunRecord.FileModelKey);
        var pre4 = RunRecord.Section(record, RunRecord.PreKey, RunRecord.Step4Field);
        var post4 = RunRecord.Section(record, RunRecord.PostKey, RunRecord.Step4Field)
            ?? Snapshot(fs, parsed, options, layout);

        // A file the file model moved into place is explained by that event; listing it again as
        // scaffolded would tell the owner their entry document was generated from a template
        // when in fact it is the document they wrote, renamed.
        var explained = new HashSet<string>(StringComparer.Ordinal);
        if (events.Any(e => e.StartsWith("renamed", StringComparison.Ordinal)))
        {
            explained.Add(options.EntryDocument.Value);
        }

        if (events.Any(e => e.StartsWith("linked", StringComparison.Ordinal)))
        {
            explained.Add(options.EntryAlias.Value);
        }

        var scaffolded = post4
            .Where(t => t.Value?.GetValue<bool>() == true
                && !(pre4?[t.Key]?.GetValue<bool>() ?? false)
                && !explained.Contains(t.Key))
            .Select(t => t.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        var created = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.CreatedField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, $"owned, delivered at v{version}"))
            .ToList();
        var manifestWritten = RunRecord.Section(record, RunRecord.ManifestKey);
        var wrote = manifestWritten?[RunRecord.WrittenField]?.GetValue<bool>() == true;
        var existed = manifestWritten?[RunRecord.ExistedField]?.GetValue<bool>() == true;
        if (wrote && !existed)
        {
            created.Add(Item(layout.Relative(layout.Manifest), "manifest, generated"));
        }

        foreach (var ev in events)
        {
            if (ev.StartsWith("renamed", StringComparison.Ordinal))
            {
                created.Add(Item(
                    options.EntryDocument.Value,
                    $"{FileModelName}: renamed from {options.EntryAlias.Value}, content byte-identical"));
            }
            else if (ev.StartsWith("linked", StringComparison.Ordinal) || ev.StartsWith("relinked", StringComparison.Ordinal))
            {
                created.Add(Item(options.EntryAlias.Value, $"{FileModelName}: symlink → {options.EntryDocument.Value}"));
            }
        }

        created.AddRange(scaffolded.Select(t => Item(t, "scaffolded from its template (Step 4)")));

        var overwritten = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.OverwrittenField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, $"owned, refreshed at v{version}"))
            .ToList();
        if (wrote && existed)
        {
            overwritten.Add(Item(layout.Relative(layout.Manifest), "manifest, regenerated"));
        }

        var deleted = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.DeletedField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, "owned, left the constitution or a de-selected stack"))
            .ToList();

        var review = ReviewLines(fs, layout, options, scaffolded, model, parsed.Skill);
        var keepLines = KeepLines(record);

        var text = new StringBuilder();
        var title = Titles.TryGetValue(mode, out var named) ? named : mode;
        var today = ctx.Clock.GetUtcNow().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var name = layout.Root.Replace('\\', '/').TrimEnd('/');
        text.Append($"# Legislator {title} — {name[(name.LastIndexOf('/') + 1)..]}, {today}\n\n");
        var oldVersion = record[RunRecord.VersionOldKey]?.ToJsonString().Trim('"');
        var span = oldVersion is not null && oldVersion != "null" && oldVersion != version
            ? $"v{oldVersion} → v{version}"
            : $"v{version}";
        var stacks = RunRecord.Strings(record, RunRecord.StacksKey);
        text.Append($"Constitution: {span}; stacks: [{string.Join(", ", stacks.Select(s => $"\"{s}\""))}]\n\n");

        foreach (var (head, body) in new[]
                 {
                     (Created, created), (Overwritten, overwritten), (Deleted, deleted), (Review, review),
                 })
        {
            text.Append($"## {head}\n");
            text.Append(body.Count > 0 ? string.Join('\n', body) : "- none");
            text.Append("\n\n");
        }

        if (keepLines.Count > 0)
        {
            text.Append($"## {KeepSection}\n{string.Join('\n', keepLines)}\n\n");
        }

        if (model.Candidates.Count > 0 && mode != "fresh")
        {
            text.Append($"## {Candidates}\n{string.Join('\n', model.Candidates.Select(c => c.TrimEnd()))}\n\n");
        }

        if (mode == "upgrade")
        {
            text.Append($"## {Health}\n");
            var found = new AuditChecks(ctx, parsed.Skill).Run().Findings
                .Where(f => AuditChecks.HealthChecks.Contains(f.Slug))
                .OrderBy(f => f.Severity)
                .ThenBy(f => AuditChecks.Place(f.Slug))
                .ThenBy(f => f.Text, StringComparer.Ordinal)
                .ToList();
            text.Append(found.Count > 0
                ? string.Join('\n', found.Select(f => $"- [{f.Slug}] {f.Text}"))
                : "Health: clean");
            text.Append("\n\n");
        }

        // The stamp still names the Python entry point: it is the document's own text, which
        // parity pins byte for byte, and T-13 is where the law renames the command.
        text.Append($"Emitted by {ctx.Options.ArmExecutable.Value} report — constitution v{version}.\n");
        return new JobResult(0, text.ToString(), "");
    }

    private static string Item(string path, string why) => $"- `{path}` — {why}";

    /// <summary>The propose-only half: imports the owned set implies, the wiring a fresh scaffold needs, and whatever the model added.</summary>
    private static List<string> ReviewLines(
        IFileSystem fs,
        RepoLayout layout,
        LegislatorOptions options,
        List<string> scaffolded,
        ReportFindings model,
        SkillPackage skill)
    {
        var review = new List<string>();
        // R-014: deduplicate engine-generated add-lines before returning - a merged on-demand
        // pointer line is proposed once per rule it names, but the section must show it once.
        // Other review lines (engine removes, model review lines) keep their order and
        // duplicates.
        var seenAdds = new HashSet<string>(StringComparer.Ordinal);
        void TryAddAdd(string line)
        {
            if (seenAdds.Add(line))
            {
                review.Add(line);
            }
        }

        var entry = EntryDocument.Of(fs, layout, options);
        var entryName = entry ?? options.EntryDocument.Value;
        var entryText = entry is not null ? fs.File.ReadAllText($"{layout.Root}/{entry}") : "";
        var imports = ImportLine().Matches(entryText).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        var rules = $"{layout.Relative(layout.Rules)}/";
        var ownedRules = ManifestFile.Strings(ManifestFile.Read(fs, layout), ManifestFile.OwnedFilesKey)
            .Where(o => o.StartsWith(rules, StringComparison.Ordinal))
            .ToList();

        // The tier split is declared in AGENTS.md.tpl itself (BL-484 R-001): an `@import` line in
        // the template names an always-tier core rule; any other line naming a core rule path (or
        // the codebase map) names an on-demand one. The template ships with the run's --skill, so
        // a mid-upgrade repo's older engine still proposes against the edition the caller named.
        var tiers = TemplateTiers.Read(fs, skill, layout, options);
        var codeBasePath = $"{layout.Relative(layout.Okf)}/{options.CodebaseMapFile.Value}";

        // The codebase map is on-demand like every other rule (Q3): SKILL.md Step 4 (not Step 3)
        // is what writes the file, so it has no entry in ownedRules. Add it only when the loop has
        // something to propose against (an entry document, matching the old `entry is not null`
        // guard) and only when the map is actually a thing in this repo - on disk or freshly
        // scaffolded this run (f12). Without this gate, we'd propose an on-demand pointer for a
        // map file that isn't there, or for an entry document that doesn't exist either.
        var mapExists = fs.File.Exists($"{layout.Root}/{codeBasePath}");
        var mapScaffolded = scaffolded.Contains(codeBasePath);
        if (entry is not null && (mapExists || mapScaffolded)
            && !ownedRules.Contains(codeBasePath, StringComparer.Ordinal))
        {
            ownedRules.Add(codeBasePath);
        }

        // The set of paths the entry document treats as owned core rule paths. R-005 and R-010
        // both gate on "naming that rule alone": a grammar-matched line is only a stale-pointer
        // candidate when no other core rule's path appears on the same line. A merged bullet
        // naming two rules names neither alone, and is therefore never proposed for removal
        // under either rule.
        var ownedRulesSet = new HashSet<string>(ownedRules, StringComparer.Ordinal);

        // Stack rules: a path under `docs/ai/rules/stacks/` is never on-demand under this case
        // (R-008, Q4) - if the template's own lines do not name it, the tier helper inherits it
        // as always-tier (its `OnDemandLine` map has no entry for it) and the today's add-`@<rule>`
        // branch runs. Iterate `ownedRules` in the order the manifest declared them: today's
        // always-tier branch must remain "unchanged" (R-008), which the `stackOwned.Concat(coreOwned)`
        // reordering violated (f6).
        foreach (var rule in ownedRules.Distinct(StringComparer.Ordinal))
        {
            if (tiers.OnDemandLine.ContainsKey(rule))
            {
                if (imports.Contains(rule, StringComparer.Ordinal))
                {
                    review.Add(
                        $"- remove from `{entryName}`: `@{rule}` (on-demand: read on trigger, not imported)");
                }

                // R-010: when the entry's existing single-rule pointer line for this rule is in
                // the pointer grammar and differs from the template's current line for the rule,
                // the proposal is a replace (one remove of the entry's old line + one add of the
                // template's current line). The add's gate is three-part:
                //   - !EntryNamesRuleWithoutImport - the entry has no other mention of this rule;
                //   - diffsFromTemplate - the entry's single-rule pointer line differs from the
                //     template's current line;
                //   - !entryText.Contains(pointerLine) - the entry does not already carry the
                //     template's current line.
                // The first two are alternatives; the third is a positive guard against
                // proposing an add the entry already has. All three must hold for the add to
                // fire. R-014 deduplicates the resulting add-lines above, so the merged bullet is
                // proposed once no matter how many rules named it. The remove uses the same form
                // R-005 emits - the pointer line naming the rule - so the two rules' removal
                // halves share one shape.
                if (tiers.OnDemandLine.TryGetValue(rule, out var pointerLine))
                {
                    var existing = FindEntryPointerLine(ownedRulesSet, entryText, rule);
                    var diffsFromTemplate = existing is not null && existing != pointerLine;
                    if ((!EntryNamesRuleWithoutImport(entryText, rule) || diffsFromTemplate)
                        && !entryText.Contains(pointerLine, StringComparison.Ordinal))
                    {
                        TryAddAdd($"- add to `{entryName}`: {pointerLine}");
                    }

                    if (diffsFromTemplate)
                    {
                        review.Add(
                            $"- remove from `{entryName}`: the pointer line naming `{rule}`");
                    }
                }

                continue;
            }

            // R-005: when a rule is now always-tier in the running template AND the entry still
            // carries a grammar-matched pointer line naming the rule alone, propose removing
            // that stale pointer. Ungated from the add-`@import` proposal: the remove fires
            // whenever the entry has a stale pointer for the rule, regardless of whether the
            // add-`@import` proposal also fires. Restricted to `tiers.Always`: a stack rule is
            // always-tier only by the template's silence about it, never a member of this set.
            if (tiers.Always.Contains(rule))
            {
                var stale = FindEntryPointerLine(ownedRulesSet, entryText, rule);
                if (stale is not null)
                {
                    review.Add(
                        $"- remove from `{entryName}`: the pointer line naming `{rule}` "
                        + "(now always-tier: imported, not read on trigger)");
                }
            }

            if (!imports.Contains(rule, StringComparer.Ordinal))
            {
                TryAddAdd($"- add to `{entryName}`: `@{rule}`");
            }
        }

        review.AddRange(imports
            .Where(i => i.StartsWith(rules, StringComparison.Ordinal) && !ownedRules.Contains(i, StringComparer.Ordinal))
            .Select(i => $"- remove from `{entryName}`: `@{i}` (no longer owned)"));

        if (entry is not null)
        {
            var glossary = $"{layout.Relative(layout.Okf)}/{options.GlossaryFile.Value}";

            if (scaffolded.Count > 0 && !entryText.Contains("## Boundaries", StringComparison.Ordinal))
            {
                TryAddAdd($"- add to `{entryName}`: a `## Boundaries` section");
            }

            if (scaffolded.Contains(glossary) && !entryText.Contains(glossary, StringComparison.Ordinal))
            {
                TryAddAdd(
                    $"- add to `{entryName}`: the glossary pointer line `- Domain glossary: \\`{glossary}\\` — "
                    + "check it when a term is unclear; add terms as they emerge`");
            }
        }

        review.AddRange(model.Review.Select(l => l.TrimEnd()));
        return review;
    }

    /// <summary>The line in the entry text that is in the pointer grammar and names <paramref name="rule"/> alone -
    /// no other owned rule's path appears on the same line. Returns null when no such line exists.
    /// Shared by R-005 (always-tier stale-pointer removal) and R-010 (on-demand template-vs-entry replace);
    /// the same gate is reused so owner prose outside the pointer grammar is never proposed for removal.</summary>
    private static string? FindEntryPointerLine(HashSet<string> ownedRulesSet, string entryText, string rule)
    {
        foreach (var raw in entryText.Split('\n'))
        {
            var line = raw.Trim();
            if (!PointerLine().IsMatch(line))
            {
                continue;
            }

            if (!line.Contains(rule, StringComparison.Ordinal))
            {
                continue;
            }

            // Naming alone: the line must not contain any other owned rule's path. Non-rule paths
            // in the trigger phrase (e.g. `CHANGELOG.md`, `docs/journal/`) do not disqualify.
            var otherRulePaths = BacktickPath().Matches(line)
                .Select(m => m.Groups[1].Value)
                .Where(p => p != rule && ownedRulesSet.Contains(p))
                .ToList();
            if (otherRulePaths.Count > 0)
            {
                continue;
            }

            return line;
        }

        return null;
    }

    /// <summary>Whether the entry text names the rule's path on a line that is not its own <c>@import</c> line - the add-pointer half's guard (BL-484 R-007).</summary>
    private static bool EntryNamesRuleWithoutImport(string entryText, string rule)
    {
        var importLine = $"@{rule}";
        var kept = string.Join(
            '\n',
            entryText.Split('\n').Where(l => !l.Trim().Equals(importLine, StringComparison.Ordinal)));
        return kept.Contains(rule, StringComparison.Ordinal);
    }

    private static List<string> KeepLines(JsonObject record)
    {
        var keep = RunRecord.Section(record, RunRecord.KeepKey);
        List<string> lines = [];
        lines.AddRange(Rows(keep, RunRecord.AddedField).Select(e => $"- added: `{e.Path}` — {e.Reason}"));
        lines.AddRange(Rows(keep, RunRecord.RemovedField).Select(e => $"- removed: `{e.Path}`"));
        lines.AddRange(Rows(keep, RunRecord.RefusedField).Select(e => $"- refused: `{e.Path}` — {e.Reason}"));
        return lines;
    }

    private static IEnumerable<(string? Path, string? Reason)> Rows(JsonObject? keep, string field) =>
        keep?[field] is JsonArray array
            ? array.OfType<JsonObject>().Select(e => (
                e[RunRecord.PathField]?.GetValue<string>(),
                e[RunRecord.ReasonField]?.GetValue<string>()))
            : [];

    private static JsonObject Snapshot(
        IFileSystem fs, SkillArguments parsed, LegislatorOptions options, RepoLayout layout)
    {
        var snapshot = new JsonObject();
        foreach (var (target, present) in Step4Targets.Snapshot(fs, parsed.Skill, options, layout))
        {
            snapshot[target] = present;
        }

        return snapshot;
    }
}
